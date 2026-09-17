/** Produces one stable monogram for every Place name. */
export function placeInitials(name: string | null | undefined): string {
  const words = (name ?? '').trim().match(/[\p{L}\p{N}]+/gu) ?? [];
  if (words.length === 0) return '?';

  const initials = words.length === 1
    ? Array.from(words[0]).slice(0, 2)
    : words.slice(0, 3).map((word) => Array.from(word)[0]);

  return initials.join('').toLocaleUpperCase('ca');
}
