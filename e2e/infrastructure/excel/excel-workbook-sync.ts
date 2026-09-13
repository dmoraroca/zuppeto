import ExcelJS from 'exceljs';
import type { ExcelExecution, ExcelSyncGateway, ExcelSyncResult } from '../../ports/excel-sync-gateway.js';
import { AtomicWorkbookWriter, WorkbookWriteLock, type WorkbookWrite } from './atomic-workbook-writer.js';
import { e2eOrigin, executionsHeaders, manualOrigin, pendingResult, provesHeaders } from './excel-contract.js';
import { ExcelRowResolver, headerIndex, stringValue } from './excel-row-resolver.js';

const principalScenarioId = 'principal';

export class ExcelWorkbookSync implements ExcelSyncGateway {
  private readonly lock: WorkbookWriteLock;
  private readonly write: WorkbookWrite;
  private readonly resolver = new ExcelRowResolver();

  public constructor(private readonly workbookPath: string, writer: WorkbookWrite = (workbook, path) => new AtomicWorkbookWriter().write(workbook, path)) {
    this.lock = new WorkbookWriteLock(workbookPath);
    this.write = writer;
  }

  public async migrate(): Promise<void> {
    await this.lock.execute(async () => {
      const workbook = await this.load();
      const proves = requiredSheet(workbook, 'Proves');
      const executions = requiredSheet(workbook, 'Execucions E2E');
      migrateProves(proves);
      migrateExecutions(executions);
      await this.write(workbook, this.workbookPath);
    });
  }

  public async synchronize(execution: ExcelExecution): Promise<ExcelSyncResult> {
    try {
      return await this.lock.execute(async () => {
        const workbook = await this.load();
        const proves = requiredSheet(workbook, 'Proves');
        const executions = requiredSheet(workbook, 'Execucions E2E');
        assertMigrated(proves, executions);
        const executionColumns = headerIndex(executions);
        const existing = findExecution(executions, executionColumns.get('executionId')!, execution.executionId);
        if (existing !== undefined) {
          return { status: 'ALREADY_SYNCED', executionRecorded: false, provesUpdated: false, message: 'executionId ja existent a Execucions E2E.' };
        }

        const provesResult = applyProvesResult(proves, execution, this.resolver);
        const syncStatus = provesResult.status === 'INTEGRITY_ERROR' ? 'INTEGRITY_ERROR' : 'SYNCED';
        appendExecution(executions, execution, syncStatus);
        await this.write(workbook, this.workbookPath);
        return { ...provesResult, executionRecorded: true };
      });
    } catch (error) {
      return {
        status: 'NOT_SYNCED', executionRecorded: false, provesUpdated: false,
        message: error instanceof Error ? error.message : 'Error desconegut escrivint l’Excel.'
      };
    }
  }

  private async load(): Promise<ExcelJS.Workbook> {
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.readFile(this.workbookPath);
    return workbook;
  }
}

function migrateProves(sheet: ExcelJS.Worksheet): void {
  const columns = headerIndex(sheet);
  const scenarioColumn = ensureHeader(sheet, columns, provesHeaders.scenarioId);
  const originColumn = ensureHeader(sheet, columns, provesHeaders.resultOrigin);
  const resultColumn = requiredColumn(columns, provesHeaders.result);
  for (let rowNumber = 2; rowNumber <= sheet.rowCount; rowNumber += 1) {
    const row = sheet.getRow(rowNumber);
    if (stringValue(row.getCell(scenarioColumn).value) === '') row.getCell(scenarioColumn).value = principalScenarioId;
    const result = stringValue(row.getCell(resultColumn).value);
    if (result !== '' && result !== pendingResult && stringValue(row.getCell(originColumn).value) === '') {
      row.getCell(originColumn).value = manualOrigin;
    }
  }
}

function migrateExecutions(sheet: ExcelJS.Worksheet): void {
  if (sheet.rowCount > 1) throw new Error('La migració d’Execucions E2E requereix un historial buit.');
  executionsHeaders.forEach((header, index) => { sheet.getCell(1, index + 1).value = header; });
  if (sheet.columnCount > executionsHeaders.length) {
    for (let column = executionsHeaders.length + 1; column <= sheet.columnCount; column += 1) sheet.getColumn(column).hidden = true;
  }
  sheet.autoFilter = { from: 'A1', to: String.fromCharCode(64 + executionsHeaders.length) + '1' };
}

function applyProvesResult(sheet: ExcelJS.Worksheet, execution: ExcelExecution, resolver: ExcelRowResolver): Omit<ExcelSyncResult, 'executionRecorded'> {
  if (execution.outcome !== 'passed' && execution.outcome !== 'failed') {
    return { status: 'SYNCED', provesUpdated: false, message: execution.outcome + ' no modifica Proves.' };
  }
  const resolution = resolver.resolve(sheet, execution.scenario);
  if (resolution.kind === 'missing') return { status: 'INTEGRITY_ERROR', provesUpdated: false, message: 'Cap fila de Proves coincideix amb la clau de l’escenari.' };
  if (resolution.kind === 'ambiguous') return { status: 'INTEGRITY_ERROR', provesUpdated: false, message: 'Més d’una fila de Proves coincideix amb la clau de l’escenari.' };
  if (resolution.row!.origin === manualOrigin || resolution.row!.result !== pendingResult) {
    return { status: 'NOT_ELIGIBLE', provesUpdated: false, message: 'La fila de Proves està protegida i no és elegible per E2E.' };
  }
  const columns = headerIndex(sheet);
  const row = sheet.getRow(resolution.row!.rowNumber);
  row.getCell(requiredColumn(columns, provesHeaders.result)).value = execution.outcome === 'passed' ? 'OK' : 'KO';
  row.getCell(requiredColumn(columns, provesHeaders.resultOrigin)).value = e2eOrigin;
  row.getCell(requiredColumn(columns, provesHeaders.date)).value = execution.finishedAt.slice(0, 10);
  const observationColumn = requiredColumn(columns, provesHeaders.observations);
  const originalObservation = stringValue(row.getCell(observationColumn).value);
  const reference = 'E2E ' + execution.executionId + ': ' + execution.message;
  row.getCell(observationColumn).value = originalObservation === '' ? reference : originalObservation + '\n' + reference;
  return { status: 'SYNCED', provesUpdated: true, message: 'Fila elegible de Proves actualitzada per E2E.' };
}

function appendExecution(sheet: ExcelJS.Worksheet, execution: ExcelExecution, syncStatus: 'SYNCED' | 'INTEGRITY_ERROR'): void {
  sheet.addRow([
    execution.runId, execution.executionId, execution.scenario.scenarioId, execution.scenario.testCode, execution.scenario.role,
    execution.variant, execution.scenario.browser, execution.engine, execution.browserVersion, execution.environment,
    execution.commit, execution.startedAt, execution.finishedAt, execution.durationMs, execution.attempt, execution.outcome.toUpperCase(),
    execution.javascriptErrors, execution.networkErrors, execution.message, execution.evidencePaths, syncStatus, execution.origin,
    execution.revalidationReference ?? ''
  ]);
}

function assertMigrated(proves: ExcelJS.Worksheet, executions: ExcelJS.Worksheet): void {
  const provesColumns = headerIndex(proves);
  requiredColumn(provesColumns, provesHeaders.scenarioId);
  requiredColumn(provesColumns, provesHeaders.resultOrigin);
  const executionColumns = headerIndex(executions);
  for (const header of executionsHeaders) requiredColumn(executionColumns, header);
}

function ensureHeader(sheet: ExcelJS.Worksheet, columns: Map<string, number>, header: string): number {
  const existing = columns.get(header);
  if (existing !== undefined) return existing;
  const column = sheet.columnCount + 1;
  sheet.getCell(1, column).value = header;
  columns.set(header, column);
  return column;
}

function requiredColumn(columns: Map<string, number>, header: string): number {
  const column = columns.get(header);
  if (column === undefined) throw new Error('Falta la columna requerida: ' + header);
  return column;
}

function requiredSheet(workbook: ExcelJS.Workbook, name: string): ExcelJS.Worksheet {
  const sheet = workbook.getWorksheet(name);
  if (sheet === undefined) throw new Error('Falta el full requerit: ' + name);
  return sheet;
}

function findExecution(sheet: ExcelJS.Worksheet, executionIdColumn: number, executionId: string): number | undefined {
  for (let row = 2; row <= sheet.rowCount; row += 1) {
    if (stringValue(sheet.getRow(row).getCell(executionIdColumn).value) === executionId) return row;
  }
  return undefined;
}
