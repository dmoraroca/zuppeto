import { MappingProjectionDraft } from '../models/territorial-admin.model';

export const MAX_TERRITORIAL_FILE_SIZE = 25 * 1024 * 1024;

export function territorialFileError(file: File | null): string | null {
  if (!file) return 'Selecciona un fitxer XLSX.';
  if (!file.name.toLowerCase().endsWith('.xlsx')) return 'Només s’admeten fitxers XLSX.';
  if (file.size <= 0) return 'El fitxer és buit.';
  if (file.size > MAX_TERRITORIAL_FILE_SIZE) return 'El fitxer supera el màxim de 25 MB.';
  return null;
}

function value(columns: string[]): unknown {
  const clean = columns.map((item) => item.trim()).filter(Boolean);
  if (clean.length === 1) return { operation: 'Column', column: clean[0] };
  return {
    operation: 'Concat',
    separator: ':',
    parts: clean.map((column) => ({ operation: 'Column', column }))
  };
}

export function mappingDraftError(draft: MappingProjectionDraft): string | null {
  if (!draft.sheet || !draft.unitTypeCode || !draft.nameColumn) return 'Full, tipus i nom són obligatoris.';
  if (!draft.canonicalColumns.length || !draft.codeColumns.length || !draft.codeScheme.trim()) {
    return 'La clau canònica i el codi oficial són obligatoris.';
  }
  return null;
}

export function buildMappingDefinition(drafts: MappingProjectionDraft[]): unknown {
  if (!drafts.length || drafts.some(mappingDraftError)) throw new Error('El mapping visual és incomplet.');
  const grouped = new Map<string, MappingProjectionDraft[]>();
  drafts.forEach((draft) => grouped.set(draft.sheet, [...(grouped.get(draft.sheet) ?? []), draft]));
  return {
    canonicalizer: 'default',
    sheets: [...grouped.entries()].map(([sheet, units]) => ({
      sheet,
      headerRow: units[0].headerRow,
      units: units.map((draft) => ({
        territorialUnitTypeCode: draft.unitTypeCode,
        canonicalUnitKey: value(draft.canonicalColumns),
        parentCanonicalUnitKey: draft.parentColumns.length ? value(draft.parentColumns) : null,
        name: { operation: 'Column', column: draft.nameColumn },
        locale: draft.locale || null,
        nameKind: 'Official',
        codes: [{
          scheme: draft.codeScheme.trim(),
          value: value(draft.codeColumns),
          isPrimary: true
        }],
        longitude: draft.longitudeColumn
          ? { operation: 'Column', column: draft.longitudeColumn } : null,
        latitude: draft.latitudeColumn
          ? { operation: 'Column', column: draft.latitudeColumn } : null,
        zeroZeroIsSentinel: draft.zeroZeroIsSentinel
      }))
    }))
  };
}
