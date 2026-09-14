import { resolve } from 'node:path';
import { summarizeChromeInventory } from '../domain/chrome-inventory.js';
import { ExcelChromeScenarioInventory } from '../infrastructure/excel/excel-chrome-scenario-inventory.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

void new ExcelChromeScenarioInventory(workbookPath).load()
  .then((items) => console.log(JSON.stringify({ summary: summarizeChromeInventory(items), exclusions: items.filter((item) => !item.automatable) }, null, 2)))
  .catch((error) => { console.error(error instanceof Error ? error.message : 'Error desconegut de l’inventari Chrome.'); process.exitCode = 1; });
