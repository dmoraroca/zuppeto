import ExcelJS from 'exceljs';
import { AtomicWorkbookWriter, WorkbookWriteLock, type WorkbookWrite } from './atomic-workbook-writer.js';
import { pendingResult, provesHeaders } from './excel-contract.js';
import { headerIndex, stringValue } from './excel-row-resolver.js';

export interface BrowserMatrixProvisionResult {
  readonly created: number;
  readonly existing: number;
}

export class ExcelBrowserMatrixProvisioner {
  private readonly lock: WorkbookWriteLock;
  private readonly write: WorkbookWrite;

  public constructor(private readonly workbookPath: string, writer: WorkbookWrite = (workbook, path) => new AtomicWorkbookWriter().write(workbook, path)) {
    this.lock = new WorkbookWriteLock(workbookPath);
    this.write = writer;
  }

  public async provision(sourceBrowser: string, targetBrowser: string): Promise<BrowserMatrixProvisionResult> {
    if (!sourceBrowser || !targetBrowser || sourceBrowser === targetBrowser) throw new Error('La matriu requereix navegadors origen i destí diferents.');
    return this.lock.execute(async () => {
      const workbook = new ExcelJS.Workbook();
      await workbook.xlsx.readFile(this.workbookPath);
      const sheet = workbook.getWorksheet('Proves');
      if (sheet === undefined) throw new Error('No existeix el full Proves.');
      const columns = headerIndex(sheet);
      const browserColumn = required(columns, provesHeaders.browser);
      const sourceRows: ExcelJS.Row[] = [];
      const targetRows: ExcelJS.Row[] = [];
      for (let rowNumber = 2; rowNumber <= sheet.rowCount; rowNumber += 1) {
        const row = sheet.getRow(rowNumber);
        const browser = stringValue(row.getCell(browserColumn).value);
        if (browser === sourceBrowser) sourceRows.push(row);
        if (browser === targetBrowser) targetRows.push(row);
      }
      assertUnique(sourceRows, columns, sourceBrowser);
      if (sourceRows.length === 0) throw new Error(`La matriu origen ${sourceBrowser} és buida.`);
      if (targetRows.length > 0) {
        assertEquivalent(sourceRows, targetRows, columns, sourceBrowser, targetBrowser);
        return { created: 0, existing: targetRows.length };
      }
      for (const source of sourceRows) {
        const values = Array.from({ length: sheet.columnCount }, (_, index) => source.getCell(index + 1).value);
        const target = sheet.addRow(values);
        target.getCell(browserColumn).value = targetBrowser;
        target.getCell(required(columns, provesHeaders.result)).value = pendingResult;
        target.getCell(required(columns, provesHeaders.resultOrigin)).value = '';
        for (const header of [provesHeaders.date, provesHeaders.observations, provesHeaders.correctionDate, provesHeaders.correctionResult, provesHeaders.completedTasks]) {
          target.getCell(required(columns, header)).value = '';
        }
      }
      await this.write(workbook, this.workbookPath);
      return { created: sourceRows.length, existing: 0 };
    });
  }
}

function assertEquivalent(sourceRows: readonly ExcelJS.Row[], targetRows: readonly ExcelJS.Row[], columns: ReadonlyMap<string, number>, sourceBrowser: string, targetBrowser: string): void {
  assertUnique(targetRows, columns, targetBrowser);
  const sourceKeys = new Set(sourceRows.map((row) => key(row, columns)));
  const targetKeys = new Set(targetRows.map((row) => key(row, columns)));
  if (sourceKeys.size !== targetKeys.size || [...sourceKeys].some((value) => !targetKeys.has(value))) {
    throw new Error(`La matriu ${targetBrowser} existent no és equivalent a ${sourceBrowser}.`);
  }
}

function assertUnique(rows: readonly ExcelJS.Row[], columns: ReadonlyMap<string, number>, browser: string): void {
  const keys = rows.map((row) => key(row, columns));
  if (new Set(keys).size !== keys.length) throw new Error(`La matriu ${browser} conté claus d’escenari duplicades.`);
}

function key(row: ExcelJS.Row, columns: ReadonlyMap<string, number>): string {
  return [provesHeaders.testCode, provesHeaders.role, provesHeaders.scenarioId]
    .map((header) => stringValue(row.getCell(required(columns, header)).value)).join('\u0000');
}

function required(columns: ReadonlyMap<string, number>, header: string): number {
  const column = columns.get(header);
  if (column === undefined) throw new Error(`Falta la columna requerida ${header}.`);
  return column;
}
