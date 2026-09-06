/**
 * Remembers a baseline snapshot so signal / ngModel editors can report dirty
 * the same way a pristine FormGroup does.
 */
export class EditorChangeTracker {
  private baseline = '';

  remember(value: unknown): void {
    this.baseline = serializeEditorValue(value);
  }

  hasChanges(value: unknown): boolean {
    return this.baseline !== serializeEditorValue(value);
  }
}

function serializeEditorValue(value: unknown): string {
  return JSON.stringify(normalizeEditorValue(value));
}

function normalizeEditorValue(value: unknown): unknown {
  if (value === undefined) {
    return null;
  }

  if (Array.isArray(value)) {
    return value.map((item) => normalizeEditorValue(item));
  }

  if (value && typeof value === 'object') {
    const record = value as Record<string, unknown>;
    const normalized: Record<string, unknown> = {};
    for (const key of Object.keys(record).sort()) {
      normalized[key] = normalizeEditorValue(record[key]);
    }
    return normalized;
  }

  return value;
}
