import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-totp-challenge-page', standalone: true, imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './totp-challenge-page.component.html',
  styleUrls: ['../resend-activation-page/resend-activation-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TotpChallengePageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly error = signal(false);
  protected readonly busy = signal(false);
  protected readonly form = this.formBuilder.nonNullable.group({ code: ['', [Validators.required, Validators.minLength(6)]] });

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.busy()) return;
    this.error.set(false); this.busy.set(true);
    const ok = await this.auth.completeTotpLogin(this.route.snapshot.queryParamMap.get('challenge') ?? '', this.form.getRawValue().code);
    this.busy.set(false);
    if (ok) void this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('redirectTo') || this.auth.getPostLoginRoute());
    else this.error.set(true);
  }
}
