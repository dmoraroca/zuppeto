import assert from 'node:assert/strict';
import { resolve } from 'node:path';
import test from 'node:test';
import { chromeTarget, edgeTarget, firefoxTarget, webkitTarget } from '../../domain/browser-target.js';
import { summarizeChromeInventory } from '../../domain/chrome-inventory.js';
import { ExcelChromeScenarioInventory } from '../../infrastructure/excel/excel-chrome-scenario-inventory.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

test('Phase VIII reuses the complete scenario inventory for Edge', async () => {
  const inventory = await new ExcelChromeScenarioInventory(workbookPath, edgeTarget.name).load();
  const summary = summarizeChromeInventory(inventory);
  assert.equal(summary.scenarios, 177);
  assert.equal(summary.uniqueTestCodes, 153);
  assert.equal(summary.automatable, 172);
  assert.equal(summary.nonAutomatable, 5);
  assert.equal(new Set(inventory.map((item) => item.scenarioId)).size, 177);
  assert.ok(inventory.every((item) => item.variant === 'principal'));
});

test('Phase VIII isolates Edge state and records Edge over Blink', () => {
  assert.equal(edgeTarget.name, 'Edge');
  assert.equal(edgeTarget.engine, 'Blink');
  assert.equal(edgeTarget.runsDirectoryName, 'edge');
  for (const target of [chromeTarget, firefoxTarget, webkitTarget]) assert.notEqual(edgeTarget.runsDirectoryName, target.runsDirectoryName);
});
