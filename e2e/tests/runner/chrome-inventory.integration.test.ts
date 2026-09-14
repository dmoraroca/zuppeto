import assert from 'node:assert/strict';
import { resolve } from 'node:path';
import test from 'node:test';
import { chromeBlocks, summarizeChromeInventory } from '../../domain/chrome-inventory.js';
import { ExcelChromeScenarioInventory } from '../../infrastructure/excel/excel-chrome-scenario-inventory.js';

test('Chrome inventory preserves all Excel scenarios and classifies explicit exclusions', async () => {
  const inventory = await new ExcelChromeScenarioInventory(resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx')).load();
  const summary = summarizeChromeInventory(inventory);
  assert.equal(summary.chromeRows, 177);
  assert.equal(summary.uniqueTestCodes, 153);
  assert.equal(summary.scenarios, 177);
  assert.equal(summary.automatable, 172);
  assert.equal(summary.nonAutomatable, 5);
  assert.equal(Object.keys(summary.byBlock).length, chromeBlocks.length);
  assert.equal(new Set(inventory.map((item) => item.scenarioId)).size, 177);
  assert.ok(inventory.every((item) => item.tags.includes(`@${item.testCode}`) && item.fixture.length > 0));
});
