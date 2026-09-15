import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-account-activation-page', standalone: true, imports: [RouterLink],
  templateUrl: './account-activation-page.component.html', styleUrls: ['./account-activation-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager
})
export class AccountActivationPageComponent {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  protected readonly status = signal<'loading' | 'Activated' | 'Invalid' | 'Expired' | 'Used'>('loading');
  constructor() { void this.activate(); }
  private async activate(): Promise<void> {
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.status.set(await this.auth.activateEmail(token));
  }
}
