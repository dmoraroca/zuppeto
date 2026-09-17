import { DirtyAndValidFormCommitPolicy } from '../../../shared/policies/form-commit.policy';
import { RecommendedPasswordStrengthPolicy } from './password-strength.policy';
import { CatalogProfileSavePolicy, ProfileFormSnapshot } from './profile-save.policy';

describe('CatalogProfileSavePolicy', () => {
  const policy = new CatalogProfileSavePolicy(
    new RecommendedPasswordStrengthPolicy(),
    new DirtyAndValidFormCommitPolicy()
  );

  const completeUserProfile = (overrides: Partial<ProfileFormSnapshot> = {}): ProfileFormSnapshot => ({
    name: 'Usuari Google',
    email: 'google@example.test',
    originalEmail: 'google@example.test',
    emailInvalid: false,
    currentPassword: '',
    currentPasswordMatches: false,
    newPassword: '',
    confirmNewPassword: '',
    city: 'Barcelona',
    country: 'Espanya',
    comments: '',
    privacyAccepted: true,
    isAdmin: false,
    isPristine: false,
    ...overrides
  });

  it('allows saving a complete changed profile with empty optional comments', () => {
    const snapshot = completeUserProfile({ comments: '' });

    expect(policy.missingRequiredLabels(snapshot)).toEqual([]);
    expect(policy.canSave(snapshot)).toBe(true);
  });

  it('reports privacy consent as missing for a non-admin profile', () => {
    const snapshot = completeUserProfile({ privacyAccepted: false });

    expect(policy.missingRequiredLabels(snapshot)).toContain('Consentiment de privacitat');
    expect(policy.canSave(snapshot)).toBe(false);
  });
});
