import { Component, computed, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { ErrorNotificationsService } from '../../../../core/services/error-notifications.service';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-profile-page',
  imports: [ReactiveFormsModule, SiteHeaderComponent, SiteFooterComponent, SectionHeadingComponent],
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss'
})
export class ProfilePageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(ErrorNotificationsService);
  private readonly router = inject(Router);
  private readonly currentUser = this.authService.currentUser();

  protected readonly user = computed(() => this.authService.currentUser());
  protected readonly isAdmin = computed(() => this.authService.isAdmin());

  protected readonly form = this.formBuilder.nonNullable.group({
    name: [this.currentUser?.name ?? '', [Validators.required, Validators.minLength(3)]],
    city: [this.currentUser?.city ?? '', Validators.required],
    country: [this.currentUser?.country ?? '', Validators.required],
    bio: [this.currentUser?.bio ?? '', [Validators.required, Validators.minLength(12)]],
    avatarUrl: [this.currentUser?.avatarUrl ?? ''],
    privacyAccepted: [this.currentUser?.privacyAccepted ?? false]
  });

  protected readonly previewAvatarUrl = computed(() => {
    const raw = this.form.controls.avatarUrl.value.trim();

    return raw || null;
  });

  protected save(): void {
    if (!this.isAdmin() && !this.form.controls.privacyAccepted.value) {
      this.notifications.notify(
        'Consentiment obligatori',
        'Per guardar el perfil cal acceptar el tractament de dades del manteniment d’usuari.'
      );

      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.notify('Revisa el perfil', 'Completa tots els camps obligatoris abans de guardar.');

      return;
    }

    const value = this.form.getRawValue();
    this.authService.updateProfile({
      name: value.name.trim(),
      city: value.city.trim(),
      country: value.country.trim(),
      bio: value.bio.trim(),
      avatarUrl: value.avatarUrl.trim() || null,
      privacyAccepted: this.isAdmin() ? true : value.privacyAccepted
    });

    this.notifications.notify('Perfil actualitzat', 'Els canvis s’han guardat correctament en mode fake.');
  }

  protected logout(): void {
    this.authService.logout();
    this.notifications.notify('Sessió tancada', 'Has sortit del perfil de proves.');
    void this.router.navigateByUrl('/');
  }
}
