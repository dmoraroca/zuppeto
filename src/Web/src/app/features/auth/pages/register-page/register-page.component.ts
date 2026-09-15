import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register-page', standalone: true, imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register-page.component.html', styleUrls: ['./register-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager
})
export class RegisterPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  protected readonly submitted = signal(false);
  protected readonly failed = signal(false);
  protected readonly form = this.formBuilder.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.failed.set(false);
    const ok = await this.auth.register(this.form.getRawValue());
    this.submitted.set(ok);
    this.failed.set(!ok);
  }
}
