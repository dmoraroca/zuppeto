import ExcelJS from 'exceljs';
import JSZip from 'jszip';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const sourcePath = path.join(root, 'docs', 'idiomes', 'municipis per pais', 'Petiloc_Espanya_20260101_CNIG_Coordenades.xlsx');
const outputPath = path.join(root, 'docs', 'idiomes', 'municipis per pais', 'Petiloc_Espanya_20260101_CNIG_Coordenades_Contracte_1_0.xlsx');

const decodeXml = (value) => value
  .replaceAll('&lt;', '<').replaceAll('&gt;', '>').replaceAll('&quot;', '"')
  .replaceAll('&apos;', "'").replaceAll('&amp;', '&');
const zip = await JSZip.loadAsync(await readFile(sourcePath));
const sharedXml = (await zip.file('xl/sharedStrings.xml')?.async('text') ?? '').replaceAll('x:', '');
const sharedStrings = [...sharedXml.matchAll(/<si\b[^>]*>([\s\S]*?)<\/si>/g)]
  .map(match => decodeXml([...match[1].matchAll(/<t\b[^>]*>([\s\S]*?)<\/t>/g)].map(value => value[1]).join('')));
const rawSheetXml = await zip.file('xl/worksheets/sheet2.xml')?.async('text');
if (!rawSheetXml) throw new Error('No existeix el full Municipis.');
const sheetXml = rawSheetXml.replaceAll('x:', '');
const municipalityRows = [...sheetXml.matchAll(/<row\b[^>]*r="(\d+)"[^>]*>([\s\S]*?)<\/row>/g)].map(row => {
  const values = new Map();
  for (const cell of row[2].matchAll(/<c\b([^>]*)>([\s\S]*?)<\/c>/g)) {
    const reference = /\br="([A-Z]+)\d+"/.exec(cell[1])?.[1];
    const type = /\bt="([^"]+)"/.exec(cell[1])?.[1];
    const raw = /<v>([\s\S]*?)<\/v>/.exec(cell[2])?.[1] ?? '';
    const inline = [...cell[2].matchAll(/<t\b[^>]*>([\s\S]*?)<\/t>/g)].map(value => value[1]).join('');
    if (reference) values.set(reference, type === 's' ? sharedStrings[Number(raw)] : decodeXml(type === 'inlineStr' ? inline : raw));
  }
  return { number: Number(row[1]), values };
});

const workbook = new ExcelJS.Workbook();
workbook.creator = 'Zuppeto';
workbook.created = new Date('2026-01-01T00:00:00Z');

function addTable(name, headers, rows) {
  const sheet = workbook.addWorksheet(name);
  sheet.addRow(headers);
  for (const row of rows) sheet.addRow(row);
  sheet.views = [{ state: 'frozen', ySplit: 1 }];
  sheet.getRow(1).font = { bold: true };
  sheet.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: headers.length } };
  return sheet;
}

addTable('Manifest', ['key', 'value'], [
  ['contract_id', 'PETILOC_TERRITORIAL'],
  ['contract_version', '1.0'],
  ['country_iso2', 'ES'],
  ['import_mode', 'PATCH'],
  ['dataset_version', '2026-01-01-CNIG-coordinates'],
  ['territorial_date', '2026-01-01'],
  ['primary_source_key', 'CNIG_NGMEP_2026'],
  ['field_scope', 'coordinates'],
  ['coverage', 'COUNTRY']
]);

addTable('Fonts', ['source_key', 'organisation', 'dataset', 'dataset_version', 'territorial_date', 'url', 'license', 'license_url', 'attribution'], [
  ['ES-INE-30247', 'Instituto Nacional de Estadística (INE)', 'Relación de municipios y sus códigos por provincias', '2026-01-01', '2026-01-01', 'https://www.ine.es/dyngs/INEbase/operacion.htm?c=Estadistica_C&cid=1254736177031&idp=1254734710990', 'CC BY 4.0', 'https://creativecommons.org/licenses/by/4.0/', 'Instituto Nacional de Estadística'],
  ['CNIG_NGMEP_2026', 'Instituto Geográfico Nacional (IGN) / Centro Nacional de Información Geográfica (CNIG)', 'Nomenclátor Geográfico de Municipios y Entidades de Población', '2026', '2026-01-01', 'https://centrodedescargas.cnig.es/CentroDescargas/detalleArchivo?sec=9000004', 'CC BY 4.0', 'https://www.ign.es/resources/licencia/Condiciones_licenciaUso_IGN.pdf', 'Obra derivada de NGMEP CC-BY 4.0 ign.es']
]);

const rows = [];
for (const row of municipalityRows) {
  const number = row.number;
  if (number <= 2) continue;
  const province = String(row.values.get('B') ?? '').padStart(2, '0');
  const municipality = String(row.values.get('C') ?? '').padStart(3, '0');
  const longitude = row.values.get('F');
  const latitude = row.values.get('G');
  const identity = `${province}${municipality}`;
  if (!/^\d{5}$/.test(identity)) throw new Error(`Codi INE invàlid a la fila ${number}.`);
  if (latitude === null || longitude === null || latitude === '' || longitude === '')
    throw new Error(`Coordenades incompletes a la fila ${number}.`);
  rows.push([
    `es:municipality:${identity}`, 'es:ine:municipality', identity,
    '', '', '', '', '', '', latitude, longitude, 'CNIG_NGMEP_2026'
  ]);
}
if (rows.length !== 8132) throw new Error(`S'esperaven 8.132 municipis i se n'han llegit ${rows.length}.`);
if (process.argv.includes('--print-codes')) {
  console.log(rows.map(row => row[2]).sort().join('\n'));
  process.exit(0);
}

addTable('Unitats', [
  'unit_key', 'identity_scheme', 'identity_value', 'type_code', 'parent_unit_key', 'primary_name',
  'primary_locale', 'is_active', 'structure_source_key', 'latitude', 'longitude', 'coordinate_source_key'
], rows);

await workbook.xlsx.writeFile(outputPath);
console.log(JSON.stringify({ outputPath, municipalities: rows.length }));
