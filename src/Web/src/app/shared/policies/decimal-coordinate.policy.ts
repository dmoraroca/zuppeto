/** Optional signed decimal for lat/lng. Letters and extra symbols are dropped. */
export function sanitizeDecimalCoordinate(raw: string): string {
  let result = '';
  let hasSeparator = false;

  for (const character of raw.trimStart()) {
    if (character === '-' && result.length === 0) {
      result = '-';
      continue;
    }

    if ((character === '.' || character === ',') && !hasSeparator) {
      hasSeparator = true;
      result += '.';
      continue;
    }

    if (character >= '0' && character <= '9') {
      result += character;
    }
  }

  return result;
}

export function isDecimalCoordinateKey(key: string): boolean {
  return key.length === 1 && /[0-9.,\-]/.test(key);
}

/** Finished value: always a decimal (21 → 21.0). Empty and in-progress stay as typed. */
export function formatDecimalCoordinate(raw: string): string {
  const sanitized = sanitizeDecimalCoordinate(raw);
  if (!sanitized || sanitized === '-' || sanitized === '.' || sanitized === '-.') {
    return sanitized;
  }

  const value = Number(sanitized);
  if (!Number.isFinite(value)) {
    return sanitized;
  }

  return sanitized.includes('.') ? sanitized : `${sanitized}.0`;
}
