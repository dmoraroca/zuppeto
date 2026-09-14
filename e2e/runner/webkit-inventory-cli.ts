import { resolve } from 'node:path';
import { ExcelBrowserMatrixProvisioner } from '../infrastructure/excel/excel-browser-matrix-provisioner.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

void new ExcelBrowserMatrixProvisioner(workbookPath).provision('Chrome', 'WebKit').then((result) => {
  console.log(result.created > 0
    ? `Matriu WebKit creada amb ${result.created} escenaris PENDENT.`
    : `Matriu WebKit ja existent i validada amb ${result.existing} escenaris.`);
}).catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut preparant la matriu WebKit.');
  process.exitCode = 1;
});
