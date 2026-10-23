import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

export interface PlaceShareTarget {
  readonly id: string;
  readonly name: string;
  readonly city: string;
}

export type PlaceShareResult =
  | { readonly status: 'shared' | 'copied' | 'cancelled'; readonly url: string }
  | { readonly status: 'manual' | 'failed'; readonly url: string; readonly message: string };

const PUBLIC_ORIGIN_META = 'petiloc-public-origin';

@Injectable({ providedIn: 'root' })
export class PlaceShareService {
  private readonly document = inject(DOCUMENT);
  private activeOperation: Promise<PlaceShareResult> | null = null;

  public share(target: PlaceShareTarget): Promise<PlaceShareResult> {
    if (this.activeOperation) return this.activeOperation;

    this.activeOperation = this.execute(target).finally(() => {
      this.activeOperation = null;
    });
    return this.activeOperation;
  }

  public buildUrl(placeId: string): string {
    const origin = this.publicOrigin();
    return new URL(`/places/${encodeURIComponent(placeId)}`, origin).href;
  }

  public buildWhatsAppUrl(target: PlaceShareTarget): string {
    return `https://api.whatsapp.com/send?text=${encodeURIComponent(this.shareMessage(target))}`;
  }

  public buildTelegramUrl(target: PlaceShareTarget): string {
    const query = new URLSearchParams({
      url: this.buildUrl(target.id),
      text: `${target.name} — Descobreix aquest establiment a PetiLoc`
    });
    return `https://t.me/share/url?${query.toString()}`;
  }

  public buildMailtoUrl(target: PlaceShareTarget): string {
    const subject = `${target.name} — PetiLoc`;
    const body = this.shareMessage(target);
    return `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
  }

  public canShareNatively(): boolean {
    return typeof this.document.defaultView?.navigator.share === 'function';
  }

  public copy(target: PlaceShareTarget): Promise<PlaceShareResult> {
    return this.runExclusive(() => this.copyUrl(this.buildUrl(target.id)));
  }

  public shareNatively(target: PlaceShareTarget): Promise<PlaceShareResult> {
    return this.runExclusive(() => this.executeNativeShare(target));
  }

  private async execute(target: PlaceShareTarget): Promise<PlaceShareResult> {
    const url = this.buildUrl(target.id);
    const browser = this.document.defaultView;
    const browserNavigator = browser?.navigator;
    if (!browser || !browserNavigator) {
      return { status: 'failed', url, message: 'No s’ha pogut preparar l’enllaç per compartir.' };
    }

    const data: ShareData = {
      title: target.name,
      text: `${target.name} · ${target.city}`,
      url
    };
    let canUseNativeShare = typeof browserNavigator.share === 'function';
    if (canUseNativeShare && typeof browserNavigator.canShare === 'function') {
      try {
        canUseNativeShare = browserNavigator.canShare(data);
      } catch {
        canUseNativeShare = false;
      }
    }
    if (canUseNativeShare) {
      try {
        await browserNavigator.share(data);
        return { status: 'shared', url };
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return { status: 'cancelled', url };
        }
        return { status: 'failed', url, message: 'No s’ha pogut compartir l’establiment. Torna-ho a provar.' };
      }
    }

    if (typeof browserNavigator.clipboard?.writeText === 'function') {
      try {
        await browserNavigator.clipboard.writeText(url);
        return { status: 'copied', url };
      } catch {
        return this.manualCopy(browser, url, 'No s’ha pogut copiar automàticament.');
      }
    }

    return this.manualCopy(browser, url, 'El navegador no permet copiar automàticament.');
  }

  private async executeNativeShare(target: PlaceShareTarget): Promise<PlaceShareResult> {
    const url = this.buildUrl(target.id);
    const browserNavigator = this.document.defaultView?.navigator;
    if (!browserNavigator || typeof browserNavigator.share !== 'function') {
      return { status: 'failed', url, message: 'Aquest navegador no ofereix més opcions de compartició.' };
    }

    const data: ShareData = {
      title: target.name,
      text: `${target.name} · ${target.city}`,
      url
    };
    if (typeof browserNavigator.canShare === 'function') {
      try {
        if (!browserNavigator.canShare(data)) {
          return { status: 'failed', url, message: 'Aquest navegador no pot compartir aquest enllaç.' };
        }
      } catch {
        return { status: 'failed', url, message: 'Aquest navegador no pot compartir aquest enllaç.' };
      }
    }

    try {
      await browserNavigator.share(data);
      return { status: 'shared', url };
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return { status: 'cancelled', url };
      }
      return { status: 'failed', url, message: 'No s’ha pogut compartir l’establiment. Torna-ho a provar.' };
    }
  }

  private async copyUrl(url: string): Promise<PlaceShareResult> {
    const browser = this.document.defaultView;
    const browserNavigator = browser?.navigator;
    if (!browser || !browserNavigator) {
      return { status: 'failed', url, message: 'No s’ha pogut preparar l’enllaç per copiar.' };
    }
    if (typeof browserNavigator.clipboard?.writeText === 'function') {
      try {
        await browserNavigator.clipboard.writeText(url);
        return { status: 'copied', url };
      } catch {
        return this.manualCopy(browser, url, 'No s’ha pogut copiar automàticament.');
      }
    }
    return this.manualCopy(browser, url, 'El navegador no permet copiar automàticament.');
  }

  private runExclusive(operation: () => Promise<PlaceShareResult>): Promise<PlaceShareResult> {
    if (this.activeOperation) return this.activeOperation;
    this.activeOperation = operation().finally(() => {
      this.activeOperation = null;
    });
    return this.activeOperation;
  }

  private shareMessage(target: PlaceShareTarget): string {
    return `${target.name} — Descobreix aquest establiment a PetiLoc: ${this.buildUrl(target.id)}`;
  }

  private manualCopy(browser: Window, url: string, reason: string): PlaceShareResult {
    if (typeof browser.prompt !== 'function') {
      return { status: 'failed', url, message: `${reason} Copia l’enllaç manualment: ${url}` };
    }
    try {
      browser.prompt('Copia aquest enllaç:', url);
      return { status: 'manual', url, message: `${reason} Copia l’enllaç de la finestra.` };
    } catch {
      return { status: 'failed', url, message: `${reason} Copia l’enllaç manualment: ${url}` };
    }
  }

  private publicOrigin(): string {
    const currentOrigin = this.document.location.origin;
    const configuredOrigin = this.document
      .querySelector<HTMLMetaElement>(`meta[name="${PUBLIC_ORIGIN_META}"]`)
      ?.content.trim();
    if (!configuredOrigin) return currentOrigin;

    try {
      const parsed = new URL(configuredOrigin);
      return parsed.protocol === 'http:' || parsed.protocol === 'https:' ? parsed.origin : currentOrigin;
    } catch {
      return currentOrigin;
    }
  }
}
