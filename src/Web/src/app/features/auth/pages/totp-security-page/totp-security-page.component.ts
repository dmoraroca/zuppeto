import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-totp-security-page', standalone: true, imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './totp-security-page.component.html', styleUrl: './totp-security-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TotpSecurityPageComponent {
  private readonly auth = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  protected readonly setup = signal<{ qrSvg: string; manualEntryKey: string } | null>(null);
  protected readonly recoveryCodes = signal<string[] | null>(null);
  protected readonly error = signal('');
  protected readonly busy = signal(false);
  protected readonly sessionEnded = signal(false);
  protected readonly enabled = signal(this.auth.currentUser()?.isTotpEnabled ?? false);
  protected readonly form = this.formBuilder.nonNullable.group({ code: ['', [Validators.required, Validators.minLength(6)]] });

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
}
