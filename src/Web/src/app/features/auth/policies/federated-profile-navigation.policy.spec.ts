import { describe, expect, it } from 'vitest';

import { shouldNavigateHomeAfterProfileSave } from './federated-profile-navigation.policy';

describe('federated profile navigation policy', () => {
  it.each(['google', 'linkedin', 'facebook', ' Google '])(
    'navigates home after a successful %s profile save',
    (provider) => {
      expect(shouldNavigateHomeAfterProfileSave(provider)).toBe(true);
    }
  );

  it.each(['password', '', null, undefined])(
    'keeps the current profile flow for %s',
    (provider) => {
      expect(shouldNavigateHomeAfterProfileSave(provider)).toBe(false);
    }
  );
});
