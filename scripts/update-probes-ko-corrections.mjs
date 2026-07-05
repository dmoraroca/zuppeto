/**
 * Updates corrected KO rows in the manual probes workbook (unified Descripció format).
 * Run: node scripts/update-probes-ko-corrections.mjs [path-to-xlsx]
 */
import ExcelJS from 'exceljs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const defaultWorkbook = path.join(
  __dirname,
  '../docs/probes-e2e-resultats/20260621_proves_zuppeto_pantalles.xlsx'
);
const workbookPath = process.argv[2] ? path.resolve(process.argv[2]) : defaultWorkbook;
const correctionDate = new Date('2026-07-06T00:00:00');

/** @type {Record<string, { result: string, description: string, roles: string[] }>} */
const correctionsByCode = {
  'ZUP-002': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: S'ha afegit una alerta visible al formulari de login (role=alert) quan el formulari és buit o invàlid. Abans el missatge només es guardava al servei intern i l'usuari no el veia. Fitxers: login-page.component.ts, .html, .scss."
  },
  'ZUP-003': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: S'ha mostrat al login l'alerta «Credencials incorrectes — l'usuari o la contrasenya no són correctes» quan l'autenticació falla. Abans no hi havia feedback visible a la pantalla. Fitxers: login-page.component.ts, .html, .scss."
  },
  'ZUP-006': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: S'ha verificat que en localhost/dev el botó Google es mostra com a «Google pendent» (comportament esperat). S'ha afegit text d'ajuda sota els proveïdors OAuth per aclarir-ho a la prova manual. Fitxers: login-page.component.html, .scss."
  },
  'ZUP-007': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: S'ha verificat que LinkedIn queda «LinkedIn pendent» i desactivat si el backend no té ClientId/Secret. S'ha afegit el mateix text d'ajuda OAuth al login. No cal canvi de backend en aquesta fase. Fitxers: login-page.component.html, .scss."
  },
  'ZUP-008': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: S'ha verificat que Facebook es mostra com a «Facebook pendent» i desactivat (encara no implementat). S'ha afegit text d'ajuda OAuth al login. Fitxers: login-page.component.html, .scss."
  },
  'ZUP-009': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: El mosaic del preview al login ara usa dades fictícies (PLACES_FAKE) sense sessió, amb filtres funcionals i missatge si no hi ha coincidències. El catàleg real de l'API continua carregant-se només després del login. Fitxer: login-page.component.ts."
  },
  'ZUP-011': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: «Obrir cerca» guarda els filtres a redirectTo i mostra una alerta informativa al login. Després d'iniciar sessió, l'usuari obrirà /places amb aquests filtres. Fitxers: login-page.component.ts, .html."
  },
  'ZUP-012': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: «Generar rutes» ja no navega sense sessió ni trenca la UI; mostra una alerta informativa que aquesta acció és preview de fase posterior. Fitxers: login-page.component.ts, .html."
  },
  'ZUP-015': {
    result: 'OK',
    roles: ['USER'],
    description:
      "Què s'ha fet: Quan /auth/callback s'obre sense paràmetres, es redirigeix a /login i l'alerta del callback es mostra al formulari de login. Fitxers: auth-callback-page.component.ts, login-page.component.ts."
  },
  'ZUP-016': {
    result: 'N/A',
    roles: ['USER'],
    description:
      "Què s'ha fet: S'ha marcat la prova com a N/A. El flux OAuth complet (Google/LinkedIn) requereix compte real del proveïdor i configuració al backend; no és un defecte del producte en dev local. No s'ha implementat automatització d'aquesta prova manual."
  },
  'ZUP-024': {
    result: 'OK',
    roles: ['ADMIN'],
    description:
      "Què s'ha fet: S'ha comprovat que el footer ja inclou l'enllaç «Contacta'ns» i s'ha actualitzat el resultat esperat de la prova al generador d'Excel. Fitxers: site-footer.component.html, scripts/generate-probes-excel.mjs."
  }
};

const workbook = new ExcelJS.Workbook();
await workbook.xlsx.readFile(workbookPath);
const sheet = workbook.getWorksheet('Proves');

if (!sheet) {
  throw new Error('Worksheet "Proves" not found');
}

let updated = 0;

sheet.eachRow((row, rowNumber) => {
  if (rowNumber === 1) {
    return;
  }

  const code = String(row.getCell(3).value ?? '').trim();
  const browser = String(row.getCell(7).value ?? '').trim();
  const role = String(row.getCell(8).value ?? '').trim();

  if (browser !== 'Chrome') {
    return;
  }

  const correction = correctionsByCode[code];
  if (!correction || !correction.roles.includes(role)) {
    return;
  }

  row.getCell(10).value = correction.result;
  row.getCell(12).value = correctionDate;
  row.getCell(13).value = correction.description;
  updated += 1;
});

await workbook.xlsx.writeFile(workbookPath);
console.log(`Updated ${updated} row(s) in ${workbookPath}`);
