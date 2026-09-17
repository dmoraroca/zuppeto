const GOOGLE_AVATAR_HOST_SUFFIX = '.googleusercontent.com';

export function normalizePetilocAvatarUrl(value: string | null | undefined): string | null {
  const normalized = value?.trim();

  if (!normalized || isGoogleAvatarUrl(normalized)) {
    return null;
  }

  return normalized;
}

export function hasPetilocAvatar(value: string | null | undefined): boolean {
  return normalizePetilocAvatarUrl(value) !== null;
}

function isGoogleAvatarUrl(value: string): boolean {
  try {
    const hostname = new URL(value).hostname.toLowerCase();
    return hostname === 'googleusercontent.com' || hostname.endsWith(GOOGLE_AVATAR_HOST_SUFFIX);
  } catch {
    return false;
  }
}
