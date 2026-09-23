import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readdir, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import test from 'node:test';
import { chromeTarget, edgeTarget, firefoxTarget, webkitTarget } from '../../domain/browser-target.js';
import { summarizeChromeInventory } from '../../domain/chrome-inventory.js';
import { ExcelChromeScenarioInventory } from '../../infrastructure/excel/excel-chrome-scenario-inventory.js';
import { FlatpakBrowserProfileCleanup } from '../../infrastructure/playwright/flatpak-browser-profile-cleanup.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

test('Phase VIII reuses the complete scenario inventory for Edge', async () => {
  const inventory = await new ExcelChromeScenarioInventory(workbookPath, edgeTarget.name).load();
  const summary = summarizeChromeInventory(inventory);
  assert.equal(summary.scenarios, 184);
  assert.equal(summary.uniqueTestCodes, 160);
  assert.equal(summary.automatable, 179);
  assert.equal(summary.nonAutomatable, 5);
  assert.equal(new Set(inventory.map((item) => item.scenarioId)).size, 184);
  assert.ok(inventory.every((item) => item.variant === 'principal'));
});

test('Phase VIII isolates Edge state and records Edge over Blink', () => {
  assert.equal(edgeTarget.name, 'Edge');
  assert.equal(edgeTarget.engine, 'Blink');
  assert.equal(edgeTarget.runsDirectoryName, 'edge');
  for (const target of [chromeTarget, firefoxTarget, webkitTarget]) assert.notEqual(edgeTarget.runsDirectoryName, target.runsDirectoryName);
});

test('Flatpak browser cleanup removes only the profiles owned by that launch', async (context) => {
  const root = await mkdtemp(join(tmpdir(), 'zuppeto-flatpak-cleanup-'));
  context.after(async () => rm(root, { recursive: true, force: true }));
  const preexisting = 'playwright_chromiumdev_profile-PREEXISTING';
  const owned = 'playwright_chromiumdev_profile-OWNED';
  const createdLater = 'playwright_chromiumdev_profile-CREATEDLATER';
  await mkdir(join(root, preexisting));

  let closed = false;
  const cleanup = new FlatpakBrowserProfileCleanup(root);
  const browser = await cleanup.launch(async () => {
    await mkdir(join(root, owned));
    return { close: async () => { closed = true; } } as never;
  });
  await mkdir(join(root, createdLater));
  await browser.close();

  assert.equal(closed, true);
  assert.deepEqual((await readdir(root)).sort(), [createdLater, preexisting].sort());
});

test('Flatpak browser cleanup also removes an owned profile when launch fails', async (context) => {
  const root = await mkdtemp(join(tmpdir(), 'zuppeto-flatpak-launch-failure-'));
  context.after(async () => rm(root, { recursive: true, force: true }));
  const cleanup = new FlatpakBrowserProfileCleanup(root);

  await assert.rejects(
    cleanup.launch(async () => {
      await mkdir(join(root, 'playwright_chromiumdev_profile-FAILED'));
      throw new Error('launch failed');
    }),
    /launch failed/
  );
  assert.deepEqual(await readdir(root), []);
});
