import { expect, type Locator, type Page } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

const sourceId = '10000000-0000-0000-0000-000000000001';
const mappingId = '20000000-0000-0000-0000-000000000001';
const importId = '30000000-0000-0000-0000-000000000001';

export async function executeTerritorialAdminScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  if (code === 155) return securityScenario(context);
  if (code === 160 && process.env['E2E_TERRITORIAL_REAL'] === 'true') return realTerritorialCircuit(context);
  const mode = code === 158 ? 'blocked' : 'ready';
  await mockTerritorialApi(context, mode);

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
    await expect(context.page.getByRole('button', { name: 'Revisar ChangeSet' })).toHaveCount(0);
    return;
  }

  await expect(context.page.getByRole('heading', { name: '5. Preview del ChangeSet' })).toBeVisible();
  await expect(context.page.getByText('synthetic:001').first()).toBeVisible();
  if (code === 157) {
    await expect(context.page.getByRole('heading', { name: 'Preview d’origen' })).toBeVisible();
    await expect(context.page.getByRole('heading', { name: 'Preview canonicalitzat' })).toBeVisible();
    await expect(context.page.getByText('CODI: 001')).toBeVisible();
    await expect(context.page.getByText('Àmbit sintètic · ca-ES')).toBeVisible();
    await expect(context.page.getByRole('heading', { name: '5. Preview del ChangeSet' })).toBeVisible();
    return;
  }

  await context.page.getByRole('button', { name: 'Anar a publicació' }).click();
  if (code === 159) {
    await context.page.getByRole('button', { name: 'Publicar catàleg' }).click();
    await expect(context.page.getByRole('alertdialog')).toBeVisible();
    await context.page.getByRole('button', { name: 'Confirmar' }).click();
    await expect(context.page.getByText('Estat: Published')).toBeVisible();
  }
}

async function realTerritorialCircuit(context: ChromeScenarioContext): Promise<void> {
  if (!context.session.session) throw new Error('El circuit territorial real requereix sessió ADMIN.');

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

async function assertSingleLocationAutocomplete(location: Locator): Promise<void> {
  await expect(location).toBeVisible();
  await expect(location.locator('select')).toHaveCount(1);
  await expect(location.getByRole('combobox')).toHaveCount(2);
  await expect(location.getByRole('button', { name: 'Cercar', exact: true })).toHaveCount(0);
  await expect(location.getByText(/Compatibilitat transitòria/i)).toHaveCount(0);
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

async function mockTerritorialApi(context: ChromeScenarioContext, mode: 'ready' | 'blocked'): Promise<void> {
  let catalogActive = true;
  const audit: Array<Record<string, unknown>> = [];
  const contextDto = {
    countries: [{ id: 'c0000000-0000-0000-0000-000000000001', code: 'synthetic', name: 'País sintètic', iso2: 'ZZ', iso3: 'ZZZ', isActive: true }],
    sources: [{
      id: sourceId, countryId: 'c0000000-0000-0000-0000-000000000001',
      organisation: 'Institut sintètic', dataset: 'Àmbits de prova', approvalStatus: 'Approved',
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
    : { rows: 2, errors: 0, warnings: 0, create: 1, update: 0, deactivate: 0, noChange: 0 };
  const summary = {
    id: importId, countryId: contextDto.countries[0].id, countryName: 'País sintètic',
    datasetSourceId: sourceId, organisation: 'Institut sintètic', dataset: 'Àmbits de prova',
    datasetVersion: 'synthetic-v1', mappingTemplateId: mappingId, mappingVersion: 1,
    status: mode === 'blocked' ? 'ValidationFailed' : 'ReadyForReview',
    hasBlockingErrors: mode === 'blocked', catalogVersion: null,
    artifactName: 'territori-sintetic.xlsx', fileSize: 20, fileChecksum: 'a'.repeat(64),
    actor: 'Admin E2E', createdAtUtc: '2026-09-22T10:00:00Z',
    updatedAtUtc: '2026-09-22T10:00:00Z', publishedAtUtc: null, counters
  };
  const detail = {
    import: summary, publicationMode: 'FullSnapshot', failureReason: null,
    schemaFingerprint: 'b'.repeat(64), canCancel: true, canPublish: mode === 'ready',
    canRevert: false, currentCatalogVersion: 7, changeSetId: '50000000-0000-0000-0000-000000000001',
    changeSetStatus: 'Preview'
  };
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
    else if (path.endsWith('/changes')) body = page(mode === 'ready' ? [{
      id: '70000000-0000-0000-0000-000000000001', kind: 'Create', territorialUnitId: null,
      canonicalUnitKey: 'synthetic:001', before: null, after: { name: 'Àmbit sintètic' }, changedFields: ['name']
    }] : []);
    else if (path.endsWith('/preview/source')) body = page([{
      id: '81000000-0000-0000-0000-000000000001', sheet: 'Municipis', rowNumber: 2,
      values: { CODI: '001', NOM: 'Àmbit sintètic' }, readingStatus: 'Llegida'
    }]);
    else if (path.endsWith('/preview/canonical')) body = page([{
      id: '82000000-0000-0000-0000-000000000001', sheet: 'Municipis', rowNumber: 2,
      canonicalUnitKey: 'synthetic:001', parentCanonicalUnitKey: null, name: 'Àmbit sintètic',
      locale: 'ca-ES', territorialUnitTypeCode: 'MUNICIPALITY',
      codes: [{ scheme: 'synthetic:code', value: '001', isPrimary: true }],
      latitude: 41.5, longitude: 2.1, status: 'Vàlida', issueCount: 0
    }]);
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
