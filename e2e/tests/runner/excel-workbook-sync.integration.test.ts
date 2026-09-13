import assert from 'node:assert/strict';
import { copyFile, readFile } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import test from 'node:test';
import ExcelJS from 'exceljs';
import { JsonlExecutionJournal } from '../../infrastructure/persistence/jsonl-execution-journal.js';
import { ExcelWorkbookSync } from '../../infrastructure/excel/excel-workbook-sync.js';
import { headerIndex, stringValue } from '../../infrastructure/excel/excel-row-resolver.js';
import type { ExcelExecution } from '../../ports/excel-sync-gateway.js';
import { withTemporaryDirectory } from './test-support.js';

const sourceWorkbook = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

interface ProvesRow {
  readonly rowNumber: number;
  readonly testCode: string;
  readonly role: string;
  readonly browser: string;
  readonly scenarioId: string;
  readonly result: string;
  readonly origin: string;
  readonly date: string;
  readonly observations: string;
  readonly correctionDate: string;
  readonly correctionResult: string;
  readonly completedTasks: string;
}

async function withMigratedWorkbook<T>(action: (path: string) => Promise<T>): Promise<T> {
  return withTemporaryDirectory(async (directory) => {
    const path = join(directory, 'proves.xlsx');
    await copyFile(sourceWorkbook, path);
    await new ExcelWorkbookSync(path).migrate();
    return action(path);
  });
}

async function readProvesRows(path: string): Promise<ProvesRow[]> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(path);
  const sheet = workbook.getWorksheet('Proves')!;
  const columns = headerIndex(sheet);
  const at = (row: number, header: string) => stringValue(sheet.getRow(row).getCell(columns.get(header)!).value);
  const rows: ProvesRow[] = [];
  for (let rowNumber = 2; rowNumber <= sheet.rowCount; rowNumber += 1) {
    rows.push({
      rowNumber, testCode: at(rowNumber, 'Codi prova'), role: at(rowNumber, 'Rol probat'), browser: at(rowNumber, 'Navegador'),
      scenarioId: at(rowNumber, 'Id escenari'), result: at(rowNumber, 'Resultat'), origin: at(rowNumber, 'Origen resultat'),
      date: at(rowNumber, 'Data'), observations: at(rowNumber, 'Observacions'), correctionDate: at(rowNumber, 'Data Correcció'),
      correctionResult: at(rowNumber, 'Resultat Correcció'), completedTasks: at(rowNumber, 'Tasques Fetes')
    });
  }
  return rows;
}

function execution(row: ProvesRow, executionId: string, outcome: ExcelExecution['outcome']): ExcelExecution {
  return {
    runId: 'run-phase-2', executionId, scenario: { testCode: row.testCode, role: row.role, browser: row.browser, scenarioId: row.scenarioId },
    variant: 'principal', engine: 'simulat', browserVersion: '0', environment: 'temporary', commit: 'test',
    startedAt: '2026-09-13T12:00:00.000Z', finishedAt: '2026-09-13T12:00:02.000Z', durationMs: 2000,
    attempt: 1, outcome, javascriptErrors: '', networkErrors: '', message: 'Resultat de prova Fase II.', evidencePaths: '', origin: 'LOCAL'
  };
}

function pick(rows: readonly ProvesRow[], result: string): ProvesRow {
  const row = rows.find((item) => item.result === result);
  assert.ok(row, 'Cal una fila ' + result + ' al workbook de prova.');
  return row;
}

async function executionRows(path: string): Promise<readonly string[][]> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(path);
  const sheet = workbook.getWorksheet('Execucions E2E')!;
  const values: string[][] = [];
  for (let row = 2; row <= sheet.rowCount; row += 1) values.push(rowValues(sheet, row));
  return values;
}

function rowValues(sheet: ExcelJS.Worksheet, rowNumber: number): string[] {
  return Array.from({ length: sheet.columnCount }, (_, index) => stringValue(sheet.getRow(rowNumber).getCell(index + 1).value));
}

test('migration adds only the agreed Proves columns, preserves historical values and prepares empty E2E history', async () => {
  await withMigratedWorkbook(async (path) => {
    const rows = await readProvesRows(path);
    assert.equal(rows.length, 1062);
    assert.ok(rows.every((row) => row.scenarioId === 'principal'));
    assert.ok(rows.filter((row) => row.result !== 'PENDENT').every((row) => row.origin === 'MANUAL'));
    assert.ok(rows.filter((row) => row.result === 'PENDENT').every((row) => row.origin === ''));
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.readFile(path);
    const proves = workbook.getWorksheet('Proves')!;
    const history = workbook.getWorksheet('Execucions E2E')!;
    assert.equal(proves.columnCount, 16);
    assert.equal(history.rowCount, 1);
    assert.equal(history.columnCount, 23);
    assert.deepEqual(rowValues(history, 1), [
      'runId', 'executionId', 'Id escenari', 'Codi prova', 'Rol', 'Variant', 'Navegador', 'Motor', 'Versió navegador', 'Entorn', 'Commit',
      'Inici', 'Final', 'Durada (ms)', 'Attempt', 'Resultat tècnic', 'Errors JavaScript', 'Errors HTTP/xarxa', 'Missatge',
      'Evidències/rutes relatives', 'SyncStatus', 'Origen Local/CI', 'Referència revalidació'
    ]);
  });
});

test('MANUAL, EN CURS and existing OK/KO/N/A rows are protected', async () => {
  await withMigratedWorkbook(async (path) => {
    const before = await readProvesRows(path);
    const service = new ExcelWorkbookSync(path);
    for (const result of ['OK', 'EN CURS', 'N/A']) {
      const row = pick(before, result);
      const sync = await service.synchronize(execution(row, 'protected-' + result.replace(' ', '-'), 'passed'));
      assert.equal(sync.status, 'NOT_ELIGIBLE');
    }
    const after = await readProvesRows(path);
    for (const result of ['OK', 'EN CURS', 'N/A']) {
      const original = pick(before, result);
      const current = after.find((row) => row.rowNumber === original.rowNumber)!;
      assert.deepEqual(current, original);
    }
    assert.equal((await executionRows(path)).length, 3);
  });
});

test('eligible PENDENT rows translate PASS to OK/E2E and FAIL to KO/E2E', async () => {
  await withMigratedWorkbook(async (path) => {
    const before = await readProvesRows(path);
    const pending = before.filter((row) => row.result === 'PENDENT');
    const passRow = pending[0];
    const failRow = pending[1];
    const service = new ExcelWorkbookSync(path);
    assert.equal((await service.synchronize(execution(passRow, 'eligible-pass', 'passed'))).status, 'SYNCED');
    assert.equal((await service.synchronize(execution(failRow, 'eligible-fail', 'failed'))).status, 'SYNCED');
    const after = await readProvesRows(path);
    const passed = after.find((row) => row.rowNumber === passRow.rowNumber)!;
    const failed = after.find((row) => row.rowNumber === failRow.rowNumber)!;
    assert.equal(passed.result, 'OK'); assert.equal(passed.origin, 'E2E'); assert.match(passed.observations, /eligible-pass/);
    assert.equal(failed.result, 'KO'); assert.equal(failed.origin, 'E2E'); assert.match(failed.observations, /eligible-fail/);
    assert.equal((await executionRows(path)).length, 2);
  });
});

test('BLOCKED, SKIP and INTERRUPTED append technical history but never alter Proves', async () => {
  await withMigratedWorkbook(async (path) => {
    const before = await readProvesRows(path);
    const row = pick(before, 'PENDENT');
    const service = new ExcelWorkbookSync(path);
    for (const outcome of ['blocked', 'skipped', 'interrupted'] as const) {
      const sync = await service.synchronize(execution(row, 'terminal-' + outcome, outcome));
      assert.equal(sync.status, 'SYNCED'); assert.equal(sync.provesUpdated, false);
    }
    const after = await readProvesRows(path);
    assert.deepEqual(after.find((item) => item.rowNumber === row.rowNumber), row);
    assert.equal((await executionRows(path)).length, 3);
  });
});

test('executionId is idempotent and a missing or ambiguous key never modifies Proves', async () => {
  await withMigratedWorkbook(async (path) => {
    const before = await readProvesRows(path);
    const row = pick(before, 'PENDENT');
    const service = new ExcelWorkbookSync(path);
    const first = execution(row, 'idempotent', 'passed');
    assert.equal((await service.synchronize(first)).status, 'SYNCED');
    assert.equal((await service.synchronize(first)).status, 'ALREADY_SYNCED');
    const missing = { ...first, executionId: 'missing', scenario: { ...first.scenario, testCode: 'ZUP-999' } };
    assert.equal((await service.synchronize(missing)).status, 'INTEGRITY_ERROR');
    const afterMissing = await readProvesRows(path);
    assert.equal(afterMissing.find((item) => item.rowNumber === row.rowNumber)!.result, 'OK');

    const workbook = new ExcelJS.Workbook(); await workbook.xlsx.readFile(path);
    const proves = workbook.getWorksheet('Proves')!; const columns = headerIndex(proves);
    const duplicated = proves.addRow(rowValues(proves, row.rowNumber));
    duplicated.getCell(columns.get('Resultat')!).value = 'PENDENT'; duplicated.getCell(columns.get('Origen resultat')!).value = '';
    await workbook.xlsx.writeFile(path);
    const ambiguous = { ...first, executionId: 'ambiguous', scenario: { ...first.scenario, testCode: row.testCode, role: row.role, browser: row.browser, scenarioId: row.scenarioId } };
    assert.equal((await service.synchronize(ambiguous)).status, 'INTEGRITY_ERROR');
    assert.equal((await executionRows(path)).length, 3);
  });
});

test('an Excel write error leaves JSONL intact and later resynchronization succeeds without duplication', async () => {
  await withMigratedWorkbook(async (path) => {
    const row = pick(await readProvesRows(path), 'PENDENT');
    const item = execution(row, 'recoverable-write-error', 'passed');
    const journal = new JsonlExecutionJournal(join(path, '..', 'runs'));
    await journal.append({
      executionId: item.executionId, runId: item.runId, scenarioId: item.scenario.scenarioId, attempt: item.attempt, outcome: item.outcome,
      startedAt: item.startedAt, finishedAt: item.finishedAt, durationMs: item.durationMs, message: item.message
    });
    const failing = new ExcelWorkbookSync(path, async () => { throw new Error('Simulated Excel write failure.'); });
    const failed = await failing.synchronize(item);
    assert.equal(failed.status, 'NOT_SYNCED');
    assert.equal((await journal.list(item.runId)).length, 1);
    assert.equal((await executionRows(path)).length, 0);

    const recovered = new ExcelWorkbookSync(path);
    assert.equal((await recovered.synchronize(item)).status, 'SYNCED');
    assert.equal((await recovered.synchronize(item)).status, 'ALREADY_SYNCED');
    assert.equal((await executionRows(path)).length, 1);
  });
});
