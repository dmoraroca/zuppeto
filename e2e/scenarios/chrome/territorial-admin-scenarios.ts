import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

const sourceId = '10000000-0000-0000-0000-000000000001';
const mappingId = '20000000-0000-0000-0000-000000000001';
const importId = '30000000-0000-0000-0000-000000000001';

export async function executeTerritorialAdminScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  if (code === 155) return securityScenario(context);
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
    await context.page.getByRole('cell', { name: 'Àmbit sintètic' }).click();
    await expect(context.page.getByRole('heading', { name: 'Àmbit sintètic' })).toBeVisible();
    await expect(context.page.getByRole('definition').filter({ hasText: 'País sintètic' })).toBeVisible();
    await context.page.getByLabel('Motiu obligatori').fill('Revisió E2E controlada');
    await context.page.getByRole('button', { name: 'Desactivar' }).click();
    await expect(context.page.getByText('Admin E2E · deactivate')).toBeVisible();
    await context.page.getByLabel('Motiu obligatori').fill('Reactivació E2E controlada');
    await context.page.getByRole('button', { name: 'Reactivar' }).click();
    await expect(context.page.getByText('Admin E2E · activate')).toBeVisible();
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
    hasManualOverride: !isActive
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
