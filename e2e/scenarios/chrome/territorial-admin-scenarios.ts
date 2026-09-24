import { expect, type Locator, type Page } from '@playwright/test';
import ExcelJS from 'exceljs';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import type { ChromeScenarioContext } from './chrome-scenario.js';

const sourceId = '10000000-0000-0000-0000-000000000001';
const mappingId = '20000000-0000-0000-0000-000000000001';
const importId = '30000000-0000-0000-0000-000000000001';

export async function executeTerritorialAdminScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  if (code === 155) return securityScenario(context);
  if (code === 160 && process.env['E2E_TERRITORIAL_CATALOG_REAL'] === 'true') return realPublishedSpainCatalogExplorer(context);
  if (code === 160 && process.env['E2E_TERRITORIAL_REAL'] === 'true') return realTerritorialCircuit(context);
  const mode = code === 158 ? 'blocked' : 'ready';
  await mockTerritorialApi(context, mode, code === 157);

  if (code === 160) {
    await context.page.goto('/admin/territori');
    await context.page.getByRole('button', { name: 'Historial' }).click();
    await expect(context.page.getByRole('cell', { name: 'Sintètic' })).toBeVisible();
    await context.page.getByRole('button', { name: 'Obrir detall' }).click();
    await expect(context.page).toHaveURL(new RegExp('/admin/territori/' + importId));
    await context.page.getByRole('button', { name: 'Anar a publicació' }).click();
    await expect(context.page.getByText('Catàleg actual: v7')).toBeVisible();
    await context.page.getByRole('button', { name: 'Catàleg territorial' }).click();
    await context.page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await context.page.getByRole('cell', { name: 'Àmbit sintètic', exact: true }).click();
    await expect(context.page.getByRole('heading', { name: 'Àmbit sintètic' })).toBeVisible();
    await expect(context.page.getByRole('definition').filter({ hasText: 'País sintètic' })).toBeVisible();
    await context.page.getByRole('button', { name: 'Desactivar' }).click();
    await context.page.getByLabel('Motiu *').fill('Revisió E2E controlada');
    await context.page.getByRole('alertdialog').getByRole('button', { name: 'Desactivar' }).click();
    const deactivationAudit = context.page.locator('.audit-list li').filter({ hasText: 'Revisió E2E controlada' });
    await expect(deactivationAudit).toContainText('Admin E2E');
    await expect(deactivationAudit).toContainText('Revisió E2E controlada');
    await expect(deactivationAudit).toContainText('Abans');
    await expect(deactivationAudit).toContainText('Després');
    await context.page.getByRole('button', { name: 'General' }).click();
    await context.page.getByRole('button', { name: 'Reactivar' }).click();
    await context.page.getByLabel('Motiu *').fill('Reactivació E2E controlada');
    await context.page.getByRole('alertdialog').getByRole('button', { name: 'Reactivar' }).click();
    const activationAudit = context.page.locator('.audit-list li').filter({ hasText: 'Reactivació E2E controlada' });
    await expect(activationAudit).toContainText('Admin E2E');
    await expect(activationAudit).toContainText('Reactivació E2E controlada');
    return;
  }

  await context.page.goto('/admin/territori');
  await expect(context.page.getByRole('heading', { name: 'Gestió territorial' })).toBeVisible();
  if (code === 154) {
    await expect(context.page.getByText('Cap XLSX modifica el catàleg directament.')).toBeVisible();
    await expect(context.page.locator('.steps li').filter({ hasText: 'Context' })).toBeVisible();
    return;
  }

  await context.page.getByLabel('Font oficial').selectOption(sourceId);
  if (code === 156) {
    await context.page.locator('input[type=file]').setInputFiles({
      name: 'territori.csv', mimeType: 'text/csv', buffer: Buffer.from('codi,nom')
    });
    await expect(context.page.getByRole('alert')).toContainText('XLSX');
    await expect(context.page.getByRole('button', { name: 'Inspeccionar esquema' })).toBeVisible();
    return;
  }

  await context.page.locator('input[type=file]').setInputFiles({
    name: 'territori-sintetic.xlsx',
    mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    buffer: Buffer.from('PK synthetic fixture')
  });
  await context.page.getByRole('button', { name: 'Inspeccionar esquema' }).click();
  await expect(context.page.getByRole('heading', { name: '3. Mapping reutilitzable' })).toBeVisible();
  await context.page.getByRole('button', { name: 'Preparar validació i preview' }).click();

  if (code === 158) {
    await expect(context.page.getByText('DUPLICATE_CODE')).toBeVisible();
    await expect(context.page.getByRole('button', { name: 'Revisar canvis' })).toHaveCount(0);
    return;
  }

  await expect(context.page.getByRole('heading', { name: '5. Canvis a publicar' })).toBeVisible();
  await expect(context.page.locator('.changes-table').getByRole('cell', { name: 'Àmbit sintètic', exact: true })).toBeVisible();
  if (code === 157) {
    await expect(context.page.getByRole('heading', { name: 'Previsualització d’origen' })).toBeVisible();
    await expect(context.page.getByRole('heading', { name: 'Previsualització canonicalitzada' })).toBeVisible();
    await expect(context.page.getByText('Selecciona un full per previsualitzar-ne les dades.')).toBeVisible();
    await context.page.getByLabel('Full del preview').selectOption('Municipis');
    const sourceTable = context.page.locator('.source-preview-table');
    await expect(sourceTable.getByRole('columnheader', { name: 'CMUN' })).toBeVisible();
    await expect(sourceTable.getByRole('columnheader', { name: 'CODAUTO' })).toBeVisible();
    await expect(sourceTable.getByRole('columnheader', { name: 'CPRO' })).toBeVisible();
    await expect(sourceTable.getByRole('columnheader', { name: 'NOMBRE' })).toBeVisible();
    await expect(context.page.getByRole('columnheader', { name: 'Camps admesos', exact: true })).toHaveCount(0);
    await context.page.getByLabel('Cerca del preview').fill('Àmbit');
    await context.page.getByRole('button', { name: 'Aplicar filtres' }).first().click();
    await expect(sourceTable.getByRole('cell', { name: 'Àmbit sintètic', exact: true })).toBeVisible();
    await expect(context.page.getByRole('cell', { name: 'Àmbit sintètic', exact: true }).first()).toBeVisible();
    await expect(context.page.getByRole('cell', { name: 'Municipi', exact: true }).first()).toBeVisible();
    await expect(context.page.getByRole('cell', { name: 'Comunitats · fila 3', exact: true })).toBeVisible();
    await expect(context.page.getByRole('cell', { name: 'Municipis · fila 31', exact: true })).toBeVisible();
    await expect(context.page.getByText('Vàlida (0)')).toHaveCount(0);
    await expect(context.page.getByText('Pàgina 1 de 165')).toBeVisible();
    await expect(context.page.getByText('Pàgina 1 de 164')).toBeVisible();
    await context.page.getByText('Veure incidències', { exact: true }).click();
    const canonicalIssues = context.page.locator('.canonical-issues');
    await expect(canonicalIssues).toContainText('Avís');
    await expect(canonicalIssues).toContainText('COORDINATE_REVIEW');
    await expect(canonicalIssues).toContainText('Municipis · fila 31');
    await expect(canonicalIssues).toContainText('Cal revisar la procedència de les coordenades.');
    await assertFunctionalChangeDetails(context.page);
    await context.page.getByRole('button', { name: 'Anar a publicació' }).click();
    await expect(context.page.getByRole('heading', { name: '5. Canvis a publicar' })).toHaveCount(0);
    await expect(context.page.getByRole('heading', { name: '6. Publicació del catàleg territorial' })).toBeVisible();
    await expect(context.page.getByText('Pendent d’aprovació')).toBeVisible();
    await expect(context.page.getByRole('button', { name: 'Publicar catàleg' })).toBeDisabled();
    await context.page.getByRole('button', { name: 'Tornar als canvis' }).click();
    await expect(context.page.getByLabel('Cerca per nom o codi')).toHaveValue('Àmbit');
    await expect(context.page.getByText('Pàgina 1 de 164')).toBeVisible();
    return;
  }

  await context.page.getByRole('button', { name: 'Anar a publicació' }).click();
  if (code === 159) {
    await context.page.getByRole('button', { name: 'Publicar catàleg' }).click();
    await expect(context.page.getByRole('alertdialog')).toBeVisible();
    await context.page.getByRole('button', { name: 'Confirmar' }).click();
    await expect(context.page.getByText('Estat: Publicat')).toBeVisible();
  }
}

async function realPublishedSpainCatalogExplorer(context: ChromeScenarioContext): Promise<void> {
  if (!context.session.session) throw new Error('L’explorador territorial real requereix sessió ADMIN.');
  const page = context.page;
  let delayedInitialCatalog = false;
  await page.route('**/api/admin/territorial/catalog?**', async (route) => {
    const url = new URL(route.request().url());
    if (!delayedInitialCatalog && url.searchParams.get('page') === '1') {
      delayedInitialCatalog = true;
      await new Promise((resolve) => setTimeout(resolve, 220));
    }
    await route.continue();
  });

  await page.goto('/admin/territori');
  await page.getByRole('button', { name: 'Catàleg territorial' }).click();
  await expect(page.locator('.catalog-skeleton')).toBeVisible();
  await expect(page.getByText('8199 unitats')).toBeVisible();

  await page.locator('select[name="country"]').selectOption({ label: 'Espanya' });
  await page.locator('input[name="search"]').fill('Adra');
  await page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await page.locator('.catalog tbody tr').filter({ hasText: 'Adra' }).filter({ hasText: 'Municipi' }).first().click();
  await page.getByRole('button', { name: 'Jerarquia' }).click();
  const adraExplorer = page.locator('app-territorial-hierarchy-explorer');
  const pathNodes = adraExplorer.locator('.tree-node[data-depth]');
  await expect(pathNodes).toHaveCount(4);
  await expect(pathNodes.nth(0)).toHaveAttribute('data-depth', '0');
  await expect(pathNodes.nth(1)).toHaveAttribute('data-depth', '1');
  await expect(pathNodes.nth(2)).toHaveAttribute('data-depth', '2');
  await expect(pathNodes.nth(3)).toHaveAttribute('data-depth', '3');
  const pathPositions = await pathNodes.evaluateAll((nodes) => nodes.map((node) => node.getBoundingClientRect().x));
  expect(pathPositions[1]).toBeGreaterThan(pathPositions[0]);
  expect(pathPositions[2]).toBeGreaterThan(pathPositions[1]);
  expect(pathPositions[3]).toBeGreaterThan(pathPositions[2]);
  await expect(pathNodes.nth(1)).toContainText('Andalucía');
  await expect(pathNodes.nth(2)).toContainText('Almería');
  await expect(pathNodes.nth(3)).toContainText('Adra');
  await expect(pathNodes.nth(3)).toHaveClass(/tree-node--current/);
  await expect(adraExplorer.getByText('Unitat actual', { exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Explorar jerarquia' })).toHaveCount(0);
  await captureTree(page, 'A-adra-quatre-profunditats.png');
  await page.getByRole('button', { name: 'Tancar el detall territorial' }).click();

  await page.locator('input[name="search"]').fill('Aragón');
  await page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await page.locator('.catalog tbody tr').filter({ hasText: 'Aragón' }).filter({ hasText: 'Comunitat autònoma' }).click();
  await page.getByRole('button', { name: 'Jerarquia' }).click();
  const aragonExplorer = page.getByRole('dialog', { name: 'Aragón', exact: true })
    .locator('app-territorial-hierarchy-explorer');
  for (const province of ['Huesca', 'Teruel', 'Zaragoza']) {
    await expect(aragonExplorer.getByText(province, { exact: true })).toBeVisible();
  }
  const aragonProvinces = aragonExplorer.locator('.tree-node[data-depth="2"]');
  await expect(aragonProvinces).toHaveCount(3);
  await expect(aragonExplorer.getByRole('button', { name: 'Expandir Teruel' })).toBeVisible();
  expect(await aragonExplorer.getByRole('button', { name: 'Expandir Teruel' })
    .evaluate((button) => getComputedStyle(button).cursor)).toBe('pointer');
  expect(await aragonExplorer.locator('.node-main').filter({ hasText: 'Teruel' })
    .evaluate((button) => getComputedStyle(button).cursor)).toBe('pointer');
  await expect(page.getByRole('button', { name: 'Explorar jerarquia' })).toHaveCount(0);
  await captureTree(page, 'B-aragon-provincies.png');

  await aragonExplorer.getByRole('button', { name: 'Expandir Teruel' }).click();
  const teruelMunicipalities = aragonExplorer.locator('.tree-node[data-depth="3"]');
  await expect(teruelMunicipalities.first()).toBeVisible();
  await expect(aragonExplorer.locator('.node-main strong').filter({ hasText: /^Teruel$/ })).toHaveCount(1);
  await expect(aragonExplorer.getByText('Seleccionable', { exact: true })).toHaveCount(0);
  await expect(aragonExplorer.getByRole('button', { name: /Veure detall de / })).toHaveCount(50);
  await captureTree(page, 'C-teruel-municipis.png');
  await captureTree(page, 'E-teruel-veure-detall.png');

  await aragonExplorer.getByRole('button', { name: 'Veure detall de Ababuj' }).click();
  await expect(page.getByRole('heading', { name: 'Ababuj', exact: true })).toBeVisible();
  await expect(page.getByText('Municipi · Espanya · 44001', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Jerarquia' }).last().click();
  const ababujExplorer = page.locator('app-territorial-hierarchy-explorer').last();
  const ababujPath = ababujExplorer.locator('.tree-node[data-depth]');
  await expect(ababujPath).toHaveCount(4);
  await expect(ababujPath.nth(1)).toContainText('Aragón');
  await expect(ababujPath.nth(2)).toContainText('Teruel');
  await expect(ababujPath.nth(3)).toContainText('Ababuj');
  await page.getByRole('button', { name: 'Tancar el detall territorial' }).last().click();
  await expect(aragonExplorer).toBeVisible();
  await aragonExplorer.getByRole('button', { name: 'Contraure Teruel' }).click();
  await expect(teruelMunicipalities).toHaveCount(0);
  await aragonExplorer.getByRole('button', { name: 'Expandir Teruel' }).click();
  await expect(aragonExplorer.locator('.tree-node[data-depth="3"]').first()).toBeVisible();
  await page.getByRole('button', { name: 'Tancar el detall territorial' }).click();

  await page.locator('input[name="search"]').fill('Andalucía');
  await page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await page.locator('.catalog tbody tr').filter({ hasText: 'Andalucía' }).filter({ hasText: 'Comunitat autònoma' }).click();
  await page.getByRole('button', { name: 'Jerarquia' }).click();
  const andalusiaExplorer = page.locator('app-territorial-hierarchy-explorer');
  await andalusiaExplorer.getByRole('button', { name: 'Expandir Almería' }).click();
  await expect(andalusiaExplorer.locator('.tree-node[data-depth="3"]').first()).toBeVisible();
  await captureTree(page, 'D-andalucia-almeria.png');
  await page.getByRole('button', { name: 'Tancar el detall territorial' }).click();

  await page.locator('input[name="search"]').fill('Cataluña');
  await page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await page.locator('.catalog tbody tr').filter({ hasText: 'Cataluña' }).filter({ hasText: 'Comunitat autònoma' }).click();
  await page.getByRole('button', { name: 'Jerarquia' }).click();

  const explorer = page.locator('app-territorial-hierarchy-explorer');
  for (const province of ['Barcelona', 'Girona', 'Lleida', 'Tarragona']) {
    await expect(explorer.getByText(province, { exact: true })).toBeVisible();
  }
  const countryNode = explorer.locator('.tree-node[data-depth="0"]');
  const currentNode = explorer.locator('.tree-node[data-depth="1"]');
  const provinceNodes = explorer.locator('.tree-node[data-depth="2"]');
  await expect(provinceNodes).toHaveCount(4);
  const countryX = (await countryNode.boundingBox())?.x ?? 0;
  const currentX = (await currentNode.boundingBox())?.x ?? 0;
  const provinceX = (await provinceNodes.first().boundingBox())?.x ?? 0;
  expect(currentX).toBeGreaterThan(countryX);
  expect(provinceX).toBeGreaterThan(currentX);
  expect(await provinceNodes.first().evaluate((node) => getComputedStyle(node.parentElement!, '::before').width)).not.toBe('0px');
  await explorer.getByRole('button', { name: 'Expandir Barcelona' }).click();
  const arenysNode = explorer.locator('.tree-node').filter({ hasText: 'Arenys de Mar' });
  await expect(arenysNode).toContainText('08006');
  await expect(arenysNode).toHaveAttribute('data-depth', '3');
  const municipalityX = (await arenysNode.boundingBox())?.x ?? 0;
  expect(municipalityX).toBeGreaterThan(provinceX);
  await arenysNode.getByRole('button', { name: 'Veure detall de Arenys de Mar' }).click();
  await expect(page.getByRole('heading', { name: 'Arenys de Mar' })).toBeVisible();
  await page.getByRole('button', { name: 'Tancar el detall territorial' }).last().click();
  await expect(explorer).toBeVisible();
  await expect(arenysNode).toBeVisible();

  await explorer.getByRole('button', { name: /Tots els municipis/ }).click();
  await explorer.locator('.descendants input').fill('Arenys de Mar');
  await explorer.locator('.descendants').getByRole('button', { name: 'Cercar', exact: true }).click();
  const descendantRow = explorer.locator('.descendants tbody tr').filter({ hasText: 'Arenys de Mar' });
  await expect(descendantRow).toContainText('08006');
  await expect(descendantRow).toContainText('Barcelona');

  await page.getByRole('button', { name: 'Tancar el detall territorial' }).click();
  await page.locator('input[name="search"]').fill('Canarias');
  await page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await page.locator('.catalog tbody tr').filter({ hasText: 'Canarias' }).filter({ hasText: 'Comunitat autònoma' }).click();
  await page.getByRole('button', { name: 'Jerarquia' }).click();
  const canariasExplorer = page.locator('app-territorial-hierarchy-explorer');
  await canariasExplorer.getByRole('button', { name: 'Expandir Palmas, Las' }).click();
  const arrecife = canariasExplorer.locator('.node-main').filter({ hasText: 'Arrecife' });
  await expect(arrecife).toContainText('35004');
  await expect(canariasExplorer.getByRole('button', { name: 'Expandir Arrecife' })).toHaveCount(0);
}

async function captureTree(page: Page, fileName: string): Promise<void> {
  const directory = process.env['E2E_TERRITORIAL_CAPTURE_DIR'];
  if (!directory) return;
  await mkdir(directory, { recursive: true });
  await page.locator('.detail-modal').last().screenshot({ path: join(directory, fileName), animations: 'disabled' });
}

async function realTerritorialCircuit(context: ChromeScenarioContext): Promise<void> {
  if (!context.session.session) throw new Error('El circuit territorial real requereix sessió ADMIN.');

  await realAsyncImportCircuit(context);

  await context.page.goto('/admin/territori');
  await context.page.getByRole('button', { name: 'Catàleg territorial' }).click();
  await context.page.getByLabel('País').selectOption({ label: 'País E2E À' });
  await context.page.getByLabel('Cerca').fill('E2E-001');
  await context.page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await context.page.getByRole('cell', { name: 'Vila E2E À', exact: true }).click();
  await expect(context.page.getByRole('dialog')).toBeVisible();
  await context.page.getByRole('button', { name: 'Coordenades' }).click();
  await expect(context.page.getByRole('definition').filter({ hasText: '40.4168' })).toBeVisible();
  await context.page.getByRole('button', { name: 'General' }).click();
  const deactivate = context.page.getByRole('button', { name: 'Desactivar' });
  await deactivate.click();
  const reason = context.page.getByLabel('Motiu *');
  const confirmDeactivate = context.page.getByRole('alertdialog').getByRole('button', { name: 'Desactivar' });
  await expect(confirmDeactivate).toBeDisabled();
  await reason.click();
  await reason.pressSequentially('ab');
  await expect(reason).toHaveValue('ab');
  await expect(confirmDeactivate).toBeDisabled();
  await reason.pressSequentially('c');
  await expect(reason).toHaveValue('abc');
  await expect(confirmDeactivate).toBeEnabled();
  await reason.pressSequentially(' Circuit E2E real fase VI');
  const deactivateRequest = context.page.waitForRequest((request) =>
    request.method() === 'POST' && request.url().endsWith('/api/admin/territorial/catalog/e2e00000-0000-0000-0000-000000000101/maintenance')
  );
  await confirmDeactivate.click();
  expect((await deactivateRequest).postDataJSON()).toMatchObject({ action: 'deactivate', reason: 'abc Circuit E2E real fase VI' });
  const currentDeactivation = context.page.locator('.audit-list li').filter({ hasText: 'abc Circuit E2E real fase VI' }).first();
  await expect(currentDeactivation).toContainText('AbansActiu');
  await expect(currentDeactivation).toContainText('DesprésInactiu');
  await context.page.getByRole('button', { name: 'General' }).click();
  await context.page.getByRole('button', { name: 'Reactivar' }).click();
  const reactivationReason = context.page.getByLabel('Motiu *');
  await reactivationReason.pressSequentially('Reactivació E2E real fase VI');
  const reactivateRequest = context.page.waitForRequest((request) =>
    request.method() === 'POST' && request.url().endsWith('/api/admin/territorial/catalog/e2e00000-0000-0000-0000-000000000101/maintenance')
  );
  await context.page.getByRole('alertdialog').getByRole('button', { name: 'Reactivar' }).click();
  expect((await reactivateRequest).postDataJSON()).toMatchObject({ action: 'activate', reason: 'Reactivació E2E real fase VI' });
  const currentReactivation = context.page.locator('.audit-list li').filter({ hasText: 'Reactivació E2E real fase VI' }).first();
  await expect(currentReactivation).toContainText('AbansInactiu');
  await expect(currentReactivation).toContainText('DesprésActiu');
  await context.page.getByRole('button', { name: 'Tancar el detall territorial' }).click();
  await expect(context.page.getByRole('cell', { name: 'Vila E2E À', exact: true })).toBeVisible();
  await expect(context.page.getByLabel('País')).toHaveValue('e2e00000-0000-0000-0000-000000000001');
  await context.page.goto('/perfil');
  const location = context.page.locator('app-territorial-location-selector');
  await assertSingleLocationAutocomplete(location);
  const country = location.getByLabel('País');
  const locality = location.getByRole('combobox', { name: 'Localitat', exact: true });
  await expect(locality).toBeDisabled();
  await expect(locality).toHaveAttribute('placeholder', 'Selecciona primer un país');
  await country.selectOption({ label: 'País E2E À' });
  await locality.fill('Vila');
  await location.getByRole('option').filter({ hasText: 'Vila E2E À' }).click();
  await expect(locality).toHaveValue('Vila E2E À');
  await country.selectOption({ label: 'País E2E B' });
  await expect(locality).toHaveValue('');
  await locality.fill('Vila');
  await location.getByRole('option').filter({ hasText: 'Vila E2E B' }).click();
  await expect(locality).toHaveValue('Vila E2E B');

  await context.page.goto('/places');
  const placesLocation = context.page.locator('app-place-filters app-territorial-location-selector');
  await assertSingleLocationAutocomplete(placesLocation);
  await placesLocation.getByLabel('País').selectOption({ label: 'País E2E À' });
  await placesLocation.getByRole('combobox', { name: 'Localitat', exact: true }).fill('Vila');
  await placesLocation.getByRole('option').filter({ hasText: 'Vila E2E À' }).click();
  await expect(context.page.getByRole('button', { name: 'Cercar', exact: true })).toBeVisible();
  await assertResponsiveFilterLayout(context.page, context.page.locator('app-place-filters'), false);
  await context.page.getByRole('button', { name: 'Cercar', exact: true }).click();

  await context.favoriteFactory.create(context.identity, context.session.session, context.cleanup);
  await context.page.goto('/favorites');
  const favoritesLocation = context.page.locator('app-place-filters app-territorial-location-selector');
  await assertSingleLocationAutocomplete(favoritesLocation);
  await expect(context.page.getByRole('button', { name: 'Cercar', exact: true })).toBeVisible();
  await assertResponsiveFilterLayout(context.page, context.page.locator('app-place-filters'), true);

  await context.page.goto('/admin/usuaris');
  await context.page.getByRole('button', { name: 'Crear usuari' }).click();
  await assertSingleLocationAutocomplete(context.page.locator('app-territorial-location-selector'));

  await context.page.goto('/admin/llocs');
  await context.page.getByRole('button', { name: 'Nou lloc' }).click();
  await assertSingleLocationAutocomplete(context.page.locator('app-territorial-location-selector'));

  await context.page.evaluate(() => localStorage.removeItem('zuppeto-auth-session'));
  await context.page.goto('/login', { waitUntil: 'domcontentloaded' });
  const publicLocation = context.page.locator('app-territorial-location-selector');
  await assertSingleLocationAutocomplete(publicLocation);
  await expect(context.page.getByText(/Compatibilitat transitòria/i)).toHaveCount(0);
}

async function realAsyncImportCircuit(context: ChromeScenarioContext): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  const sheet = workbook.addWorksheet('Data');
  sheet.addRow(['CODE', 'NAME']);
  for (let index = 1; index <= 250; index += 1) {
    sheet.addRow([`E2E-${String(index).padStart(3, '0')}`, `Localitat worker E2E ${index}`]);
  }
  const content = Buffer.from(await workbook.xlsx.writeBuffer());

  await context.page.goto('/admin/territori');
  await context.page.getByLabel('Font oficial').selectOption({ label: 'Organisme E2E · Dataset worker E2E' });
  await context.page.locator('input[type=file]').setInputFiles({
    name: 'territori-worker-e2e.xlsx',
    mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    buffer: content
  });
  await context.page.getByRole('button', { name: 'Inspeccionar esquema' }).click();
  await expect(context.page.getByRole('heading', { name: '3. Mapping reutilitzable' })).toBeVisible();
  await expect(context.page.getByText('B: Localitat worker E2E 1 ·', { exact: true })).toBeVisible();

  await context.page.getByLabel('Columna de nom').selectOption({ label: 'NAME' });
  await context.page.getByLabel('Columnes clau (separades per coma)').fill('CODE');
  await context.page.getByLabel('Esquema del codi').fill('e2e:worker');
  await context.page.getByLabel('Columnes del codi').fill('CODE');
  await context.page.getByLabel('Locale del nom').fill('ca-ES');
  await context.page.getByRole('button', { name: 'Desar nova versió de mapping' }).click();
  await expect(context.page.getByRole('button', { name: 'Preparar validació i preview' })).toBeEnabled();

  const queuedResponse = context.page.waitForResponse((response) =>
    response.request().method() === 'POST' && response.url().endsWith('/api/admin/territorial/imports'));
  await context.page.getByRole('button', { name: 'Preparar validació i preview' }).click();
  const queued = await queuedResponse;
  expect(queued.status()).toBe(202);
  const queuedBody = await queued.json() as { import: { id: string; status: string } };
  const queuedId = queuedBody.import.id;
  expect(queuedBody.import.status).toBe('Queued');

  // Angular pot desaparèixer: la cua, l'artefacte i el worker continuen sent la font de veritat.
  await context.page.goto('/');
  const token = context.session.session?.accessToken;
  await expect.poll(async () => {
    const response = await context.transport.send<{ import: { status: string } }>({
      method: 'GET', path: `/api/admin/territorial/imports/${queuedId}`, accessToken: token
    });
    return response.body?.import.status;
  }, { timeout: 45_000, intervals: [250, 500, 1000] }).toBe('ReadyForReview');

  await context.page.goto('/admin/territori');
  await context.page.reload();
  await context.page.getByRole('button', { name: 'Historial' }).click();
  const historyRow = context.page.getByRole('row').filter({ hasText: 'Dataset worker E2E' }).first();
  await expect(historyRow).toContainText('Preparat per revisar');
  await expect(historyRow).toContainText('250/250');
  await historyRow.getByRole('button', { name: 'Obrir detall' }).click();
  await expect(context.page).toHaveURL(new RegExp(`/admin/territori/${queuedId}$`));
  await expect(context.page.getByRole('heading', { name: '5. Canvis a publicar' })).toBeVisible();
  await context.page.getByRole('button', { name: 'Anar a publicació' }).click();
  await context.page.getByRole('button', { name: 'Publicar catàleg' }).click();
  await context.page.getByRole('alertdialog').getByRole('button', { name: 'Confirmar' }).click();
  await expect(context.page.getByText('Estat: Publicat')).toBeVisible({ timeout: 45_000 });

  await context.page.getByRole('button', { name: 'Catàleg territorial' }).click();
  await context.page.getByLabel('País').selectOption({ label: 'País E2E Importació' });
  await context.page.getByLabel('Cerca').fill('E2E-001');
  await context.page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await expect(context.page.getByRole('cell', { name: 'Localitat worker E2E 1', exact: true })).toBeVisible();
}

async function assertSingleLocationAutocomplete(location: Locator): Promise<void> {
  await expect(location).toBeVisible();
  await expect(location.locator('select')).toHaveCount(1);
  await expect(location.getByRole('combobox')).toHaveCount(2);
  await expect(location.getByRole('button', { name: 'Cercar', exact: true })).toHaveCount(0);
  await expect(location.getByText(/Compatibilitat transitòria/i)).toHaveCount(0);
}

async function assertFunctionalChangeDetails(page: Page): Promise<void> {
  const filters = page.locator('[aria-label="Filtres dels canvis"]');
  await filters.getByLabel('Cerca per nom o codi').fill('Àmbit');
  await filters.getByRole('button', { name: 'Aplicar filtres' }).click();
  await expect(page.getByRole('cell', { name: 'Crear', exact: true }).first()).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Actualitzar', exact: true })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Desactivar', exact: true })).toBeVisible();
  await expect(page.locator('details.change-detail')).toHaveCount(0);
  const firstDetail = page.getByRole('button', { name: 'Veure detall' }).first();
  await firstDetail.click();
  const modal = page.getByRole('dialog');
  await expect(modal).toBeVisible();
  await expect(modal.getByRole('heading', { name: 'Regió sintètica' })).toBeVisible();
  await expect(modal.getByText('Regió · Arrel territorial · R1')).toBeVisible();
  await expect(modal.getByRole('button', { name: 'General' })).toBeVisible();
  await expect(modal.getByRole('button', { name: 'Jerarquia' })).toBeVisible();
  await expect(modal.getByRole('button', { name: 'Noms i codis' })).toBeVisible();
  await expect(modal.getByRole('button', { name: 'Procedència' })).toBeVisible();
  await expect(modal.getByRole('button', { name: 'Canvis' })).toBeVisible();
  await modal.getByRole('button', { name: 'Jerarquia' }).click();
  await expect(modal).toContainText('País sintètic');
  await expect(modal.locator('.hierarchy-current')).toContainText('Regió sintètica');
  await expect(modal.locator('.hierarchy-children-table')).toContainText('Província sintètica');
  await modal.locator('.hierarchy-children-table').getByRole('button', { name: 'Veure detall' }).click();
  await expect(modal.locator('.hierarchy-current')).toContainText('Província sintètica');
  await expect(modal.locator('.hierarchy-children-table')).toContainText('Àmbit sintètic');
  await modal.locator('.hierarchy-children-table').getByRole('button', { name: 'Veure detall' }).click();
  await expect(modal.locator('.hierarchy-current')).toContainText('Àmbit sintètic');
  await modal.locator('.change-hierarchy').getByRole('button', { name: 'Província sintètica' }).click();
  await expect(modal.locator('.hierarchy-current')).toContainText('Província sintètica');
  await modal.locator('.change-hierarchy').getByRole('button', { name: 'Regió sintètica' }).click();
  await expect(modal.locator('.hierarchy-current')).toContainText('Regió sintètica');
  await modal.getByRole('button', { name: 'Canvis' }).click();
  await expect(modal).toContainText('No existeix');
  await expect(modal).toContainText('Aquesta unitat territorial encara no existeix i es crearà en publicar.');
  await page.keyboard.press('Escape');
  await expect(modal).toHaveCount(0);
  await expect(firstDetail).toBeFocused();
  await expect(filters.getByLabel('Cerca per nom o codi')).toHaveValue('Àmbit');
  await expect(page.locator('pre')).toHaveCount(0);
  await expect(page.getByText('canonicalUnitKey')).toHaveCount(0);
}

async function assertResponsiveFilterLayout(page: Page, filters: Locator, includesSort: boolean): Promise<void> {
  const territory = filters.locator('app-territorial-location-selector');
  const controls = [
    filters.locator('.place-filters__search input'),
    territory.locator('select'),
    territory.locator('input[role="combobox"]'),
    filters.locator('.place-filters__type select'),
    filters.locator('.place-filters__pet select'),
    ...(includesSort ? [filters.locator('.place-filters__sort select')] : [])
  ];
  for (const width of [1280, 900, 600]) {
    await page.setViewportSize({ width, height: 900 });
    const boxes = await Promise.all(controls.map((control) => control.boundingBox()));
    boxes.forEach((box) => {
      expect(box).not.toBeNull();
      expect(box!.x).toBeGreaterThanOrEqual(0);
      expect(box!.x + box!.width).toBeLessThanOrEqual(width + 1);
    });
    for (let left = 0; left < boxes.length; left += 1) {
      for (let right = left + 1; right < boxes.length; right += 1) {
        const a = boxes[left]!;
        const b = boxes[right]!;
        const separated = a.x + a.width <= b.x + 1 || b.x + b.width <= a.x + 1
          || a.y + a.height <= b.y + 1 || b.y + b.height <= a.y + 1;
        expect(separated).toBe(true);
      }
    }
    const columnWidths = await Promise.all(controls.map((control) => control.evaluate((element) => {
      const label = element.closest('label');
      return label?.getBoundingClientRect().width ?? 0;
    })));
    boxes.forEach((box, index) => expect(Math.abs(box!.width - columnWidths[index]!)).toBeLessThanOrEqual(2));
    expect(Math.max(...boxes.map((box) => box!.height)) - Math.min(...boxes.map((box) => box!.height))).toBeLessThanOrEqual(2);

    if (width === 1280) {
      expect(Math.max(...boxes.slice(0, 5).map((box) => box!.y)) - Math.min(...boxes.slice(0, 5).map((box) => box!.y))).toBeLessThanOrEqual(2);
      expect(boxes[0]!.width).toBeGreaterThan(boxes[1]!.width);
      expect(boxes[2]!.width).toBeGreaterThan(boxes[1]!.width);
    } else if (width === 900) {
      expect(new Set(boxes.map((box) => Math.round(box!.x))).size).toBeLessThanOrEqual(2);
    } else {
      expect(new Set(boxes.map((box) => Math.round(box!.x))).size).toBe(1);
    }
  }
  await page.setViewportSize({ width: 1280, height: 900 });
}

async function securityScenario(context: ChromeScenarioContext): Promise<void> {
  await context.page.goto('/admin/territori');
  await expect(context.page).not.toHaveURL(/\/admin\/territori/);
  const token = context.session.session?.accessToken;
  if (!token) throw new Error('La prova de seguretat territorial requereix sessió USER.');
  context.allowHttpStatus(403, '/api/admin/territorial/context');
  context.allowConsoleError('403');
  const response = await context.transport.send({
    method: 'GET', path: '/api/admin/territorial/context', accessToken: token
  });
  expect(response.status).toBe(403);
}

async function mockTerritorialApi(context: ChromeScenarioContext, mode: 'ready' | 'blocked', pendingSource = false): Promise<void> {
  let catalogActive = true;
  const audit: Array<Record<string, unknown>> = [];
  const contextDto = {
    countries: [{ id: 'c0000000-0000-0000-0000-000000000001', code: 'synthetic', name: 'País sintètic', iso2: 'ZZ', iso3: 'ZZZ', isActive: true }],
    sources: [{
      id: sourceId, countryId: 'c0000000-0000-0000-0000-000000000001',
      organisation: 'Institut sintètic', dataset: 'Àmbits de prova', approvalStatus: pendingSource ? 'Pending' : 'Approved',
      isActive: true, publicationMode: 'FullSnapshot', datasetVersion: 'synthetic-v1',
      datasetDate: '2026-09-22', license: 'Test-only', attribution: 'Fixture E2E'
    }],
    unitTypes: [{
      id: '40000000-0000-0000-0000-000000000001',
      countryId: 'c0000000-0000-0000-0000-000000000001',
      code: 'MUNICIPALITY', name: 'Municipi', displayOrder: 1, isSelectableLocality: true
    }]
  };
  const counters = mode === 'blocked'
    ? { rows: 2, errors: 1, warnings: 0, create: 0, update: 0, deactivate: 0, noChange: 0 }
    : { rows: 4, errors: 0, warnings: 0, create: 2, update: 1, deactivate: 1, noChange: 0 };
  const summary = {
    id: importId, countryId: contextDto.countries[0].id, countryName: 'País sintètic',
    datasetSourceId: sourceId, organisation: 'Institut sintètic', dataset: 'Àmbits de prova',
    datasetVersion: 'synthetic-v1', mappingTemplateId: mappingId, mappingVersion: 1,
    status: mode === 'blocked' ? 'Validated' : 'ReadyForReview',
    hasBlockingErrors: mode === 'blocked', catalogVersion: null,
    artifactName: 'territori-sintetic.xlsx', fileSize: 20, fileChecksum: 'a'.repeat(64),
    actor: 'Admin E2E', createdAtUtc: '2026-09-22T10:00:00Z',
    updatedAtUtc: '2026-09-22T10:00:00Z', publishedAtUtc: null, counters
  };
  const detail = {
    import: summary, publicationMode: 'FullSnapshot', failureReason: null,
    schemaFingerprint: 'b'.repeat(64), canCancel: true, canPublish: mode === 'ready',
    canRevert: false, currentCatalogVersion: 7, changeSetId: '50000000-0000-0000-0000-000000000001',
    changeSetStatus: 'Preview', sourceSheets: ['Comunitats', 'Municipis'], manualConflictCount: 0,
    territorialBreakdown: [
      { territorialUnitTypeCode: 'REGION', territorialUnitType: 'Regió', create: 1, update: 0, deactivate: 0, noChange: 0 },
      { territorialUnitTypeCode: 'MUNICIPALITY', territorialUnitType: 'Municipi', create: 1, update: 1, deactivate: 1, noChange: 0 }
    ]
  };
  const functionalChanges = [
    {
      id: '70000000-0000-0000-0000-000000000010', kind: 'Create', territorialUnitId: null,
      name: 'Regió sintètica', primaryCode: 'R1', territorialUnitTypeCode: 'REGION',
      territorialUnitType: 'Regió', country: 'País sintètic', parent: null, locale: 'ca-ES',
      isActive: true, isSelectableLocality: false, latitude: null, longitude: null,
      source: 'Institut sintètic · Àmbits de prova', hierarchy: ['País sintètic', 'Regió sintètica'],
      provenance: { organisation: 'Institut sintètic', dataset: 'Àmbits de prova', datasetVersion: 'synthetic-v1', datasetDate: '2026-09-22', mappingVersion: 1, locale: 'ca-ES', source: null },
      names: [{ name: 'Regió sintètica', locale: 'ca-ES', kind: 'Official', isPrimary: true }],
      codes: [{ scheme: 'synthetic:region', value: 'R1', isPrimary: true }], differences: [],
      functionalReason: 'Aquesta unitat territorial encara no existeix i es crearà en publicar.', hasManualConflict: false
    },
    {
      id: '70000000-0000-0000-0000-000000000001', kind: 'Create', territorialUnitId: null,
      name: 'Àmbit sintètic', primaryCode: '001', territorialUnitTypeCode: 'MUNICIPALITY',
      territorialUnitType: 'Municipi', country: 'País sintètic', parent: 'Regió sintètica', locale: 'ca-ES',
      isActive: true, isSelectableLocality: true, latitude: 41.5, longitude: 2.1,
      source: 'Institut sintètic · Àmbits de prova',
      hierarchy: ['País sintètic', 'Regió sintètica', 'Àmbit sintètic'],
      provenance: { organisation: 'Institut sintètic', dataset: 'Àmbits de prova', datasetVersion: 'synthetic-v1', datasetDate: '2026-09-22', mappingVersion: 1, locale: 'ca-ES', source: 'https://example.test/territori' },
      names: [{ name: 'Àmbit sintètic', locale: 'ca-ES', kind: 'Official', isPrimary: true }],
      codes: [{ scheme: 'synthetic:code', value: '001', isPrimary: true }], differences: [],
      functionalReason: 'Aquesta unitat territorial encara no existeix i es crearà en publicar.', hasManualConflict: false
    },
    {
      id: '70000000-0000-0000-0000-000000000002', kind: 'Update', territorialUnitId: '90000000-0000-0000-0000-000000000002',
      name: 'Àmbit actualitzat', primaryCode: '002', territorialUnitTypeCode: 'MUNICIPALITY',
      territorialUnitType: 'Municipi', country: 'País sintètic', parent: 'Regió sintètica', locale: 'ca-ES',
      isActive: true, isSelectableLocality: true, latitude: 41.6, longitude: 2.2,
      source: 'Institut sintètic · Àmbits de prova', hierarchy: ['País sintètic', 'Regió sintètica', 'Àmbit actualitzat'],
      provenance: { organisation: 'Institut sintètic', dataset: 'Àmbits de prova', datasetVersion: 'synthetic-v1', datasetDate: '2026-09-22', mappingVersion: 1, locale: 'ca-ES', source: null }, names: [], codes: [],
      differences: [{ field: 'names', before: 'Àmbit anterior', after: 'Àmbit actualitzat' }],
      functionalReason: null, hasManualConflict: false
    },
    {
      id: '70000000-0000-0000-0000-000000000003', kind: 'Deactivate', territorialUnitId: '90000000-0000-0000-0000-000000000003',
      name: 'Àmbit antic', primaryCode: '003', territorialUnitTypeCode: 'MUNICIPALITY',
      territorialUnitType: 'Municipi', country: 'País sintètic', parent: 'Regió sintètica', locale: 'ca-ES',
      isActive: false, isSelectableLocality: true, latitude: null, longitude: null,
      source: 'Institut sintètic · Àmbits de prova', hierarchy: ['País sintètic', 'Regió sintètica', 'Àmbit antic'],
      provenance: { organisation: 'Institut sintètic', dataset: 'Àmbits de prova', datasetVersion: 'synthetic-v1', datasetDate: '2026-09-22', mappingVersion: 1, locale: 'ca-ES', source: null }, names: [], codes: [],
      differences: [{ field: 'isActive', before: 'true', after: 'false' }],
      functionalReason: 'La unitat ja no apareix al dataset oficial actual.', hasManualConflict: false
    }
  ];
  const page = <T>(items: T[]) => ({ items, page: 1, pageSize: 50, totalCount: items.length, totalPages: items.length ? 1 : 0 });

  await context.page.route('**/api/admin/territorial/**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    let body: unknown;
    if (path.endsWith('/context')) body = contextDto;
    else if (path.endsWith('/workbooks/inspect')) body = {
      artifactName: 'territori-sintetic.xlsx', fileSize: 20, fileChecksum: 'a'.repeat(64),
      format: 'XLSX', schemaFingerprint: 'b'.repeat(64),
      sheets: [{ name: 'Municipis', detectedHeaderRow: 1, rowCount: 2, columns: [{ column: 'A', header: 'CODI' }, { column: 'B', header: 'NOM' }] }],
      compatibleMappings: [{ id: mappingId, datasetSourceId: sourceId, version: 1, schemaFingerprint: 'b'.repeat(64), definitionChecksum: 'c'.repeat(64), isActive: true, createdAtUtc: '2026-09-22T09:00:00Z' }]
    };
    else if (path.endsWith('/issues')) body = page(mode === 'blocked' ? [{
      id: '60000000-0000-0000-0000-000000000001', severity: 'Error', ruleCode: 'DUPLICATE_CODE',
      message: 'Codi territorial duplicat', sheet: 'Municipis', rowNumber: 2, field: 'CODI',
      problemValue: '001', canonicalUnitKey: 'synthetic:001'
    }] : []);
    else if (path.endsWith('/hierarchy')) {
      const changeId = path.split('/').at(-2);
      const hierarchyNode = (change: { id: string; name: string; primaryCode: string | null; territorialUnitType: string; kind: string }, conflict = false) => ({
        changeId: change.id, name: change.name, primaryCode: change.primaryCode,
        territorialUnitType: change.territorialUnitType, kind: change.kind, hasBlockingConflict: conflict
      });
      const region = functionalChanges[0]!;
      const municipality = functionalChanges[1]!;
      const province = {
        ...region, id: '70000000-0000-0000-0000-000000000011', name: 'Província sintètica', primaryCode: 'P1',
        territorialUnitTypeCode: 'PROVINCE', territorialUnitType: 'Província', parent: 'Regió sintètica',
        hierarchy: ['País sintètic', 'Regió sintètica', 'Província sintètica']
      };
      if (changeId === region.id) body = { current: region, ancestors: [], children: page([hierarchyNode(province)]) };
      else if (changeId === province.id) body = { current: province, ancestors: [hierarchyNode(region)], children: page([hierarchyNode(municipality)]) };
      else body = { current: municipality, ancestors: [hierarchyNode(region), hierarchyNode(province)], children: page([]) };
    }
    else if (path.endsWith('/changes')) body = mode === 'ready'
      ? { ...page(functionalChanges), totalCount: 8199, totalPages: 164 }
      : page([]);
    else if (path.endsWith('/preview/source')) {
      const sheet = new URL(request.url()).searchParams.get('sheet');
      body = page(sheet === 'Comunitats' ? [{
        id: '81000000-0000-0000-0000-000000000002', sheet: 'Comunitats', rowNumber: 3,
        values: { CODAUTO: '01', 'COMUNIDAD AUTÓNOMA': 'Andalucía' }, readingStatus: 'Llegida'
      }] : [{
        id: '81000000-0000-0000-0000-000000000001', sheet: 'Municipis', rowNumber: 31,
        values: { CMUN: '051', CODAUTO: '16', CPRO: '01', NOMBRE: 'Àmbit sintètic' }, readingStatus: 'Llegida'
      }]);
    }
    else if (path.endsWith('/preview/canonical')) body = {
      ...page([{
        id: '82000000-0000-0000-0000-000000000001', sheet: 'Comunitats', rowNumber: 3,
        canonicalUnitKey: 'synthetic:region:01', parentCanonicalUnitKey: null, name: 'Andalucía',
        locale: 'es-ES', territorialUnitTypeCode: 'AUTONOMOUS_COMMUNITY',
        codes: [{ scheme: 'synthetic:code', value: '01', isPrimary: true }],
        latitude: null, longitude: null, status: 'Vàlida', issueCount: 0, issues: []
      }, {
        id: '82000000-0000-0000-0000-000000000002', sheet: 'Municipis', rowNumber: 31,
        canonicalUnitKey: 'synthetic:001', parentCanonicalUnitKey: 'synthetic:region:01', name: 'Àmbit sintètic',
        locale: 'ca-ES', territorialUnitTypeCode: 'MUNICIPALITY',
        codes: [{ scheme: 'synthetic:code', value: '001', isPrimary: true }],
        latitude: 41.5, longitude: 2.1, status: 'Vàlida', issueCount: 1,
        issues: [{ severity: 'Warning', rule: 'COORDINATE_REVIEW', sheet: 'Municipis', rowNumber: 31,
          field: 'Coordenades', message: 'Cal revisar la procedència de les coordenades.' }]
      }]), totalCount: 8201, totalPages: 165
    };
    else if (path.endsWith('/catalog') && method === 'GET') body = page([catalogUnit(catalogActive)]);
    else if (path.endsWith('/catalog/90000000-0000-0000-0000-000000000001') && method === 'GET') body = catalogDetail(catalogActive, audit);
    else if (path.endsWith('/maintenance') && method === 'POST') {
      const requestBody = request.postDataJSON() as { action: string; reason: string };
      catalogActive = requestBody.action !== 'deactivate';
      audit.unshift({
        id: String(audit.length + 1), action: requestBody.action, field: 'isActive',
        beforeValue: String(!catalogActive), afterValue: String(catalogActive), reason: requestBody.reason,
        actor: 'Admin E2E', origin: 'Manual', createdAtUtc: '2026-09-23T10:00:00Z'
      });
      body = catalogDetail(catalogActive, audit);
    }
    else if (path.endsWith('/publish') && method === 'POST') body = {
      ...detail, import: { ...summary, status: 'Published', publishedAtUtc: '2026-09-22T10:01:00Z' },
      canCancel: false, canPublish: false, canRevert: true, currentCatalogVersion: 8, changeSetStatus: 'Published'
    };
    else if (path.endsWith('/imports') && method === 'GET') body = page([summary]);
    else if (path.endsWith('/imports') && method === 'POST') body = detail;
    else if (path.endsWith('/' + importId)) body = detail;
    else body = [];
    await route.fulfill({ status: method === 'POST' && path.endsWith('/imports') ? 201 : 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

function catalogUnit(isActive: boolean) {
  return {
    id: '90000000-0000-0000-0000-000000000001',
    countryId: 'c0000000-0000-0000-0000-000000000001', country: 'País sintètic',
    territorialUnitTypeId: '40000000-0000-0000-0000-000000000001', typeCode: 'MUNICIPALITY',
    type: 'Municipi', parentId: null, parent: null, primaryCode: '001', primaryName: 'Àmbit sintètic',
    locale: 'ca-ES', latitude: 41.5, longitude: 2.1, isActive, isSelectableLocality: true,
    hasManualOverride: !isActive,
    hasManualActiveOverride: !isActive,
    hasManualSelectableOverride: false,
    hasManualCoordinateOverride: false
  };
}

function catalogDetail(isActive: boolean, audit: Array<Record<string, unknown>>) {
  return {
    unit: catalogUnit(isActive), ancestors: [],
    names: [{ id: 'n1', name: 'Àmbit sintètic', locale: 'ca-ES', kind: 'Official', isPrimary: true, source: 'Institut sintètic · Àmbits de prova' }],
    codes: [{ id: 'c1', scheme: 'synthetic:code', value: '001', validFrom: null, validTo: null, isPrimary: true, source: 'Institut sintètic · Àmbits de prova' }],
    coordinateSource: 'Institut sintètic · Àmbits de prova', datasetSources: ['Àmbits de prova'],
    importIds: [importId], audit
  };
}
