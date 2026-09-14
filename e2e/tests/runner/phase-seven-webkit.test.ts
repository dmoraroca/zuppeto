import assert from 'node:assert/strict';
import { copyFile, mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import test from 'node:test';
import ExcelJS from 'exceljs';
import { chromeTarget, firefoxTarget, webkitTarget } from '../../domain/browser-target.js';
import { summarizeChromeInventory } from '../../domain/chrome-inventory.js';
import { ExcelBrowserMatrixProvisioner } from '../../infrastructure/excel/excel-browser-matrix-provisioner.js';
import { ExcelChromeScenarioInventory } from '../../infrastructure/excel/excel-chrome-scenario-inventory.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

test('Phase VII reuses the complete scenario inventory for WebKit', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'zuppeto-webkit-inventory-'));
  const path = join(directory, 'matrix.xlsx');
  try {
    await copyFile(workbookPath, path);
    await new ExcelBrowserMatrixProvisioner(path).provision('Chrome', 'WebKit');
    const inventory = await new ExcelChromeScenarioInventory(path, webkitTarget.name).load();
    const summary = summarizeChromeInventory(inventory);
    assert.equal(summary.scenarios, 177);
    assert.equal(summary.uniqueTestCodes, 153);
    assert.equal(summary.automatable, 172);
    assert.equal(summary.nonAutomatable, 5);
    assert.equal(new Set(inventory.map((item) => item.scenarioId)).size, 177);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

test('Phase VII isolates WebKit resume state and records WebKit metadata', () => {
  assert.equal(webkitTarget.engine, 'WebKit');
  assert.equal(webkitTarget.name, 'WebKit');
  assert.equal(webkitTarget.runsDirectoryName, 'webkit');
  assert.notEqual(webkitTarget.runsDirectoryName, chromeTarget.runsDirectoryName);
  assert.notEqual(webkitTarget.runsDirectoryName, firefoxTarget.runsDirectoryName);
});

test('WebKit matrix provisioning is exact, idempotent and preserves existing rows', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'zuppeto-webkit-matrix-'));
  const path = join(directory, 'matrix.xlsx');
  try {
    await copyFile(workbookPath, path);
    await removeBrowserRows(path, 'WebKit');
    const before = await rows(path);
    const provisioner = new ExcelBrowserMatrixProvisioner(path);
    const first = await provisioner.provision('Chrome', 'WebKit');
    const second = await provisioner.provision('Chrome', 'WebKit');
    const after = await rows(path);
    assert.equal(first.created, 177);
    assert.deepEqual(second, { created: 0, existing: 177 });
    assert.deepEqual(after.filter((row) => row.browser !== 'WebKit'), before);
    const webkit = after.filter((row) => row.browser === 'WebKit');
    assert.equal(webkit.length, 177);
    assert.ok(webkit.every((row) => row.result === 'PENDENT' && row.origin === ''));
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

async function removeBrowserRows(path: string, browser: string): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(path);
  const sheet = workbook.getWorksheet('Proves')!;
  const headers = new Map<string, number>();
  sheet.getRow(1).eachCell((cell, column) => headers.set(String(cell.value).trim(), column));
  for (let rowNumber = sheet.rowCount; rowNumber >= 2; rowNumber -= 1) {
    if (String(sheet.getRow(rowNumber).getCell(headers.get('Navegador')!).text).trim() === browser) sheet.spliceRows(rowNumber, 1);
  }
  await workbook.xlsx.writeFile(path);
}

async function rows(path: string): Promise<readonly { readonly key: string; readonly browser: string; readonly result: string; readonly origin: string; readonly raw: string }[]> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(path);
  const sheet = workbook.getWorksheet('Proves')!;
  const headers = new Map<string, number>();
  sheet.getRow(1).eachCell((cell, column) => headers.set(String(cell.value).trim(), column));
  const value = (row: ExcelJS.Row, header: string) => String(row.getCell(headers.get(header)!).text ?? '').trim();
  const result = [];
  for (let rowNumber = 2; rowNumber <= sheet.rowCount; rowNumber += 1) {
    const row = sheet.getRow(rowNumber);
    result.push({
      key: [value(row, 'Codi prova'), value(row, 'Rol probat'), value(row, 'Navegador'), value(row, 'Id escenari')].join('|'),
      browser: value(row, 'Navegador'), result: value(row, 'Resultat'), origin: value(row, 'Origen resultat'), raw: JSON.stringify(row.values)
    });
  }
  return result;
}
