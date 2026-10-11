import JSZip from 'jszip';
import { readFile } from 'node:fs/promises';
import path from 'node:path';

const decodeXml = (value) => value
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

async function readWorkbook(filePath) {
  const zip = await JSZip.loadAsync(await readFile(filePath));
  const sharedXml = (await zip.file('xl/sharedStrings.xml')?.async('text') ?? '').replaceAll('x:', '');
  const sharedStrings = [...sharedXml.matchAll(/<si\b[^>]*>([\s\S]*?)<\/si>/g)]
    .map(match => decodeXml([...match[1].matchAll(/<t\b[^>]*>([\s\S]*?)<\/t>/g)].map(value => value[1]).join('')));
  const names = ['Informacio', 'Bundeslaender', 'Regierungsbezirke', 'Regionen', 'Kreise', 'Gemeindeverbaende', 'Municipis'];
  const sheets = new Map();
  for (let index = 0; index < names.length; index++) {
    const xml = await zip.file(`xl/worksheets/sheet${index + 1}.xml`)?.async('text');
    if (!xml) throw new Error(`Falta ${names[index]}.`);
    const rows = parseRows(xml, sharedStrings);
    const header = rows.find(row => row.number === 2);
    const columns = header ? Object.fromEntries([...header.values].map(([column, value]) => [column, value])) : {};
    const data = rows.filter(row => row.number > 2 && [...row.values.values()].some(Boolean));
    sheets.set(names[index], { columns, data });
  }
  return sheets;
}

const rowObject = (sheet, row) => Object.fromEntries([...row.values].map(([column, value]) => [sheet.columns[column] ?? column, value]));
const countBy = (rows, field) => Object.fromEntries([...rows.reduce((counts, row) => {
  const key = row[field] || '(buit)';
  counts.set(key, (counts.get(key) ?? 0) + 1);
  return counts;
}, new Map()).entries()].sort());

const requested = process.argv.slice(2);
const files = requested.length ? requested : [
  'docs/ca/Municipis/PetiLoc_Alemanya_20260930.xlsx',
  'docs/idiomes/municipis per pais/PetiLoc_Alemanya_20260930_Coordenades_Revisades.xlsx'
];

for (const file of files) {
  const sheets = await readWorkbook(file);
  const objects = Object.fromEntries([...sheets].map(([name, sheet]) => [name, sheet.data.map(row => rowObject(sheet, row))]));
  const municipalityCoordinates = objects.Municipis.map(row => ({
    ags: row.AGS,
    latitude: row.Latitud,
    longitude: row.Longitud
  }));
  const missingCoordinates = municipalityCoordinates.filter(row => !row.latitude || !row.longitude || (Number(row.latitude) === 0 && Number(row.longitude) === 0));
  const independentDistricts = objects.Kreise.filter(row => ['41', '42'].includes(row.Textkennzeichen));
  const districts = objects.Kreise.filter(row => !['41', '42'].includes(row.Textkennzeichen));
  const associations = objects.Gemeindeverbaende.filter(row => row.VB !== '0000' && row.KREIS !== '00');
  const specialAssociations = objects.Gemeindeverbaende.filter(row => row.VB !== '0000' && row.KREIS === '00');
  const independentMunicipalities = objects.Municipis.filter(row => row.VB === '0000' && row.GEM === '000');
  const municipalities = objects.Municipis.filter(row => row.KREIS !== '00' && !(row.VB === '0000' && row.GEM === '000'));
  const specialMunicipalities = objects.Municipis.filter(row => row.KREIS === '00' && row.VB !== '0000');
  console.log(JSON.stringify({
    file: path.resolve(file),
    sheets: Object.fromEntries([...sheets].map(([name, sheet]) => [name, { columns: sheet.columns, rows: sheet.data.length }])),
    information: objects.Informacio,
    regierungsbezirke: objects.Regierungsbezirke,
    regionen: objects.Regionen,
    badenWuerttembergDistricts: objects.Kreise.filter(row => row.LAND === '08'),
    normalizedCounts: {
      laender: objects.Bundeslaender.length,
      regierungsbezirke: objects.Regierungsbezirke.length,
      regionen: objects.Regionen.length,
      kreise: districts.length,
      kreisfreieStaedte: independentDistricts.length,
      gemeindeverbaende: associations.length,
      gemeinden: municipalities.length,
      specialTerritories: specialAssociations.length + specialMunicipalities.length
    },
    specialTerritories: [...specialAssociations, ...specialMunicipalities],
    coordinateExceptions: objects.Municipis.filter(row => missingCoordinates.some(item => item.ags === row.AGS)),
    kreiseByTextkennzeichen: countBy(objects.Kreise, 'Textkennzeichen'),
    associationsByTextkennzeichen: countBy(objects.Gemeindeverbaende, 'Textkennzeichen'),
    municipalitiesByTextkennzeichen: countBy(objects.Municipis, 'Textkennzeichen'),
    municipalitiesByKreis: countBy(objects.Municipis, 'KREIS'),
    missingCoordinates
  }, null, 2));
}
