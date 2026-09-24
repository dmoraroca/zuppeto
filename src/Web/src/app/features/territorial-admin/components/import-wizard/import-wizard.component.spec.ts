import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import { ChangeItem, ImportDetail, TerritorialContext } from '../../models/territorial-admin.model';
import { TerritorialImportWizardComponent } from './import-wizard.component';

describe('TerritorialImportWizardComponent functional change presentation', () => {
  it('tolerates an import detail without the new preview aggregates during a rolling update', async () => {
    const legacyDetail = detail();
    delete legacyDetail.sourceSheets;
    delete legacyDetail.manualConflictCount;
    delete legacyDetail.territorialBreakdown;
    const api = {
      detail: vi.fn().mockResolvedValue(legacyDetail), issues: vi.fn().mockResolvedValue(page([])),
      changes: vi.fn().mockResolvedValue(page([])), sourcePreview: vi.fn().mockResolvedValue(page([])),
      canonicalPreview: vi.fn().mockResolvedValue(page([]))
    };
    await TestBed.configureTestingModule({
      imports: [TerritorialImportWizardComponent],
      providers: [{ provide: TerritorialAdminApiService, useValue: api }]
    }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialImportWizardComponent);
    fixture.componentRef.setInput('context', context());
    fixture.detectChanges();

    await (fixture.componentInstance as unknown as { open(id: string): Promise<void> }).open('legacy-import');
    fixture.detectChanges();

    expect(fixture.debugElement.queryAll(By.css('.alert--error'))).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('5. Canvis a publicar');
  });

  it('renders translated status, human rows and structured Create/Update/Deactivate details without raw JSON', async () => {
    const changes = [
      change('Create', 'Arenys de Mar', '08006', 'Municipi', 'Maresme', [], 'Aquesta unitat territorial encara no existeix i es crearà en publicar.'),
      change('Update', 'Vila actualitzada', '08007', 'Municipi', 'Maresme', [{ field: 'coordinates', before: '—', after: '41.5812, 2.5503', hasManualConflict: false }], null),
      change('Deactivate', 'Vila antiga', '08008', 'Municipi', 'Maresme', [{ field: 'isActive', before: 'Sí', after: 'No', hasManualConflict: false }], 'La unitat ja no apareix al dataset oficial actual.')
    ];
    const catalunya = { ...change('Create', 'Catalunya', '09', 'Comunitat autònoma', 'Espanya', [], 'Aquesta unitat territorial encara no existeix i es crearà en publicar.'), id: 'catalunya-id', territorialUnitTypeCode: 'AUTONOMOUS_COMMUNITY' };
    const barcelona = { ...change('Create', 'Barcelona', '08', 'Província', 'Catalunya', [], null), id: 'barcelona-id', territorialUnitTypeCode: 'PROVINCE' };
    const arenys = changes[0];
    const node = (item: ChangeItem, conflict = false) => ({
      changeId: item.id, name: item.name, primaryCode: item.primaryCode,
      territorialUnitType: item.territorialUnitType, kind: item.kind, hasBlockingConflict: conflict
    });
    const api = {
      detail: vi.fn().mockResolvedValue(detail()),
      issues: vi.fn().mockResolvedValue(page([])),
      changes: vi.fn().mockResolvedValue({ ...page(changes), page: 2, totalCount: 8199, totalPages: 164 }),
      sourcePreview: vi.fn().mockImplementation((_id: string, filters: { sheet?: string } = {}) => Promise.resolve(page(
        filters.sheet === 'Comunitats'
          ? [{ id: 'source-1', sheet: 'Comunitats', rowNumber: 3, values: { CODAUTO: '01', 'COMUNIDAD AUTÓNOMA': 'Andalucía' }, readingStatus: 'Llegida' }]
          : [{ id: 'source-2', sheet: 'Municipis', rowNumber: 31, values: { CMUN: '051', CODAUTO: '16', CPRO: '01', NOMBRE: 'Agurain/Salvatierra' }, readingStatus: 'Llegida' }]
      ))),
      canonicalPreview: vi.fn().mockResolvedValue({
        ...page([
          canonicalRow('canonical-1', 'Comunitats', 3, 'Andalucía', '01', []),
          canonicalRow('canonical-2', 'Municipis', 31, 'Municipi amb avís', '00031', [{
            severity: 'Warning', rule: 'COORDINATE_REVIEW', sheet: 'Municipis', rowNumber: 31,
            field: 'Coordenades', message: 'Cal revisar la procedència de les coordenades.'
          }])
        ]),
        totalCount: 8201, totalPages: 165
      }),
      changeHierarchy: vi.fn().mockImplementation((_importId: string, changeId: string, filters: { search?: string; page?: number } = {}) => {
        if (changeId === catalunya.id) return Promise.resolve({
          current: catalunya, ancestors: [],
          children: { ...page([node(barcelona), { ...node(change('Create', 'Girona', '17', 'Província', 'Catalunya', [], null), true), changeId: 'girona-id' }]), totalCount: filters.search ? 1 : 4, totalPages: 1 }
        });
        if (changeId === barcelona.id) {
          const child = filters.page === 2
            ? { ...node(change('Create', 'Vila de prova', '08999', 'Municipi', 'Barcelona', [], null)), changeId: 'vila-page-2' }
            : node(arenys);
          return Promise.resolve({
            current: barcelona, ancestors: [node(catalunya)],
            children: { items: [child], page: filters.page ?? 1, pageSize: 25, totalCount: 26, totalPages: 2 }
          });
        }
        return Promise.resolve({
          current: arenys, ancestors: [node(catalunya), node(barcelona)], children: page([])
        });
      })
    };
    await TestBed.configureTestingModule({
      imports: [TerritorialImportWizardComponent],
      providers: [{ provide: TerritorialAdminApiService, useValue: api }]
    }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialImportWizardComponent);
    fixture.componentRef.setInput('context', context());
    fixture.detectChanges();
    await (fixture.componentInstance as unknown as { open(id: string): Promise<void> }).open('import-1');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Preparat per revisar');
    expect(text).toContain('Canvis a publicar');
    expect(text).toContain('Crear');
    expect(text).toContain('Actualitzar');
    expect(text).toContain('Desactivar');
    expect(text).toContain('Arenys de Mar');
    expect(text).toContain('08006');
    expect(text).toContain('Municipi');
    expect(text).toContain('Maresme');
    expect(text).toContain('Pàgina 2 de 164');
    expect(text).toContain('Pàgina 1 de 165');
    expect(text).toContain('Comunitats · fila 3');
    expect(text).toContain('Municipis · fila 31');
    expect(text).toContain('Incidències');
    expect(text).toContain('Vàlida');
    expect(text).not.toContain('Vàlida (0)');
    expect(text).toContain('Cal revisar la procedència de les coordenades.');
    expect(fixture.debugElement.queryAll(By.css('.canonical-issues'))).toHaveLength(1);
    expect(fixture.debugElement.queryAll(By.css('.canonical-issues summary'))[0].nativeElement.textContent).toContain('Veure incidències');
    expect(fixture.debugElement.queryAll(By.css('details.change-detail'))).toHaveLength(0);
    expect(fixture.debugElement.queryAll(By.css('.change-detail-modal'))).toHaveLength(0);
    expect(text).not.toContain('Camps admesos');
    expect(text).not.toContain('canonicalUnitKey');
    expect(text).not.toContain('territorialUnitTypeCode');
    expect(fixture.debugElement.queryAll(By.css('pre'))).toHaveLength(0);

    const search = fixture.debugElement.query(By.css('input[aria-label="Cerca per nom o codi"]')).nativeElement as HTMLInputElement;
    search.value = 'Arenys de Mar';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.debugElement.query(By.css('[aria-label="Filtres dels canvis"] button')).nativeElement.click();
    await fixture.whenStable();
    expect(api.changes).toHaveBeenLastCalledWith('import-1', expect.objectContaining({ search: 'Arenys de Mar' }));

    const sheet = fixture.debugElement.query(By.css('select[aria-label="Full del preview"]')).nativeElement as HTMLSelectElement;
    sheet.value = 'Municipis';
    sheet.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    fixture.detectChanges();
    const sourceTableText = fixture.debugElement.query(By.css('.source-preview-table')).nativeElement.textContent as string;
    expect(sourceTableText).toContain('CMUN');
    expect(sourceTableText).toContain('CODAUTO');
    expect(sourceTableText).toContain('CPRO');
    expect(sourceTableText).toContain('NOMBRE');
    expect(sourceTableText).toContain('Agurain/Salvatierra');
    expect(sourceTableText).not.toContain('COMUNIDAD AUTÓNOMA');

    const detailButtons = fixture.debugElement.queryAll(By.css('.detail-link'));
    const returnTarget = detailButtons[0].nativeElement as HTMLButtonElement;
    returnTarget.focus();
    returnTarget.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.debugElement.queryAll(By.css('.change-detail-modal'))).toHaveLength(1);
    expect(fixture.debugElement.queryAll(By.css('.change-detail-tabs button'))).toHaveLength(5);
    let modalText = fixture.debugElement.query(By.css('.change-detail-modal')).nativeElement.textContent as string;
    expect(modalText).toContain('Arenys de Mar');
    expect(modalText).toContain('Municipi · Maresme · 08006');
    expect(modalText).toContain('Coordenades');

    const tabButtons = fixture.debugElement.queryAll(By.css('.change-detail-tabs button'));
    (tabButtons[1].nativeElement as HTMLButtonElement).click();
    fixture.detectChanges();
    modalText = fixture.debugElement.query(By.css('.change-detail-modal')).nativeElement.textContent as string;
    expect(modalText).toContain('Espanya');
    expect(modalText).toContain('Catalunya');
    expect(modalText).toContain('Barcelona');
    expect(modalText).toContain('Aquesta unitat no té fills directes');

    const catalunyaBreadcrumb = [...fixture.nativeElement.querySelectorAll('.change-hierarchy button')]
      .find((button: Element) => button.textContent?.trim() === 'Catalunya') as HTMLButtonElement;
    catalunyaBreadcrumb.click();
    await fixture.whenStable();
    fixture.detectChanges();
    modalText = fixture.debugElement.query(By.css('.change-detail-modal')).nativeElement.textContent as string;
    expect(modalText).toContain('Unitat actual');
    expect(modalText).toContain('Barcelona');
    expect(modalText).toContain('Girona');
    expect(modalText).toContain('Conflicte: revisió obligatòria');

    const childSearch = fixture.debugElement.query(By.css('input[aria-label="Cerca fills per nom o codi"]')).nativeElement as HTMLInputElement;
    childSearch.value = 'Barcelona';
    childSearch.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.debugElement.query(By.css('[aria-label="Cerca de fills directes"] button')).nativeElement.click();
    await fixture.whenStable();
    expect(api.changeHierarchy).toHaveBeenLastCalledWith('import-1', catalunya.id, expect.objectContaining({ search: 'Barcelona', page: 1 }));

    const barcelonaDetail = fixture.debugElement.query(By.css('.hierarchy-children-table .detail-link')).nativeElement as HTMLButtonElement;
    barcelonaDetail.click();
    await fixture.whenStable();
    fixture.detectChanges();
    modalText = fixture.debugElement.query(By.css('.change-detail-modal')).nativeElement.textContent as string;
    expect(modalText).toContain('Barcelona');
    expect(modalText).toContain('Arenys de Mar');
    expect(modalText).toContain('Pàgina 1 de 2');
    const nextChildren = [...fixture.nativeElement.querySelectorAll('.change-detail-content .pager button')]
      .find((button: Element) => button.textContent?.trim() === 'Següent') as HTMLButtonElement;
    nextChildren.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.change-detail-modal')).nativeElement.textContent).toContain('Vila de prova');
    expect(api.changeHierarchy).toHaveBeenLastCalledWith('import-1', barcelona.id, expect.objectContaining({ page: 2 }));

    const backToCatalunya = [...fixture.nativeElement.querySelectorAll('.change-hierarchy button')]
      .find((button: Element) => button.textContent?.trim() === 'Catalunya') as HTMLButtonElement;
    backToCatalunya.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.hierarchy-current')).nativeElement.textContent).toContain('Catalunya');

    const currentTabButtons = fixture.debugElement.queryAll(By.css('.change-detail-tabs button'));
    (currentTabButtons[4].nativeElement as HTMLButtonElement).click();
    fixture.detectChanges();
    modalText = fixture.debugElement.query(By.css('.change-detail-modal')).nativeElement.textContent as string;
    expect(modalText).toContain('No existeix');
    expect(modalText).toContain('Aquesta unitat territorial encara no existeix');

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.debugElement.queryAll(By.css('.change-detail-modal'))).toHaveLength(0);
    expect(document.activeElement).toBe(returnTarget);

    const publicationButton = [...fixture.nativeElement.querySelectorAll('button')]
      .find((button: Element) => button.textContent?.trim() === 'Anar a publicació') as HTMLButtonElement;
    publicationButton.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('6. Publicació del catàleg territorial');
    expect(fixture.nativeElement.textContent).not.toContain('5. Canvis a publicar');
    expect(fixture.nativeElement.textContent).toContain('Pendent d’aprovació');
    expect(fixture.debugElement.query(By.css('button.primary[disabled]')).nativeElement.textContent).toContain('Publicar catàleg');
    const backButton = [...fixture.nativeElement.querySelectorAll('button')]
      .find((button: Element) => button.textContent?.trim() === 'Tornar als canvis') as HTMLButtonElement;
    backButton.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Pàgina 2 de 164');
    expect((fixture.debugElement.query(By.css('input[aria-label="Cerca per nom o codi"]')).nativeElement as HTMLInputElement).value).toBe('Arenys de Mar');
  });
});

function canonicalRow(id: string, sheet: string, rowNumber: number, name: string, code: string,
  issues: Array<{ severity: string; rule: string; sheet: string; rowNumber: number; field: string | null; message: string }>) {
  return {
    id, sheet, rowNumber, canonicalUnitKey: `internal:${code}`, parentCanonicalUnitKey: null,
    name, locale: 'es-ES', territorialUnitTypeCode: sheet === 'Comunitats' ? 'AUTONOMOUS_COMMUNITY' : 'MUNICIPALITY',
    codes: [{ scheme: 'es:ine', value: code, isPrimary: true }], latitude: null, longitude: null,
    status: issues.some(issue => issue.severity === 'Error') ? 'Invàlida' : 'Vàlida', issueCount: issues.length, issues
  };
}

function change(kind: string, name: string, code: string, type: string, parent: string, differences: ChangeItem['differences'], reason: string | null): ChangeItem {
  return {
    id: `${kind}-id`, kind, territorialUnitId: null, name, primaryCode: code,
    territorialUnitTypeCode: 'MUNICIPALITY', territorialUnitType: type, country: 'Espanya', parent,
    locale: 'es-ES', isActive: kind !== 'Deactivate', isSelectableLocality: true,
    latitude: null, longitude: null, source: 'INE · Relació oficial',
    hierarchy: ['Espanya', 'Catalunya', 'Barcelona', 'Maresme', name],
    provenance: {
      organisation: 'INE', dataset: 'Relació oficial', datasetVersion: '2026', datasetDate: '2026-09-20',
      mappingVersion: 1, locale: 'es-ES', source: 'https://example.test/territori'
    },
    names: [{ name, locale: 'es-ES', kind: 'Official', isPrimary: true, source: 'INE · Relació oficial' }],
    codes: [{ scheme: 'es:ine:municipality', value: code, isPrimary: true, validFrom: null, validTo: null, source: 'INE · Relació oficial' }],
    differences, functionalReason: reason, hasManualConflict: false
  };
}

function detail(): ImportDetail {
  return {
    import: {
      id: 'import-1', countryId: 'country-1', countryName: 'Espanya', datasetSourceId: 'source-1',
      organisation: 'INE', dataset: 'Relació oficial', datasetVersion: '2026', mappingTemplateId: 'mapping-1',
      mappingVersion: 1, status: 'ReadyForReview', hasBlockingErrors: false, catalogVersion: 0,
      artifactName: 'spain.xlsx', fileSize: 100, fileChecksum: 'a'.repeat(64), actor: 'Admin',
      createdAtUtc: '2026-09-24T10:00:00Z', updatedAtUtc: '2026-09-24T10:01:00Z', publishedAtUtc: null,
      currentStage: 'ChangeSet', totalRows: 8201, processedRows: 8201, processingStartedAtUtc: null,
      processingCompletedAtUtc: null, lastHeartbeatAtUtc: null, attemptCount: 1, lastErrorCode: null,
      lastErrorMessage: null, isRecoverable: false, cancellationRequested: false,
      counters: { rows: 8201, errors: 0, warnings: 0, create: 8199, update: 0, deactivate: 0, noChange: 0 }
    },
    publicationMode: 'FullSnapshot', failureReason: null, schemaFingerprint: 'b'.repeat(64), canCancel: true,
    canPublish: false, canRevert: false, currentCatalogVersion: 0, changeSetId: 'set-1', changeSetStatus: 'Prepared',
    sourceSheets: ['Comunitats', 'Municipis'],
    manualConflictCount: 0,
    territorialBreakdown: [{ territorialUnitTypeCode: 'MUNICIPALITY', territorialUnitType: 'Municipi', create: 8199, update: 0, deactivate: 0, noChange: 0 }]
  };
}

function context(): TerritorialContext {
  return {
    countries: [{ id: 'country-1', code: 'ES', name: 'Espanya', iso2: 'ES', iso3: 'ESP', isActive: true }],
    sources: [{ id: 'source-1', countryId: 'country-1', organisation: 'INE', dataset: 'Relació oficial', datasetType: 'AdministrativeTerritory', locale: 'es-ES', approvalStatus: 'Pending', isActive: true, publicationMode: 'FullSnapshot', datasetVersion: '2026', datasetDate: null, license: null, attribution: null }],
    unitTypes: [{ id: 'type-1', countryId: 'country-1', code: 'MUNICIPALITY', name: 'Municipi', displayOrder: 1, isSelectableLocality: true }]
  };
}

function page<T>(items: T[]) { return { items, page: 1, pageSize: 50, totalCount: items.length, totalPages: items.length ? 1 : 0 }; }
