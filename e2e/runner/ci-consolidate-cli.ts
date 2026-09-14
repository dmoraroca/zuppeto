import { copyFile, mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { CiResultConsolidator } from '../infrastructure/ci/ci-result-consolidator.js';
import { ExcelWorkbookSync } from '../infrastructure/excel/excel-workbook-sync.js';

const read = (name: string): string | undefined => process.argv.slice(2).find((arg) => arg.startsWith(`${name}=`))?.slice(name.length + 1);
const required = (name: string): string => {
  const value = read(name);
  if (!value) throw new Error(`Falta ${name}.`);
  return resolve(value);
};

async function main(): Promise<void> {
  const artifacts = required('--artifacts');
  const source = required('--workbook');
  const output = required('--output');
  const summary = required('--summary');
  await mkdir(dirname(output), { recursive: true });
  await copyFile(source, output);
  const result = await new CiResultConsolidator(new ExcelWorkbookSync(output)).consolidate(artifacts, summary);
  console.log(`Consolidació CI completada: ${result.runs} runs, ${result.executions} execucions, ${result.alreadyPresent} ja existents.`);
}

void main().catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut consolidant resultats CI.');
  process.exitCode = 1;
});
