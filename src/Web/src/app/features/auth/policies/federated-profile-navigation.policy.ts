const PROFILE_COMPLETION_PROVIDERS = new Set(['google', 'linkedin', 'facebook']);

export function shouldNavigateHomeAfterProfileSave(provider: string | null | undefined): boolean {
  return PROFILE_COMPLETION_PROVIDERS.has(provider?.trim().toLowerCase() ?? '');
}
