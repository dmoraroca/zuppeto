import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import { TerritorialCatalogComponent } from './territorial-catalog.component';

describe('TerritorialCatalogComponent', () => {
  it('loads the table, opens and closes the modal and preserves filters', async () => {
    const { fixture, api } = await setup();
    expect(api.catalog).toHaveBeenCalled();

    const country = fixture.nativeElement.querySelector('select[name="country"]') as HTMLSelectElement;
    country.value = 'country';
    country.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="dialog"]')).not.toBeNull();

    const close = [...fixture.nativeElement.querySelectorAll('button')]
      .find((item: HTMLButtonElement) => item.textContent?.trim() === 'Tancar') as HTMLButtonElement;
    close.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeNull();
    expect(country.value).toBe('country');
  });

  it('forwards contextual maintenance and refreshes the current page', async () => {
    const { fixture, api } = await setup();
    (fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    button(fixture, 'Desactivar').click();
    fixture.detectChanges();
    const textarea = fixture.nativeElement.querySelector('[role="alertdialog"] textarea') as HTMLTextAreaElement;
    const confirm = button(fixture, 'Desactivar', true);
    expect(confirm.disabled).toBe(true);
    textarea.value = 'Motiu funcional';
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(confirm.disabled).toBe(false);
    confirm.click();
    await fixture.whenStable();

    expect(api.maintain).toHaveBeenCalledWith('unit', {
      action: 'deactivate', reason: 'Motiu funcional', isSelectableLocality: undefined,
      latitude: undefined, longitude: undefined
    });
    expect(api.catalog).toHaveBeenCalledTimes(2);
  });

  it('builds locale options from the complete context instead of the current paginated page', async () => {
    const { fixture } = await setup({
      unit: { countryId: 'de', country: 'Alemanya', locale: 'de-DE' },
      context: {
        countries: [
          { id: 'es', code: 'spain', name: 'Espanya', iso2: 'ES', iso3: 'ESP', isActive: true },
          { id: 'de', code: 'germany', name: 'Alemanya', iso2: 'DE', iso3: 'DEU', isActive: true }
        ],
        sources: [
          { id: 'ine', countryId: 'es', sourceKey: 'ES-INE', organisation: 'INE', dataset: 'Municipis', datasetType: 'AdministrativeTerritory', locale: 'es-ES', approvalStatus: 'Approved', isActive: true, publicationMode: 'FullSnapshot', datasetVersion: '2026', datasetDate: null, license: null, attribution: null },
          { id: 'gvisys', countryId: 'de', sourceKey: 'DE-GVISYS', organisation: 'Destatis', dataset: 'GV-ISys', datasetType: 'AdministrativeTerritory', locale: 'de-DE', approvalStatus: 'Approved', isActive: true, publicationMode: 'FullSnapshot', datasetVersion: '2026', datasetDate: null, license: null, attribution: null }
        ],
        unitTypes: [],
        functionalTypes: [
          { code: 'MUNICIPALITY', name: 'Municipi', displayOrder: 10, countryIds: ['es', 'de'] },
          { code: 'PLANNING_REGION', name: 'Regió de planificació', displayOrder: 60, countryIds: ['de'] }
        ]
      }
    });
    const country = fixture.nativeElement.querySelector('select[name="country"]') as HTMLSelectElement;
    const locale = fixture.nativeElement.querySelector('select[name="locale"]') as HTMLSelectElement;
    expect([...locale.options].map((option) => option.value)).toEqual(['', 'de-DE', 'es-ES']);

    country.value = 'es';
    country.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect([...locale.options].map((option) => option.value)).toEqual(['', 'es-ES']);

    locale.value = 'es-ES';
    locale.dispatchEvent(new Event('change'));
    country.value = 'de';
    country.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(locale.value).toBe('');
    expect([...locale.options].map((option) => option.value)).toEqual(['', 'de-DE']);
  });

  it('uses common functional categories and preserves only filters compatible with the selected country', async () => {
    const { fixture, api } = await setup({
      context: {
        countries: [
          { id: 'es', code: 'ES', name: 'Espanya', iso2: 'ES', iso3: 'ESP', isActive: true },
          { id: 'de', code: 'DE', name: 'Alemanya', iso2: 'DE', iso3: 'DEU', isActive: true }
        ],
        sources: [], unitTypes: [],
        functionalTypes: [
          { code: 'MUNICIPALITY', name: 'Municipi', displayOrder: 10, countryIds: ['es', 'de'] },
          { code: 'PLANNING_REGION', name: 'Regió de planificació', displayOrder: 60, countryIds: ['de'] }
        ]
      }
    });
    const country = fixture.nativeElement.querySelector('select[name="country"]') as HTMLSelectElement;
    const type = fixture.nativeElement.querySelector('select[name="type"]') as HTMLSelectElement;
    expect([...type.options].map((option) => option.text)).toEqual(['Totes', 'Municipi', 'Regió de planificació']);

    type.value = 'MUNICIPALITY';
    type.dispatchEvent(new Event('change'));
    country.value = 'es';
    country.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(type.value).toBe('MUNICIPALITY');
    await (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    expect(api.catalog).toHaveBeenLastCalledWith(expect.objectContaining({ countryId: 'es', functionalTypeCode: 'MUNICIPALITY' }));

    country.value = 'de';
    country.dispatchEvent(new Event('change'));
    type.value = 'PLANNING_REGION';
    type.dispatchEvent(new Event('change'));
    country.value = 'es';
    country.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(type.value).toBe('');
  });

  it('preserves the explorer context while opening and closing a nested unit detail', async () => {
    const { fixture, api } = await setup();
    (fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
    button(fixture, 'Jerarquia').click();
    await fixture.whenStable();
    fixture.detectChanges();

    const explorer = fixture.nativeElement.querySelector('app-territorial-hierarchy-explorer') as HTMLElement;
    const childDetail = explorer.querySelector('[aria-label="Veure detall de Fill territorial"]') as HTMLButtonElement;
    expect(explorer.textContent).not.toContain('Seleccionable');
    childDetail.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.catalogDetail).toHaveBeenLastCalledWith('child');
    expect(fixture.nativeElement.textContent).toContain('Detall fill territorial');

    buttons(fixture, 'Jerarquia').at(-1)!.click();
    await fixture.whenStable();
    fixture.detectChanges();
    const nestedExplorer = [...fixture.nativeElement.querySelectorAll('app-territorial-hierarchy-explorer')].at(-1) as HTMLElement;
    expect(nestedExplorer.querySelector('.tree-node--current')?.textContent).toContain('Fill territorial');
    expect(nestedExplorer.textContent).toContain('Vila E2E À');

    const visibleClose = [...fixture.nativeElement.querySelectorAll('.close-button')].at(-1) as HTMLButtonElement;
    visibleClose.click();
    fixture.detectChanges();
    expect(explorer.isConnected).toBe(true);
    expect(explorer.closest('[hidden]')).toBeNull();
    expect(explorer.textContent).toContain('Explorador territorial');
  });
});

async function setup(options?: {
  unit?: Partial<{
    id: string; countryId: string; country: string; territorialUnitTypeId: string; typeCode: string; type: string;
    parentId: string | null; parent: string | null; primaryCode: string; primaryName: string; locale: string | null;
    latitude: number | null; longitude: number | null; isActive: boolean; isSelectableLocality: boolean;
    hasManualOverride: boolean; hasManualActiveOverride: boolean; hasManualSelectableOverride: boolean;
    hasManualCoordinateOverride: boolean;
  }>;
  context?: import('../../models/territorial-admin.model').TerritorialContext;
}) {
  const unit = {
    id: 'unit', countryId: 'country', country: 'País E2E À', territorialUnitTypeId: 'type',
    typeCode: 'LOCALITY', type: 'Localitat E2E', parentId: 'region', parent: 'Regió E2E À', primaryCode: 'E2E-001',
    primaryName: 'Vila E2E À', locale: 'ca-ES', latitude: 40.4168, longitude: -3.7038,
    isActive: true, isSelectableLocality: true, hasManualOverride: false,
    hasManualActiveOverride: false, hasManualSelectableOverride: false, hasManualCoordinateOverride: false,
    ...options?.unit
  };
  const detail = {
    unit, ancestors: [{ id: 'region', name: 'Regió E2E À', type: 'Regió E2E' }], names: [], codes: [],
    coordinateSource: null, datasetSources: [], importIds: [], audit: []
  };
  const api = {
    catalog: vi.fn().mockResolvedValue({ items: [unit], page: 1, pageSize: 50, totalCount: 1, totalPages: 1 }),
    catalogDetail: vi.fn().mockImplementation((id: string) => Promise.resolve(id === 'child'
      ? { ...detail, unit: { ...unit, id: 'child', primaryName: 'Detall fill territorial', primaryCode: 'E2E-002' } }
      : detail)),
    catalogHierarchy: vi.fn().mockImplementation((id: string) => Promise.resolve(id === 'child' ? {
      current: {
        id: 'child', countryId: 'country', parentId: 'unit', territorialUnitTypeId: 'type', typeCode: 'LOCALITY',
        type: 'Localitat E2E', name: 'Fill territorial', primaryCode: 'E2E-002', isActive: true,
        isSelectableLocality: true, directChildCount: 0
      },
      ancestors: [{
        id: 'unit', countryId: 'country', parentId: 'region', territorialUnitTypeId: 'type', typeCode: 'LOCALITY',
        type: 'Localitat E2E', name: 'Vila E2E À', primaryCode: 'E2E-001', isActive: true,
        isSelectableLocality: true, directChildCount: 1
      }],
      children: { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 },
      descendantTypes: []
    } : {
      current: {
        id: 'unit', countryId: 'country', parentId: 'region', territorialUnitTypeId: 'type', typeCode: 'LOCALITY',
        type: 'Localitat E2E', name: 'Vila E2E À', primaryCode: 'E2E-001', isActive: true,
        isSelectableLocality: true, directChildCount: 1
      },
      ancestors: [],
      children: {
        items: [{
          id: 'child', countryId: 'country', parentId: 'unit', territorialUnitTypeId: 'type', typeCode: 'LOCALITY',
          type: 'Localitat E2E', name: 'Fill territorial', primaryCode: 'E2E-002', isActive: true,
          isSelectableLocality: true, directChildCount: 0
        }],
        page: 1, pageSize: 50, totalCount: 1, totalPages: 1
      },
      descendantTypes: []
    })),
    catalogDescendants: vi.fn(),
    maintain: vi.fn().mockResolvedValue({ ...detail, unit: { ...unit, isActive: false, hasManualOverride: true } })
  };
  await TestBed.configureTestingModule({
    imports: [TerritorialCatalogComponent],
    providers: [{ provide: TerritorialAdminApiService, useValue: api }]
  }).compileComponents();
  const fixture = TestBed.createComponent(TerritorialCatalogComponent);
  fixture.componentRef.setInput('context', options?.context ?? {
    countries: [{ id: 'country', code: 'E2EA', name: 'País E2E À', iso2: null, iso3: null, isActive: true }],
    sources: [], unitTypes: [{ id: 'type', countryId: 'country', code: 'LOCALITY', name: 'Localitat E2E', displayOrder: 1, isSelectableLocality: true }],
    functionalTypes: [{ code: 'LOCALITY', name: 'Localitat', displayOrder: 1, countryIds: ['country'] }]
  });
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, api };
}

function button(fixture: { nativeElement: HTMLElement }, label: string, last = false): HTMLButtonElement {
  const matches = [...fixture.nativeElement.querySelectorAll('button')]
    .filter((item) => item.textContent?.trim() === label) as HTMLButtonElement[];
  return last ? matches.at(-1)! : matches[0];
}

function buttons(fixture: { nativeElement: HTMLElement }, label: string): HTMLButtonElement[] {
  return [...fixture.nativeElement.querySelectorAll('button')]
    .filter((item) => item.textContent?.trim() === label) as HTMLButtonElement[];
}
