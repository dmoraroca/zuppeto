import ExcelJS from 'exceljs';
import JSZip from 'jszip';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const sourcePath = path.join(root, 'docs', 'idiomes', 'municipis per pais', 'PetiLoc_Alemanya_20260930_Coordenades_Revisades.xlsx');
const outputPath = path.join(root, 'docs', 'idiomes', 'municipis per pais', 'PetiLoc_Alemanya_20260930_Contracte_1_0.xlsx');
const sourceKey = 'DE-DESTATIS-GVISYS';

const badenWuerttembergRegions = new Map(Object.entries({
  '08111': '08101', '08115': '08101', '08116': '08101', '08117': '08101', '08118': '08101', '08119': '08101',
  '08121': '08102', '08125': '08102', '08126': '08102', '08127': '08102', '08128': '08102',
  '08135': '08103', '08136': '08103',
  '08211': '08201', '08212': '08201', '08215': '08201', '08216': '08201',
  '08221': '08202', '08222': '08202', '08225': '08202', '08226': '08202',
  '08231': '08203', '08235': '08203', '08236': '08203', '08237': '08203',
  '08311': '08301', '08315': '08301', '08316': '08301', '08317': '08301',
  '08325': '08302', '08326': '08302', '08327': '08302',
  '08335': '08303', '08336': '08303', '08337': '08303',
  '08415': '08401', '08416': '08401', '08417': '08401',
  '08421': '08402', '08425': '08402', '08426': '08402',
  '08435': '08403', '08436': '08403', '08437': '08403'
}));
const administrativeRegionLands = new Set(['05', '06', '08', '09']);
const statisticalRegionLands = new Set(['03', '07', '14']);

const decodeXml = value => value
  .replaceAll('&lt;', '<').replaceAll('&gt;', '>').replaceAll('&quot;', '"')
  .replaceAll('&apos;', "'").replaceAll('&amp;', '&')
  .replace(/&#(\d+);/g, (_, codePoint) => String.fromCodePoint(Number(codePoint)))
  .replace(/&#x([0-9a-f]+);/gi, (_, codePoint) => String.fromCodePoint(Number.parseInt(codePoint, 16)));

function parseRows(xml, sharedStrings) {
  return [...xml.replaceAll('x:', '').matchAll(/<row\b[^>]*r="(\d+)"[^>]*>([\s\S]*?)<\/row>/g)].map(row => {
    const values = new Map();
    for (const cell of row[2].matchAll(/<c\b([^>]*)>([\s\S]*?)<\/c>/g)) {
      const column = /\br="([A-Z]+)\d+"/.exec(cell[1])?.[1];
      const type = /\bt="([^"]+)"/.exec(cell[1])?.[1];
      const raw = /<v>([\s\S]*?)<\/v>/.exec(cell[2])?.[1] ?? '';
      const inline = [...cell[2].matchAll(/<t\b[^>]*>([\s\S]*?)<\/t>/g)].map(value => value[1]).join('');
      if (column) values.set(column, decodeXml(type === 's' ? sharedStrings[Number(raw)] ?? '' : type === 'inlineStr' || type === 'str' ? inline || raw : raw));
    }
    return { number: Number(row[1]), values };
  });
}

async function readSource() {
  const zip = await JSZip.loadAsync(await readFile(sourcePath));
  const sharedXml = (await zip.file('xl/sharedStrings.xml')?.async('text') ?? '').replaceAll('x:', '');
  const sharedStrings = [...sharedXml.matchAll(/<si\b[^>]*>([\s\S]*?)<\/si>/g)]
    .map(match => decodeXml([...match[1].matchAll(/<t\b[^>]*>([\s\S]*?)<\/t>/g)].map(value => value[1]).join('')));
  const names = ['Informacio', 'Bundeslaender', 'Regierungsbezirke', 'Regionen', 'Kreise', 'Gemeindeverbaende', 'Municipis'];
  const result = {};
  for (let index = 0; index < names.length; index++) {
    const xml = await zip.file(`xl/worksheets/sheet${index + 1}.xml`)?.async('text');
    if (!xml) throw new Error(`Falta el full ${names[index]}.`);
    const rows = parseRows(xml, sharedStrings);
    const header = rows.find(row => row.number === 2);
    if (!header) throw new Error(`Falta la capçalera del full ${names[index]}.`);
    const columns = Object.fromEntries([...header.values].map(([column, value]) => [column, value]));
    result[names[index]] = rows.filter(row => row.number > 2 && [...row.values.values()].some(Boolean))
      .map(row => Object.fromEntries([...row.values].map(([column, value]) => [columns[column] ?? column, value])));
  }
  return result;
}

function districtKey(row) {
  const code = `${row.LAND}${row.RB}${row.KREIS}`;
  return ['41', '42'].includes(row.Textkennzeichen) ? `de:independent:${code}` : `de:kreis:${code}`;
}

function districtParent(row, regionalKeys) {
  const code = `${row.LAND}${row.RB}${row.KREIS}`;
  if (row.LAND === '08') {
    const region = badenWuerttembergRegions.get(code);
    if (!region) throw new Error(`El Kreis ${code} no té correspondència oficial amb cap Region de Baden-Württemberg.`);
    return `de:region:${region}`;
  }
  const regionalKey = `de:rb:${row.LAND}${row.RB}`;
  return regionalKeys.has(regionalKey) ? regionalKey : `de:land:${row.LAND}`;
}

const workbook = new ExcelJS.Workbook();
workbook.creator = 'Zuppeto';
workbook.created = new Date('2026-09-30T00:00:00Z');

function addTable(name, headers, rows) {
  const sheet = workbook.addWorksheet(name);
  sheet.addRow(headers);
  for (const row of rows) sheet.addRow(row);
  sheet.views = [{ state: 'frozen', ySplit: 1 }];
  sheet.getRow(1).font = { bold: true };
  sheet.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: headers.length } };
}

const source = await readSource();
const units = new Map();
const names = [];
const codes = [];
const addUnit = unit => {
  if (units.has(unit.unitKey)) throw new Error(`Unitat duplicada abans de consolidar: ${unit.unitKey}.`);
  units.set(unit.unitKey, unit);
};
const addCode = (unitKey, scheme, value, primary = false) => codes.push([unitKey, scheme, value, primary, '', '', sourceKey, 'SET']);

for (const row of source.Bundeslaender) addUnit({
  unitKey: `de:land:${row.LAND}`, identityScheme: 'de:destatis:land', identityValue: row.LAND,
  type: 'BUNDESLAND', parent: '', name: row.Nom
});

for (const row of source.Regierungsbezirke) {
  const type = administrativeRegionLands.has(row.LAND) ? 'REGIERUNGSBEZIRK' : statisticalRegionLands.has(row.LAND) ? 'STATISTISCHE_REGION' : null;
  if (!type) throw new Error(`Classificació desconeguda de Regierungsbezirk per al Land ${row.LAND}.`);
  addUnit({
    unitKey: `de:rb:${row.LAND}${row.RB}`, identityScheme: 'de:destatis:rb', identityValue: `${row.LAND}${row.RB}`,
    type, parent: `de:land:${row.LAND}`, name: row.Nom
  });
}
const regionalKeys = new Set(units.keys());

for (const row of source.Regionen) {
  const officialRegionCode = `${row.LAND}${row.RB}${Number(row.REGION)}`;
  addUnit({
    unitKey: `de:region:${row.LAND}${row.RB}${row.REGION}`, identityScheme: 'de:stala-bw:region', identityValue: officialRegionCode,
    type: 'REGION', parent: `de:rb:${row.LAND}${row.RB}`, name: row.Nom
  });
}

for (const row of source.Kreise) {
  const key = districtKey(row);
  addUnit({
    unitKey: key, identityScheme: 'de:destatis:kreis', identityValue: `${row.LAND}${row.RB}${row.KREIS}`,
    type: ['41', '42'].includes(row.Textkennzeichen) ? 'KREISFREIE_STADT' : 'KREIS',
    parent: districtParent(row, regionalKeys), name: row.Nom
  });
}

for (const row of source.Gemeindeverbaende) {
  if (row.VB === '0000') continue;
  const key = `de:association:${row['ARS prefix']}`;
  addUnit({
    unitKey: key, identityScheme: 'de:destatis:ars-prefix', identityValue: row['ARS prefix'],
    type: row.KREIS === '00' ? 'SPECIAL_TERRITORY' : 'GEMEINDEVERBAND',
    parent: row.KREIS === '00' ? `de:land:${row.LAND}` : districtKey(row), name: row.Nom
  });
}

for (const row of source.Municipis) {
  const independent = row.VB === '0000' && row.GEM === '000';
  const special = row.KREIS === '00' && row.VB !== '0000';
  const latitude = row.Latitud ?? '';
  const longitude = row.Longitud ?? '';
  if ((latitude === '') !== (longitude === '')) throw new Error(`Coordenades incompletes per a ${row.AGS}.`);
  if (Number(latitude) === 0 && Number(longitude) === 0 && latitude !== '') throw new Error(`Coordenada sentinella (0,0) per a ${row.AGS}.`);
  if (latitude !== '' && (Number(latitude) < -90 || Number(latitude) > 90 || Number(longitude) < -180 || Number(longitude) > 180))
    throw new Error(`Coordenades fora de rang per a ${row.AGS}.`);

  if (independent) {
    const key = `de:independent:${row.LAND}${row.RB}${row.KREIS}`;
    const existing = units.get(key);
    if (!existing) throw new Error(`Falta el registre Kreis de la ciutat independent ${row.AGS}.`);
    if (existing.name !== row.Nom) names.push([key, existing.name, 'de-DE', 'Alternative', false, sourceKey, 'SET']);
    existing.name = row.Nom;
    existing.latitude = latitude;
    existing.longitude = longitude;
    addCode(key, 'de:destatis:ars', row.ARS);
    addCode(key, 'de:destatis:ags', row.AGS);
    continue;
  }

  const unitKey = special ? `de:special:${row.AGS}` : `de:gemeinde:${row.AGS}`;
  const parent = special ? `de:land:${row.LAND}` : row.VB === '0000' ? districtKey(row) : `de:association:${row.LAND}${row.RB}${row.KREIS}${row.VB}`;
  addUnit({
    unitKey, identityScheme: 'de:destatis:ags', identityValue: row.AGS,
    type: special ? 'SPECIAL_TERRITORY' : 'GEMEINDE', parent, name: row.Nom, latitude, longitude
  });
  addCode(unitKey, 'de:destatis:ars', row.ARS);
}

const expected = {
  BUNDESLAND: 16, REGIERUNGSBEZIRK: 19, STATISTISCHE_REGION: 10, REGION: 12, KREIS: 294,
  KREISFREIE_STADT: 107, GEMEINDEVERBAND: 4474, GEMEINDE: 10833, SPECIAL_TERRITORY: 5
};
const actual = Object.fromEntries([...units.values()].reduce((counts, unit) =>
  counts.set(unit.type, (counts.get(unit.type) ?? 0) + 1), new Map()));
for (const [type, count] of Object.entries(expected))
  if (actual[type] !== count) throw new Error(`Recompte inesperat per a ${type}: ${actual[type]} (esperat ${count}).`);
if (units.size !== 15770) throw new Error(`S'esperaven 15.770 unitats i se n'han generat ${units.size}.`);
for (const unit of units.values()) if (unit.parent && !units.has(unit.parent)) throw new Error(`Pare inexistent ${unit.parent} de ${unit.unitKey}.`);
const identities = new Set();
for (const unit of units.values()) {
  const identity = `${unit.identityScheme}\u001f${unit.identityValue}`;
  if (identities.has(identity)) throw new Error(`Identitat oficial duplicada: ${unit.identityScheme} ${unit.identityValue}.`);
  identities.add(identity);
  const visited = new Set([unit.unitKey]);
  let current = unit;
  while (current.parent) {
    if (visited.has(current.parent)) throw new Error(`Cicle jeràrquic detectat a ${unit.unitKey}.`);
    visited.add(current.parent);
    current = units.get(current.parent);
  }
}
const missingCoordinateAgs = source.Municipis.filter(row => !row.Latitud || !row.Longitud)
  .map(row => row.AGS).sort();
const expectedMissingCoordinateAgs = ['07000999', '10042999', '13000998', '13000999'];
if (JSON.stringify(missingCoordinateAgs) !== JSON.stringify(expectedMissingCoordinateAgs))
  throw new Error(`Excepcions de coordenades inesperades: ${missingCoordinateAgs.join(', ')}.`);

addTable('Manifest', ['key', 'value'], [
  ['contract_id', 'PETILOC_TERRITORIAL'], ['contract_version', '1.0'], ['country_iso2', 'DE'], ['import_mode', 'FULL'],
  ['dataset_version', '2026-Q3'], ['territorial_date', '2026-09-30'], ['primary_source_key', sourceKey], ['field_scope', ''], ['coverage', 'COUNTRY']
]);
addTable('Fonts', ['source_key', 'organisation', 'dataset', 'dataset_version', 'territorial_date', 'url', 'license', 'license_url', 'attribution'], [[
  sourceKey, 'Statistische Ämter des Bundes und der Länder', 'Gemeindeverzeichnis-Informationssystem (GV-ISys) — administrative Gebietsgliederung',
  '2026-Q3', '2026-09-30',
  'https://www.destatis.de/DE/Themen/Laender-Regionen/Regionales/Gemeindeverzeichnis/Administrativ/Archiv/GVAuszugQ/AuszugGV3QAktuell.html',
  'Destatis: Vervielfältigung und Verbreitung, auch auszugsweise, mit Quellennachweis gestattet',
  'https://www.destatis.de/DE/Service/Impressum/copyright-allgemein.html',
  'Quelle: Gemeindeverzeichnis-Informationssystem (GV-ISys), Gebietsstand 30.09.2026, Statistisches Bundesamt (Destatis), 2026; für Petiloc bearbeitet.'
]]);
addTable('Tipus', ['type_code', 'name', 'display_order', 'is_selectable_locality'], [
  ['BUNDESLAND', 'Land', 10, false], ['REGIERUNGSBEZIRK', 'Regierungsbezirk', 20, false],
  ['STATISTISCHE_REGION', 'Statistische Region / antic Regierungsbezirk', 20, false], ['REGION', 'Planungsregion', 30, false],
  ['KREIS', 'Kreis', 40, false], ['KREISFREIE_STADT', 'Kreisfreie Stadt', 40, true],
  ['GEMEINDEVERBAND', 'Gemeindeverband', 50, false], ['GEMEINDE', 'Gemeinde', 60, true],
  ['SPECIAL_TERRITORY', 'Territori especial', 60, false]
]);
addTable('Unitats', [
  'unit_key', 'identity_scheme', 'identity_value', 'type_code', 'parent_unit_key', 'primary_name',
  'primary_locale', 'is_active', 'structure_source_key', 'latitude', 'longitude', 'coordinate_source_key'
], [...units.values()].map(unit => [
  unit.unitKey, unit.identityScheme, unit.identityValue, unit.type, unit.parent, unit.name, 'de-DE', true, sourceKey,
  unit.latitude ?? '', unit.longitude ?? '', unit.latitude !== undefined && unit.latitude !== '' ? sourceKey : ''
]));
addTable('Noms', ['unit_key', 'name', 'locale', 'kind', 'is_primary', 'source_key', 'action'], names);
addTable('Codis', ['unit_key', 'scheme', 'value', 'is_primary', 'valid_from', 'valid_to', 'source_key', 'action'], codes);

await workbook.xlsx.writeFile(outputPath);
console.log(JSON.stringify({ outputPath, units: units.size, types: actual, alternateNames: names.length, additionalCodes: codes.length }, null, 2));
