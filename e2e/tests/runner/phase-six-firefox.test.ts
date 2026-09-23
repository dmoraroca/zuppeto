import assert from 'node:assert/strict';
import { resolve } from 'node:path';
import test from 'node:test';
import { chromeTarget, firefoxTarget } from '../../domain/browser-target.js';
import { summarizeChromeInventory } from '../../domain/chrome-inventory.js';
import { ExcelChromeScenarioInventory } from '../../infrastructure/excel/excel-chrome-scenario-inventory.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

test('Phase VI reuses the complete scenario inventory for Firefox', async () => {
  const inventory = await new ExcelChromeScenarioInventory(workbookPath, firefoxTarget.name).load();
  const summary = summarizeChromeInventory(inventory);
  assert.equal(summary.scenarios, 184);
  assert.equal(summary.uniqueTestCodes, 160);
  assert.equal(summary.automatable, 179);
  assert.equal(summary.nonAutomatable, 5);
  assert.equal(new Set(inventory.map((item) => item.scenarioId)).size, 184);
});

test('Phase VI isolates Firefox resume state and records Gecko metadata', () => {
  assert.equal(firefoxTarget.engine, 'Gecko');
  assert.equal(firefoxTarget.name, 'Firefox');
  assert.equal(firefoxTarget.runsDirectoryName, 'firefox');
  assert.equal(chromeTarget.runsDirectoryName, undefined);
});
