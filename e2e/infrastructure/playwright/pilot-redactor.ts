export function redact(value: string): string {
  return value
    .replace(/(bearer\s+)\S+/gi, '$1[REDACTED]')
    .replace(/(authorization\s*[:=]\s*)\S+/gi, '$1[REDACTED]')
    .replace(/((?:password|token|api[_-]?key|cookie|set-cookie|access[_-]?token|refresh[_-]?token)\s*[:=]\s*)[^\s,&]+/gi, '$1[REDACTED]');
}

export function safeUrl(value: string): string {
  try {
    const url = new URL(value);
    return url.origin + url.pathname;
  } catch {
    return redact(value.split('?')[0]);
  }
}
