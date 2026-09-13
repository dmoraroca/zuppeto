import type ExcelJS from 'exceljs';
import type { ScenarioRowKey } from '../../ports/excel-sync-gateway.js';
import { provesHeaders } from './excel-contract.js';

export interface ResolvedRow {
  readonly rowNumber: number;
  readonly result: string;
  readonly origin: string;
}

export interface Resolution {
  readonly kind: 'resolved' | 'missing' | 'ambiguous';
  readonly row?: ResolvedRow;
}

export class ExcelRowResolver {
  public resolve(sheet: ExcelJS.Worksheet, key: ScenarioRowKey): Resolution {
    const headers = headerIndex(sheet);
    const matches: ResolvedRow[] = [];
    for (let rowNumber = 2; rowNumber <= sheet.rowCount; rowNumber += 1) {
      const row = sheet.getRow(rowNumber);
      if (
        stringValue(row.getCell(headers.get(provesHeaders.testCode)!).value) === key.testCode &&
        stringValue(row.getCell(headers.get(provesHeaders.role)!).value) === key.role &&
        stringValue(row.getCell(headers.get(provesHeaders.browser)!).value) === key.browser &&
        stringValue(row.getCell(headers.get(provesHeaders.scenarioId)!).value) === key.scenarioId
      ) {
        matches.push({
          rowNumber,
          result: stringValue(row.getCell(headers.get(provesHeaders.result)!).value),
          origin: stringValue(row.getCell(headers.get(provesHeaders.resultOrigin)!).value)
        });
      }
    }
    if (matches.length === 0) return { kind: 'missing' };
    if (matches.length > 1) return { kind: 'ambiguous' };
    return { kind: 'resolved', row: matches[0] };
  }
}

export function headerIndex(sheet: ExcelJS.Worksheet): Map<string, number> {
  const indexes = new Map<string, number>();
  sheet.getRow(1).eachCell((cell, column) => indexes.set(stringValue(cell.value), column));
  return indexes;
}

export function stringValue(value: ExcelJS.CellValue): string {
  if (value === null || value === undefined) return '';
  if (typeof value === 'object' && 'text' in value) return String(value.text).trim();
  return String(value).trim();
}
