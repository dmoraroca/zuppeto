import ExcelJS from 'exceljs';
import type { ChromeBlock, ChromeScenarioInventoryItem } from '../../domain/chrome-inventory.js';
import type { E2ERole } from '../../domain/e2e-role.js';

const specificationExclusions = new Map<string, string>([
  ['ZUP-016:USER', 'EXTERNAL: requereix completar un OAuth real de Google fora de l’entorn controlat.'],
  ['ZUP-021:USER', 'SPECIFICATION: el rol USER no disposa del menú «Del administrador» indicat al pas.'],
  ['ZUP-107:VIEWER', 'SPECIFICATION: VIEWER no pot accedir a Administració per crear usuaris.'],
  ['ZUP-109:DEVELOPER', 'SPECIFICATION: DEVELOPER no té permís de gestió d’usuaris.'],
  ['ZUP-145:ADMIN', 'SPECIFICATION: la fila declara ADMIN però el pas i el resultat corresponen a USER.']
]);

export class ExcelChromeScenarioInventory {
  public constructor(private readonly workbookPath: string, private readonly browserName = 'Chrome') {}

  public async load(): Promise<readonly ChromeScenarioInventoryItem[]> {
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.readFile(this.workbookPath);
    const sheet = workbook.getWorksheet('Proves');
    if (sheet === undefined) throw new Error('No existeix el full Proves.');
    const headers = new Map<string, number>();
    sheet.getRow(1).eachCell((cell, column) => headers.set(String(cell.value).trim(), column));
    const value = (row: ExcelJS.Row, name: string): string => String(row.getCell(required(headers, name)).text ?? '').trim();
    const items: ChromeScenarioInventoryItem[] = [];
    for (let rowNumber = 2; rowNumber <= sheet.rowCount; rowNumber += 1) {
      const row = sheet.getRow(rowNumber);
      if (value(row, 'Navegador') !== this.browserName) continue;
      const testCode = value(row, 'Codi prova');
      const excelRole = value(row, 'Rol probat');
      const role = normalizeRole(excelRole);
      const numericCode = Number(testCode.slice(4));
      const exclusionReason = specificationExclusions.get(`${testCode}:${excelRole}`);
      items.push({
        testCode,
        scenarioId: `${testCode}-${role}-${value(row, 'Id escenari')}`,
        role,
        excelRole,
        variant: value(row, 'Id escenari'),
        screen: value(row, 'Pantalla'),
        description: value(row, 'Descripció prova'),
        steps: value(row, 'Passos'),
        expectedResult: value(row, 'Resultat esperat'),
        priority: value(row, 'Prioritat'),
        block: blockFor(numericCode),
        preconditions: preconditionsFor(role, numericCode),
        fixture: role === 'SENSE_SESSIO' ? 'anonymousSession' : `${role.toLowerCase()}Session`,
        data: dataFor(numericCode),
        cleanup: cleanupFor(numericCode),
        tags: [`@${testCode}`, `@${role}`, `@${blockFor(numericCode)}`, `@${value(row, 'Prioritat').toLowerCase()}`],
        dependencies: dependenciesFor(role, numericCode),
        risk: riskFor(numericCode, exclusionReason),
        automatable: exclusionReason === undefined,
        exclusionReason
      });
    }
    const ids = items.map((item) => item.scenarioId);
    if (new Set(ids).size !== ids.length) throw new Error(`L’inventari ${this.browserName} conté ScenarioId duplicats.`);
    return items;
  }
}

function required(headers: ReadonlyMap<string, number>, name: string): number {
  const column = headers.get(name);
  if (column === undefined) throw new Error(`Falta la columna requerida ${name}.`);
  return column;
}

function normalizeRole(role: string): E2ERole {
  if (role === 'Sense sessió') return 'SENSE_SESSIO';
  if (role === 'USER' || role === 'ADMIN' || role === 'DEVELOPER' || role === 'VIEWER') return role;
  throw new Error(`Rol E2E desconegut: ${role}.`);
}

function blockFor(code: number): ChromeBlock {
  if (code <= 16) return 'authentication';
  if (code <= 29) return 'navigation-security';
  if (code <= 37) return 'home';
  if (code <= 54) return 'places';
  if (code <= 64) return 'place-detail';
  if (code <= 75) return 'favorites';
  if (code <= 84) return 'profile';
  if (code <= 89) return 'notifications';
  if (code <= 98) return 'help-contact';
  if (code <= 103) return 'admin-roles';
  if (code <= 115) return 'admin-users';
  if (code <= 126) return 'admin-permissions-menus';
  if (code <= 138) return 'admin-geography';
  if (code <= 143) return 'admin-places';
  return 'documentation-api-security';
}

function preconditionsFor(role: E2ERole, code: number): readonly string[] {
  const result = role === 'SENSE_SESSIO' ? ['Context de navegador net sense sessió'] : [`Compte E2E ${role} configurat`];
  if ((code >= 34 && code <= 75) || (code >= 55 && code <= 64)) result.push('Catàleg amb almenys un lloc controlable');
  return result;
}

function dataFor(code: number): readonly string[] {
  if ([34, 47, 60, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74].includes(code)) return ['Favorite temporal exacte'];
  if (code >= 78 && code <= 83) return ['Estat original del perfil', 'Valor temporal traçable'];
  if (code >= 86 && code <= 89) return ['Notificació local controlada'];
  if (code >= 100 && code <= 103) return ['Role temporal traçable'];
  if (code >= 107 && code <= 114) return ['Viewer temporal traçable'];
  if (code >= 117 && code <= 126) return ['Estat original de permisos o menú'];
  if (code >= 128 && code <= 138) return ['Country/City temporal traçable'];
  if (code >= 140 && code <= 143) return ['Place temporal traçable'];
  return [];
}

function cleanupFor(code: number): string {
  if (dataFor(code).length === 0) return 'Cap dada persistent creada';
  if ((code >= 78 && code <= 83) || (code >= 117 && code <= 126)) return 'Restauració exacta de l’estat original';
  return 'Cleanup exacte dels identificadors registrats per l’execució';
}

function dependenciesFor(role: E2ERole, code: number): readonly string[] {
  const dependencies: string[] = [];
  if (role === 'ADMIN') dependencies.push('ADMIN E2E dedicat');
  if (role === 'VIEWER') dependencies.push('Credencial VIEWER local');
  if (code === 16) dependencies.push('OAuth extern interactiu');
  if ([51, 52, 53, 54, 59, 62].includes(code)) dependencies.push('Mapa Leaflet i geometria visible');
  if ([79, 80, 81, 113].includes(code)) dependencies.push('Fitxer d’imatge temporal segur');
  return dependencies;
}

function riskFor(code: number, exclusionReason: string | undefined): 'LOW' | 'MEDIUM' | 'HIGH' {
  if (exclusionReason !== undefined || (code >= 99 && code <= 143) || [25, 51, 52, 53, 54, 59, 62, 78, 79, 80, 81].includes(code)) return 'HIGH';
  if (dataFor(code).length > 0) return 'MEDIUM';
  return 'LOW';
}
