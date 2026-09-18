import { Injectable } from '@angular/core';

export type GoogleIdentityMode = 'LOGIN' | 'LINK';

@Injectable({ providedIn: 'root' })
export class GoogleIdentityService {
  private scriptPromise: Promise<void> | null = null;
  private initializedClientId: string | null = null;
  private activeRegistration: {
    id: number;
    mode: GoogleIdentityMode;
    onCredential: (idToken: string) => void;
  } | null = null;
  private renderSequence = 0;

  async disableAutoSelect(): Promise<void> {
    try {
      await this.loadScript();
      window.google?.accounts.id.disableAutoSelect();
    } catch {
      // Petiloc logout must still complete if the external GIS script is unavailable.
    }
  }

  async renderButton(
    host: HTMLElement,
    clientId: string,
    mode: GoogleIdentityMode,
    onCredential: (idToken: string) => void,
    options: { width?: number; text?: 'signin_with' | 'continue_with' } = {}
  ): Promise<() => void> {
    await this.loadScript();
    if (!window.google?.accounts?.id) {
      throw new Error('Google Identity Services no està disponible.');
    }

    this.initializeOnce(clientId);
    const registrationId = ++this.renderSequence;
    this.activeRegistration = { id: registrationId, mode, onCredential };
    host.innerHTML = '';
    window.google.accounts.id.renderButton(host, {
      theme: 'outline',
      size: 'large',
      shape: 'pill',
      text: options.text ?? 'signin_with',
      width: options.width
    });

    return () => {
      if (this.activeRegistration?.id === registrationId) {
        this.activeRegistration = null;
      }
      host.innerHTML = '';
    };
  }

  private initializeOnce(clientId: string): void {
    if (this.initializedClientId && this.initializedClientId !== clientId) {
      throw new Error('Google Identity Services ja està inicialitzat amb un altre Client ID.');
    }
    if (this.initializedClientId) return;

    window.google!.accounts.id.initialize({
      client_id: clientId,
      callback: ({ credential }) => {
        // GIS owns a single callback per page. Angular owns the active route, so
        // only the latest mounted Google control may consume the credential.
        this.activeRegistration?.onCredential(credential);
      }
    });
    this.initializedClientId = clientId;
  }

  private loadScript(): Promise<void> {
    if (window.google?.accounts?.id) return Promise.resolve();
    if (this.scriptPromise) return this.scriptPromise;

    this.scriptPromise = new Promise<void>((resolve, reject) => {
      const existing = document.querySelector<HTMLScriptElement>('script[data-google-identity]');
      if (existing) {
        existing.addEventListener('load', () => resolve(), { once: true });
        existing.addEventListener('error', () => reject(new Error('No s’ha pogut carregar Google Identity Services.')), { once: true });
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.setAttribute('data-google-identity', 'true');
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('No s’ha pogut carregar Google Identity Services.'));
      document.head.appendChild(script);
    });
    return this.scriptPromise;
  }
}
