import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({ selector: 'app-password-reset-page', standalone: true, imports: [ReactiveFormsModule, RouterLink], templateUrl: './password-reset-page.component.html', styleUrls: ['./password-reset-page.component.scss'], changeDetection: ChangeDetectionStrategy.Eager })
export class PasswordResetPageComponent {
  private readonly formBuilder = inject(FormBuilder); private readonly auth = inject(AuthService); private readonly route = inject(ActivatedRoute);
  protected readonly status = signal<'idle' | 'Reset' | 'Invalid' | 'Expired' | 'Used'>('idle');
  protected readonly form = this.formBuilder.nonNullable.group({ newPassword: ['', [Validators.required, Validators.minLength(6)]], confirmNewPassword: ['', Validators.required] });
  protected async submit(): Promise<void> { if (this.form.invalid) { this.form.markAllAsTouched(); return; } const value = this.form.getRawValue(); this.status.set(await this.auth.resetPassword(this.route.snapshot.queryParamMap.get('token') ?? '', value.newPassword, value.confirmNewPassword)); }
}
