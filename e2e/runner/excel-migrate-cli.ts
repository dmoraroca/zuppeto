import { resolve } from 'node:path';
import { ExcelWorkbookSync } from '../infrastructure/excel/excel-workbook-sync.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

async function main(): Promise<void> {
  await new ExcelWorkbookSync(workbookPath).migrate();
  console.log('Migració estructural d’Excel completada: ' + workbookPath);
}

void main();
