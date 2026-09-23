import { describe, expect, it } from 'vitest';

import {
  MAX_TERRITORIAL_FILE_SIZE,
  buildMappingDefinition,
  mappingDraftError,
  territorialFileError
} from './territorial-mapping.policy';

describe('territorial mapping policy', () => {
  it('accepts a bounded xlsx artifact and rejects unsafe inputs', () => {
    expect(territorialFileError(new File(['ok'], 'àmbits.xlsx'))).toBeNull();
    expect(territorialFileError(new File(['x'], 'àmbits.csv'))).toContain('XLSX');
    expect(territorialFileError(new File([new Uint8Array(MAX_TERRITORIAL_FILE_SIZE + 1)], 'big.xlsx')))
      .toContain('25 MB');
  });

  it('builds grouped projections with composite and parent keys', () => {
    const definition = buildMappingDefinition([
      {
        sheet: 'Municipis', headerRow: 2, unitTypeCode: 'MUNICIPALITY',
        nameColumn: 'NOM', canonicalColumns: ['PROV', 'CODI'], parentColumns: ['PROV'],
        codeScheme: 'official:municipality', codeColumns: ['CODI'], locale: 'ca-ES',
        longitudeColumn: 'LON', latitudeColumn: 'LAT', zeroZeroIsSentinel: true
      },
      {
        sheet: 'Municipis', headerRow: 2, unitTypeCode: 'PROVINCE',
        nameColumn: 'PROV_NOM', canonicalColumns: ['PROV'], parentColumns: [],
        codeScheme: 'official:province', codeColumns: ['PROV'], locale: 'ca-ES',
        longitudeColumn: '', latitudeColumn: '', zeroZeroIsSentinel: false
      }
    ]) as { sheets: Array<{ units: Array<{ locale: string }> }> };

    expect(definition.sheets).toHaveLength(1);
    expect(definition.sheets[0].units).toHaveLength(2);
    expect(JSON.stringify(definition)).toContain('Concat');
    expect(definition.sheets[0].units[0].locale).toBe('ca-ES');
  });

  it('requires a real identity mapping', () => {
    expect(mappingDraftError({
      sheet: 'Dades', headerRow: 1, unitTypeCode: 'CITY', nameColumn: 'NOM',
      canonicalColumns: [], parentColumns: [], codeScheme: '', codeColumns: [],
      locale: 'de-DE', longitudeColumn: '', latitudeColumn: '', zeroZeroIsSentinel: false
    })).toContain('clau canònica');
    expect(() => buildMappingDefinition([])).toThrow();
  });
});
