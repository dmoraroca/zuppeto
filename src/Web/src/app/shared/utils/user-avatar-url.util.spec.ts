import { describe, expect, it } from 'vitest';

import { hasPetilocAvatar, normalizePetilocAvatarUrl } from './user-avatar-url.util';

describe('user avatar URL policy', () => {
  it('treats a Google picture or monogram as unavailable in Petiloc', () => {
    const googleAvatar = 'https://lh3.googleusercontent.com/a/example=s96-c';

    expect(hasPetilocAvatar(googleAvatar)).toBe(false);
    expect(normalizePetilocAvatarUrl(googleAvatar)).toBeNull();
  });

  it('keeps an avatar uploaded to Petiloc', () => {
    const uploadedAvatar = 'data:image/png;base64,petiloc-avatar';

    expect(hasPetilocAvatar(uploadedAvatar)).toBe(true);
    expect(normalizePetilocAvatarUrl(uploadedAvatar)).toBe(uploadedAvatar);
  });

  it('treats null, empty and whitespace values as unavailable', () => {
    expect(hasPetilocAvatar(null)).toBe(false);
    expect(hasPetilocAvatar('')).toBe(false);
    expect(hasPetilocAvatar('   ')).toBe(false);
  });
});
