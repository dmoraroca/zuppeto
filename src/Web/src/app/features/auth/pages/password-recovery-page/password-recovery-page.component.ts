import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({ selector: 'app-password-recovery-page', standalone: true, imports: [ReactiveFormsModule, RouterLink], templateUrl: './password-recovery-page.component.html', styleUrls: ['./password-recovery-page.component.scss'], changeDetection: ChangeDetectionStrategy.Eager })
export class PasswordRecoveryPageComponent {
  private readonly formBuilder = inject(FormBuilder); private readonly auth = inject(AuthService);
  protected readonly sent = signal(false);
  protected readonly form = this.formBuilder.nonNullable.group({ email: ['', [Validators.required, Validators.email]] });
  protected async submit(): Promise<void> { if (this.form.invalid) { this.form.markAllAsTouched(); return; } await this.auth.requestPasswordRecovery(this.form.getRawValue().email); this.sent.set(true); }
}
