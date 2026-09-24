import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import { TerritorialCatalogDetailComponent } from './territorial-catalog-detail.component';

describe('TerritorialCatalogDetailComponent', () => {
  it('provides tabs, readable audit and contextual action validation', async () => {
    await TestBed.configureTestingModule({ imports: [TerritorialCatalogDetailComponent] }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialCatalogDetailComponent);
    fixture.componentRef.setInput('detail', detail());
    fixture.detectChanges();

    click(fixture, 'Auditoria');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Desactivació');
    expect(fixture.nativeElement.textContent).toContain('Actiu');
    expect(fixture.nativeElement.textContent).toContain('Inactiu');

    click(fixture, 'General');
    fixture.detectChanges();
    click(fixture, 'Desactivar');
    fixture.detectChanges();
    const confirm = buttons(fixture, 'Desactivar').at(-1)!;
    const textarea = fixture.nativeElement.querySelector('[role="alertdialog"] textarea') as HTMLTextAreaElement;
    expect(confirm.disabled).toBe(true);
    textarea.value = 'ab';
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(confirm.disabled).toBe(true);
    textarea.value = 'abc';
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(confirm.disabled).toBe(false);
  });

  it('emits independent selectable and coordinate operations and exposes functional errors', async () => {
    await TestBed.configureTestingModule({ imports: [TerritorialCatalogDetailComponent] }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialCatalogDetailComponent);
    fixture.componentRef.setInput('detail', detail());
    fixture.componentRef.setInput('error', 'Error funcional del backend');
    const emitted = vi.fn();
    fixture.componentInstance.maintenance.subscribe(emitted);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Error funcional del backend');

    click(fixture, 'Fer no seleccionable');
    fixture.detectChanges();
    fillReason(fixture, 'Canvi justificat');
    buttons(fixture, 'Fer no seleccionable').at(-1)!.click();
    expect(emitted).toHaveBeenLastCalledWith(expect.objectContaining({ action: 'set-selectable', isSelectableLocality: false, reason: 'Canvi justificat' }));

    click(fixture, 'Coordenades');
    fixture.detectChanges();
    click(fixture, 'Corregir coordenades');
    fixture.detectChanges();
    const inputs = fixture.nativeElement.querySelectorAll('[role="alertdialog"] input');
    inputs[0].value = '0'; inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = '0'; inputs[1].dispatchEvent(new Event('input'));
    fillReason(fixture, 'Coordenades justificades');
    expect(buttons(fixture, 'Corregir coordenades').at(-1)!.disabled).toBe(true);
    inputs[0].value = '41.2'; inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = '2.1'; inputs[1].dispatchEvent(new Event('input'));
    fixture.detectChanges();
    buttons(fixture, 'Corregir coordenades').at(-1)!.click();
    expect(emitted).toHaveBeenLastCalledWith(expect.objectContaining({ action: 'set-coordinates', latitude: 41.2, longitude: 2.1 }));
  });

  it('reactivates an inactive unit only with a valid independent reason', async () => {
    await TestBed.configureTestingModule({ imports: [TerritorialCatalogDetailComponent] }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialCatalogDetailComponent);
    const inactive = detail();
    inactive.unit.isActive = false;
    fixture.componentRef.setInput('detail', inactive);
    const emitted = vi.fn();
    fixture.componentInstance.maintenance.subscribe(emitted);
    fixture.detectChanges();

    click(fixture, 'Reactivar');
    fixture.detectChanges();
    const confirm = buttons(fixture, 'Reactivar').at(-1)!;
    expect(confirm.disabled).toBe(true);
    fillReason(fixture, 'Reactivació validada');
    expect(confirm.disabled).toBe(false);
    confirm.click();
    expect(emitted).toHaveBeenCalledWith({
      action: 'activate', reason: 'Reactivació validada',
      isSelectableLocality: undefined, latitude: undefined, longitude: undefined
    });
  });

  it('uses Cancel·lar as the only contextual exit without executing maintenance', async () => {
    await TestBed.configureTestingModule({ imports: [TerritorialCatalogDetailComponent] }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialCatalogDetailComponent);
    fixture.componentRef.setInput('detail', detail());
    const emitted = vi.fn();
    fixture.componentInstance.maintenance.subscribe(emitted);
    fixture.detectChanges();

    click(fixture, 'Desactivar');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alertdialog"]')).not.toBeNull();
    expect(buttons(fixture, 'Tancar')).toHaveLength(1);

    click(fixture, 'Cancel·lar');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alertdialog"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[role="dialog"]')).not.toBeNull();
    expect(emitted).not.toHaveBeenCalled();
  });

  it('shows the self-explorable tree directly without redundant current-unit text or CTA', async () => {
    const api = {
      catalogHierarchy: vi.fn().mockResolvedValue({
        current: {
          id: 'unit', countryId: 'country', parentId: 'region', territorialUnitTypeId: 'type', typeCode: 'LOCALITY',
          type: 'Localitat E2E', name: 'Vila E2E À', primaryCode: 'E2E-001', isActive: true,
          isSelectableLocality: true, directChildCount: 0
        },
        ancestors: [{
          id: 'region', countryId: 'country', parentId: null, territorialUnitTypeId: 'region-type', typeCode: 'REGION',
          type: 'Regió E2E', name: 'Regió E2E À', primaryCode: 'R-E2E', isActive: true,
          isSelectableLocality: false, directChildCount: 1
        }],
        children: { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 },
        descendantTypes: []
      }),
      catalogDescendants: vi.fn()
    };
    await TestBed.configureTestingModule({
      imports: [TerritorialCatalogDetailComponent],
      providers: [{ provide: TerritorialAdminApiService, useValue: api }]
    }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialCatalogDetailComponent);
    fixture.componentRef.setInput('detail', detail());
    const opened = vi.fn();
    fixture.componentInstance.openUnit.subscribe(opened);
    fixture.detectChanges();

    click(fixture, 'Jerarquia');
    await fixture.whenStable();
    fixture.detectChanges();
    const explorer = fixture.nativeElement.querySelector('app-territorial-hierarchy-explorer') as HTMLElement;
    const nodes = [...explorer.querySelectorAll('.tree-node[data-depth]')] as HTMLElement[];
    expect(nodes.map((node) => node.dataset['depth'])).toEqual(['0', '1', '2']);
    expect(explorer.textContent).toContain('País E2E À');
    expect(explorer.textContent).not.toContain('Unitat actual');
    expect(explorer.textContent).not.toContain('Explorar jerarquia');
    expect(explorer.querySelector('.tree-node--current')?.textContent).toContain('Vila E2E À');
    (nodes[1].querySelector('.node-main') as HTMLButtonElement).click();
    expect(opened).toHaveBeenCalledWith('region');
  });
});

function detail() {
  return {
    unit: {
      id: 'unit', countryId: 'country', country: 'País E2E À', territorialUnitTypeId: 'type', typeCode: 'LOCALITY',
      type: 'Localitat E2E', parentId: 'region', parent: 'Regió E2E À', primaryCode: 'E2E-001', primaryName: 'Vila E2E À',
      locale: 'ca-ES', latitude: 40.4168, longitude: -3.7038, isActive: true, isSelectableLocality: true, hasManualOverride: true,
      hasManualActiveOverride: true, hasManualSelectableOverride: false, hasManualCoordinateOverride: false
    },
    ancestors: [{ id: 'region', name: 'Regió E2E À', type: 'Regió E2E' }],
    names: [{ id: 'name', name: 'Vila E2E À', locale: 'ca-ES', kind: 'Official', isPrimary: true, source: null }],
    codes: [{ id: 'code', scheme: 'e2e:code', value: 'E2E-001', validFrom: null, validTo: null, isPrimary: true, source: null }],
    coordinateSource: null, datasetSources: [], importIds: [],
    audit: [{ id: 'audit', action: 'deactivate', field: 'isActive', beforeValue: 'true', afterValue: 'false', reason: 'Prova', actor: 'Admin', origin: 'Manual', createdAtUtc: '2026-09-23T15:00:00Z' }]
  };
}

function buttons(fixture: { nativeElement: HTMLElement }, label: string): HTMLButtonElement[] {
  return [...fixture.nativeElement.querySelectorAll('button')].filter((item) => item.textContent?.trim() === label) as HTMLButtonElement[];
}

function click(fixture: { nativeElement: HTMLElement }, label: string): void { buttons(fixture, label)[0].click(); }

function fillReason(fixture: { nativeElement: HTMLElement; detectChanges(): void }, value: string): void {
  const textarea = fixture.nativeElement.querySelector('[role="alertdialog"] textarea') as HTMLTextAreaElement;
  textarea.value = value;
  textarea.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}
