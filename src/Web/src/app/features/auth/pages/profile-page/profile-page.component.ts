import { AfterViewInit, Component, computed, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { ErrorNotificationsService } from '../../../../core/services/error-notifications.service';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { fileToAvatarDataUrl } from '../../../../shared/utils/avatar-image.util';
import { PasswordFieldComponent } from '../../components/password-field/password-field.component';
import {
  PASSWORD_STRENGTH_POLICY,
  PasswordStrengthPolicy
} from '../../policies/password-strength.policy';
import {
  PROFILE_PASSWORD_CHANGE_POLICY,
  ProfilePasswordChangePolicy
} from '../../policies/profile-password-change.policy';
import {
  PROFILE_SAVE_POLICY,
  ProfileSavePolicy,
  readProfileFormSnapshot,
  wantsEmailChange,
  wantsPasswordChange
} from '../../policies/profile-save.policy';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-profile-page',
  imports: [
    ReactiveFormsModule,
    SiteHeaderComponent,
    SiteFooterComponent,
    SectionHeadingComponent,
    PasswordFieldComponent
  ],
  templateUrl: './profile-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './profile-page.component.scss'
})
export class ProfilePageComponent implements AfterViewInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(ErrorNotificationsService);
  private readonly router = inject(Router);
  private readonly savePolicy = inject<ProfileSavePolicy>(PROFILE_SAVE_POLICY);
  private readonly passwordChangePolicy = inject<ProfilePasswordChangePolicy>(
    PROFILE_PASSWORD_CHANGE_POLICY
  );
  private readonly strengthPolicy = inject<PasswordStrengthPolicy>(PASSWORD_STRENGTH_POLICY);
  private readonly currentUser = this.authService.currentUser();

  protected readonly user = computed(() => this.authService.currentUser());
  protected readonly isAdmin = computed(() => this.authService.isAdmin());
  protected readonly avatarPreview = signal<string | null>(this.currentUser?.avatarUrl ?? null);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: [this.currentUser?.name ?? '', [Validators.required, Validators.minLength(3)]],
    email: [this.currentUser?.email ?? '', [Validators.required, Validators.email]],
    currentPassword: [''],
    newPassword: [{ value: '', disabled: true }, [Validators.minLength(this.strengthPolicy.minLength)]],
    confirmNewPassword: [{ value: '', disabled: true }],
    city: [this.currentUser?.city ?? '', Validators.required],
    country: [this.currentUser?.country ?? '', Validators.required],
    bio: [this.currentUser?.bio ?? ''],
    avatarUrl: [this.currentUser?.avatarUrl ?? ''],
    privacyAccepted: [this.currentUser?.privacyAccepted ?? false]
  });

  protected readonly previewAvatarUrl = computed(() => this.avatarPreview());
  private readonly formRevision = signal(0);
  protected readonly currentPasswordMatches = signal(false);
  private currentEditedByUser = false;

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.refreshFormState());

    this.form.controls.currentPassword.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.currentPasswordMatches.set(false);
        this.lockNewPasswordFields();
      });

    this.form.controls.currentPassword.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => void this.verifyCurrentPassword(value));

    effect(() => {
      const sessionUser = this.user();
      if (!sessionUser || this.form.dirty) {
        return;
      }

      this.form.patchValue(
        {
          name: sessionUser.name ?? '',
          email: sessionUser.email ?? '',
          city: sessionUser.city ?? '',
          country: sessionUser.country ?? '',
          bio: sessionUser.bio ?? '',
          avatarUrl: sessionUser.avatarUrl ?? '',
          privacyAccepted: sessionUser.privacyAccepted ?? false
        },
        { emitEvent: false }
      );
      this.avatarPreview.set(sessionUser.avatarUrl ?? null);
    });
  }

  ngAfterViewInit(): void {
    this.resetPasswordFields();
    for (const delay of this.passwordChangePolicy.autofillClearDelaysMs) {
      setTimeout(() => this.resetPasswordFields(), delay);
    }
  }

  protected onCurrentPasswordFocused(): void {
    this.currentEditedByUser = true;
  }

  protected onPasswordFieldChanged(): void {
    this.refreshFormState();
  }

  private refreshFormState(): void {
    this.form.markAsDirty();
    this.formRevision.update((revision) => revision + 1);
  }

  private readonly snapshot = computed(() => {
    this.formRevision();
    return readProfileFormSnapshot(
      this.form,
      this.isAdmin(),
      this.user()?.email ?? this.currentUser?.email ?? '',
      this.currentPasswordMatches()
    );
  });

  protected readonly emailFormatInvalid = computed(() => {
    this.formRevision();
    return this.form.controls.email.hasError('email');
  });

  protected readonly requireNewPasswords = computed(() => wantsPasswordChange(this.snapshot()));

  protected readonly missingRequiredFields = computed(() =>
    this.savePolicy.missingRequiredLabels(this.snapshot())
  );

  protected readonly canSave = computed(() => this.savePolicy.canSave(this.snapshot()));

  protected async onAvatarSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.[0];

    if (!file) {
      return;
    }

    await this.setAvatarFromFile(file);
    input.value = '';
  }

  protected async onAvatarDropped(event: DragEvent): Promise<void> {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];

    if (!file) {
      return;
    }

    await this.setAvatarFromFile(file);
  }

  protected allowAvatarDrop(event: DragEvent): void {
    event.preventDefault();
  }

  protected removeAvatar(): void {
    this.avatarPreview.set(null);
    this.form.controls.avatarUrl.setValue('');
  }

  protected async save(): Promise<void> {
    if (!this.canSave()) {
      this.form.markAllAsTouched();
      this.notifications.notify(
        'Revisa el perfil',
        'Completa els camps obligatoris. Per canviar la contrasenya, l’actual ha de coincidir amb la del compte i la nova ha de tenir el format correcte.',
        'error'
      );
      return;
    }

    const value = this.form.getRawValue();
    const nextEmail = value.email.trim().toLowerCase();
    const currentPassword = value.currentPassword.trim();
    const password = value.newPassword.trim();
    const decision = this.passwordChangePolicy.resolveSave({
      currentPassword,
      newPassword: password,
      emailChanged: wantsEmailChange(this.snapshot())
    });

    if (decision.kind === 'save-without-password-check') {
      this.currentPasswordMatches.set(false);
      this.lockNewPasswordFields();
    } else {
      const matches = await this.authService.verifyCurrentPassword(currentPassword);
      if (!matches) {
        this.notifications.notify(
          'Contrasenya incorrecta',
          'La contrasenya actual no coincideix amb la del compte.',
          'error'
        );
        this.currentPasswordMatches.set(false);
        this.lockNewPasswordFields();
        return;
      }

      this.currentPasswordMatches.set(true);
    }

    try {
      const writeAccount =
        decision.kind === 'save-without-password-check'
          ? decision.writeAccount
          : decision.writeAccountIfMatch;
      if (writeAccount) {
        await this.authService.updateAccount({
          email: nextEmail,
          currentPassword: value.currentPassword,
          newPassword: password,
          confirmNewPassword: value.confirmNewPassword.trim()
        });
      }

      await this.authService.updateProfile({
        name: value.name.trim(),
        city: value.city.trim(),
        country: value.country.trim(),
        bio: value.bio.trim(),
        avatarUrl: this.avatarPreview(),
        privacyAccepted: this.isAdmin() ? true : value.privacyAccepted
      });
    } catch {
      return;
    }

    this.currentEditedByUser = false;
    this.resetPasswordFields();
    this.form.markAsPristine();

    this.notifications.notify('Perfil actualitzat', 'Els canvis s’han guardat correctament sobre backend real.', 'success');
  }

  protected logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login'], {
      replaceUrl: true
    });
  }

  private resetPasswordFields(): void {
    if (!this.passwordChangePolicy.shouldWipeAutofill(this.currentEditedByUser)) {
      return;
    }

    this.currentPasswordMatches.set(false);
    this.form.controls.currentPassword.setValue('', { emitEvent: false });
    this.lockNewPasswordFields();
  }

  private async verifyCurrentPassword(value: string): Promise<void> {
    const password = value.trim();
    if (!this.passwordChangePolicy.shouldVerifyTypedCurrent(this.currentEditedByUser, password)) {
      this.currentPasswordMatches.set(false);
      this.lockNewPasswordFields();
      if (!this.currentEditedByUser) {
        this.form.controls.currentPassword.setValue('', { emitEvent: false });
      }
      return;
    }

    const matches = await this.authService.verifyCurrentPassword(password);
    if (this.form.controls.currentPassword.value.trim() !== password) {
      return;
    }

    this.currentPasswordMatches.set(matches);
    if (this.passwordChangePolicy.canUnlockNewFields(matches)) {
      this.unlockNewPasswordFields();
    }
  }

  private lockNewPasswordFields(): void {
    this.form.controls.newPassword.disable({ emitEvent: false });
    this.form.controls.confirmNewPassword.disable({ emitEvent: false });
    this.form.controls.newPassword.setValue('', { emitEvent: false });
    this.form.controls.confirmNewPassword.setValue('', { emitEvent: false });
  }

  private unlockNewPasswordFields(): void {
    this.form.controls.newPassword.enable({ emitEvent: false });
    this.form.controls.confirmNewPassword.enable({ emitEvent: false });
  }

  private async setAvatarFromFile(file: File): Promise<void> {
    try {
      const dataUrl = await fileToAvatarDataUrl(file);
      this.avatarPreview.set(dataUrl);
      this.form.controls.avatarUrl.setValue(dataUrl);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'No s’ha pogut preparar la imatge.';
      this.notifications.notify('Imatge no vàlida', message, 'error');
    }
  }
}
