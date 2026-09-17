import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { GoogleIdentityService } from '../../services/google-identity.service';
import { AccessMethod } from '../../models/auth-user.model';

@Component({
  selector: 'app-totp-security-page', standalone: true, imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './totp-security-page.component.html', styleUrl: './totp-security-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TotpSecurityPageComponent implements AfterViewInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly googleIdentity = inject(GoogleIdentityService);
  private readonly formBuilder = inject(FormBuilder);
  protected readonly setup = signal<{ qrSvg: string; manualEntryKey: string } | null>(null);
  protected readonly recoveryCodes = signal<string[] | null>(null);
  protected readonly error = signal('');
  protected readonly busy = signal(false);
  protected readonly sessionEnded = signal(false);
  protected readonly enabled = signal(this.auth.currentUser()?.isTotpEnabled ?? false);
  protected readonly accessMethods = signal<AccessMethod[]>([]);
  protected readonly accessMethodsLoading = signal(true);
  protected readonly linkMessage = signal('');
  private googleClientId: string | null = null;
  private googleButtonRendered = false;
  private googleButtonCleanup?: () => void;
  private destroyed = false;
  private googleLinkHost?: ElementRef<HTMLDivElement>;
  @ViewChild('googleLinkHost')
  private set googleLinkHostElement(value: ElementRef<HTMLDivElement> | undefined) {
    this.googleLinkHost = value;
    if (value) queueMicrotask(() => void this.renderGoogleLinkButton());
  }
  protected readonly form = this.formBuilder.nonNullable.group({ code: ['', [Validators.required, Validators.minLength(6)]] });

  constructor() {
    void this.loadAccessMethods();
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => void this.renderGoogleLinkButton());
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.googleButtonCleanup?.();
  }

  protected accessMethod(provider: string): AccessMethod | undefined {
    return this.accessMethods().find((method) => method.provider === provider);
  }

  protected accessMethodLabel(method: AccessMethod): string {
    if (method.linked) return 'Vinculat';
    if (method.status === 'linkable') return 'Vincular';
    if (method.status === 'pending') return 'Pendent';
    return 'No disponible';
  }

  protected async start(): Promise<void> {
    this.error.set(''); this.busy.set(true);
    try { this.setup.set(await this.auth.startTotpSetup()); this.form.reset(); }
    catch { this.error.set('No s’ha pogut iniciar la configuració. Torna-ho a provar.'); }
    finally { this.busy.set(false); }
  }
  protected async confirm(): Promise<void> {
    if (this.form.invalid) return;
    this.error.set(''); this.busy.set(true);
    const codes = await this.auth.confirmTotpSetup(this.form.getRawValue().code); this.busy.set(false);
    if (!codes) { this.error.set('El codi no és vàlid o la configuració ha caducat.'); return; }
    this.recoveryCodes.set(codes); this.enabled.set(true); this.sessionEnded.set(true); this.setup.set(null); this.form.reset();
  }
  protected async regenerate(): Promise<void> {
    if (this.form.invalid) return;
    this.error.set(''); this.busy.set(true);
    const codes = await this.auth.regenerateTotpRecoveryCodes(this.form.getRawValue().code); this.busy.set(false);
    if (!codes) this.error.set('Cal un codi temporal vàlid.');
    else { this.recoveryCodes.set(codes); this.form.reset(); }
  }
  protected async disable(): Promise<void> {
    if (this.form.invalid || !window.confirm('Vols desactivar la verificació en dos passos?')) return;
    this.error.set(''); this.busy.set(true);
    const disabled = await this.auth.disableTotp(this.form.getRawValue().code); this.busy.set(false);
    if (!disabled) { this.error.set('Cal verificar un codi temporal o un codi de recuperació vàlid.'); return; }
    this.enabled.set(false); this.sessionEnded.set(true); this.recoveryCodes.set(null); this.form.reset();
  }

  private async loadAccessMethods(): Promise<void> {
    this.accessMethodsLoading.set(true);
    try {
      const [methods, providers] = await Promise.all([this.auth.getAccessMethods(), this.auth.getProviders()]);
      this.accessMethods.set(methods);
      this.googleClientId = providers.find((provider) => provider.key === 'google' && provider.configured)?.clientId ?? null;
      setTimeout(() => void this.renderGoogleLinkButton(), 0);
    } catch {
      this.error.set('No s’han pogut carregar els mètodes d’accés. Torna-ho a provar.');
    } finally {
      this.accessMethodsLoading.set(false);
    }
  }

  private async renderGoogleLinkButton(): Promise<void> {
    const google = this.accessMethod('google');
    const host = this.googleLinkHost?.nativeElement;
    if (!host || !this.googleClientId || google?.linked || this.googleButtonRendered) return;

    try {
      const cleanup = await this.googleIdentity.renderButton(host, this.googleClientId, 'LINK', (credential) => {
        void this.linkGoogle(credential);
      }, { text: 'continue_with', width: Math.max(240, Math.round(host.getBoundingClientRect().width || 280)) });
      if (this.destroyed) {
        cleanup();
        return;
      }
      this.googleButtonCleanup?.();
      this.googleButtonCleanup = cleanup;
      this.googleButtonRendered = true;
    } catch {
      this.error.set('Google no està disponible ara mateix. Torna-ho a provar més tard.');
    }
  }

  private async linkGoogle(idToken: string): Promise<void> {
    this.error.set('');
    this.linkMessage.set('');
    this.busy.set(true);
    const result = await this.auth.linkGoogle(idToken);
    this.busy.set(false);

    if (result.ok) {
      this.accessMethods.update((methods) => methods.map((method) =>
        method.provider === 'google' ? { ...method, linked: true, status: 'linked' } : method));
      this.linkMessage.set(result.alreadyLinked ? 'Google ja estava vinculat a aquest compte.' : 'Google s’ha vinculat correctament.');
      this.googleButtonCleanup?.();
      this.googleButtonCleanup = undefined;
      return;
    }

    const messages: Record<NonNullable<typeof result.failure>, string> = {
      'provider-unavailable': 'Google no està disponible o no està configurat correctament.',
      'identity-rejected': 'Google no ha pogut validar aquesta autenticació.',
      'email-mismatch': 'Has seleccionat un compte Google diferent del compte Petiloc actual.',
      'linked-elsewhere': 'Aquesta identitat Google ja està vinculada a un altre compte Petiloc.',
      'provider-already-linked': 'Aquest compte Petiloc ja té una altra identitat Google vinculada.',
      conflict: 'No s’ha pogut completar la vinculació. Torna-ho a provar.'
    };
    this.error.set(messages[result.failure ?? 'conflict']);
  }
}
