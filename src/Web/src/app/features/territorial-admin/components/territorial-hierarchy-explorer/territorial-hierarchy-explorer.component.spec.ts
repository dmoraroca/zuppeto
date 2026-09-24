import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { CatalogHierarchy, CatalogHierarchyNode, CatalogUnit } from '../../models/territorial-admin.model';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import { TerritorialHierarchyExplorerComponent } from './territorial-hierarchy-explorer.component';

describe('TerritorialHierarchyExplorerComponent', () => {
  it('shows an accessible initial skeleton and honours reduced motion', async () => {
    const originalMatchMedia = window.matchMedia;
    window.matchMedia = vi.fn().mockReturnValue({ matches: true }) as unknown as typeof window.matchMedia;
    let resolveHierarchy!: (value: CatalogHierarchy) => void;
    const hierarchyPromise = new Promise<CatalogHierarchy>((resolve) => { resolveHierarchy = resolve; });
    const { fixture } = await setup({
      catalogHierarchy: vi.fn().mockReturnValue(hierarchyPromise),
      catalogDescendants: vi.fn()
    });

    expect(fixture.nativeElement.querySelector('.tree-skeleton[aria-busy="true"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.explorer.reduced-motion')).not.toBeNull();

    resolveHierarchy(rootHierarchy());
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Catalunya');
    expect(fixture.nativeElement.textContent).toContain('Barcelona');
    window.matchMedia = originalMatchMedia;
  });

  it('loads children lazily, offers a local retry and reuses the cache after collapse', async () => {
    const api = {
      catalogHierarchy: vi.fn()
        .mockResolvedValueOnce(rootHierarchy())
        .mockRejectedValueOnce(new Error('fallada temporal'))
        .mockResolvedValueOnce(barcelonaHierarchy()),
      catalogDescendants: vi.fn()
    };
    const { fixture } = await setup(api);

    button(fixture, 'Expandir Barcelona').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('fallada temporal');

    button(fixture, 'Reintentar').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Arenys de Mar');
    expect(fixture.nativeElement.textContent).toContain('08006');
    expect(fixture.nativeElement.textContent).not.toContain('Seleccionable');
    expect(button(fixture, 'Veure detall de Arenys de Mar').textContent).toContain('Veure detall');
    expect(fixture.nativeElement.querySelector('[data-depth="3"] .toggle')).toBeNull();
    const depths = [...fixture.nativeElement.querySelectorAll('.tree-node[data-depth]')]
      .map((item) => item.getAttribute('data-depth'));
    expect(depths).toEqual(expect.arrayContaining(['0', '1', '2', '3']));
    expect(fixture.nativeElement.querySelector('[data-depth="1"]')?.textContent).toContain('Catalunya');
    expect(fixture.nativeElement.querySelector('[data-depth="2"]')?.textContent).toContain('Barcelona');
    expect(fixture.nativeElement.querySelector('[data-depth="3"]')?.textContent).toContain('Arenys de Mar');
    expect(fixture.nativeElement.querySelector('[data-depth="3"]')?.closest('.tree-group')
      ?.closest('.tree-item')?.querySelector(':scope > .tree-node')?.textContent).toContain('Barcelona');

    button(fixture, 'Contraure Barcelona').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Arenys de Mar');
    expect(button(fixture, 'Expandir Barcelona').getAttribute('aria-expanded')).toBe('false');
    button(fixture, 'Expandir Barcelona').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Arenys de Mar');
    expect(button(fixture, 'Contraure Barcelona').getAttribute('aria-expanded')).toBe('true');
    expect(api.catalogHierarchy).toHaveBeenCalledTimes(3);
  });

  it('opens a terminal node through the independent detail action', async () => {
    const api = {
      catalogHierarchy: vi.fn()
        .mockResolvedValueOnce(rootHierarchy())
        .mockResolvedValueOnce(barcelonaHierarchy()),
      catalogDescendants: vi.fn()
    };
    const { fixture } = await setup(api);
    const opened = vi.fn();
    fixture.componentInstance.openUnit.subscribe(opened);

    button(fixture, 'Expandir Barcelona').click();
    await fixture.whenStable();
    fixture.detectChanges();
    const detailAction = button(fixture, 'Veure detall de Arenys de Mar');
    expect(getComputedStyle(detailAction).cursor).toBe('pointer');
    detailAction.click();
    expect(opened).toHaveBeenCalledWith('arenys');
    expect(fixture.nativeElement.querySelector('[data-depth="3"] .toggle')).toBeNull();
  });

  it('renders a nested accessible tree with deterministic depth and no false leaf chevron', async () => {
    const { fixture } = await setup({
      catalogHierarchy: vi.fn().mockResolvedValue(rootHierarchy()),
      catalogDescendants: vi.fn()
    });

    const country = fixture.nativeElement.querySelector('.tree-node[data-depth="0"]') as HTMLElement;
    const current = fixture.nativeElement.querySelector('.tree-node[data-depth="1"]') as HTMLElement;
    const provinces = [...fixture.nativeElement.querySelectorAll('.tree-node[data-depth="2"]')] as HTMLElement[];
    expect(country.textContent).toContain('Espanya');
    expect(current.classList.contains('tree-node--current')).toBe(true);
    expect(fixture.nativeElement.textContent).not.toContain('Unitat actual');
    expect(fixture.nativeElement.textContent).not.toContain('Explorar jerarquia');
    expect(provinces.map((item) => item.textContent)).toEqual(expect.arrayContaining([
      expect.stringContaining('Barcelona'), expect.stringContaining('Girona'),
      expect.stringContaining('Lleida'), expect.stringContaining('Tarragona')
    ]));
    expect(current.querySelector('.toggle')?.getAttribute('aria-expanded')).toBe('true');
    expect(fixture.nativeElement.querySelectorAll('.tree-group').length).toBeGreaterThan(1);
  });

  it('completes an ancestor branch in the same tree without duplicating the current province', async () => {
    const api = {
      catalogHierarchy: vi.fn()
        .mockResolvedValueOnce(teruelHierarchy())
        .mockResolvedValueOnce(aragonHierarchy()),
      catalogDescendants: vi.fn()
    };
    const { fixture } = await setup(api, 'teruel');

    button(fixture, 'Contraure Aragón').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.tree-node[data-depth="2"]')).toBeNull();

    button(fixture, 'Expandir Aragón').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Huesca');
    expect(fixture.nativeElement.textContent).toContain('Zaragoza');
    expect([...fixture.nativeElement.querySelectorAll('.node-main strong')]
      .filter((item) => item.textContent === 'Teruel')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('.tree-node--current')?.textContent).toContain('Teruel');
    expect(fixture.nativeElement.textContent).not.toContain('Unitat actual');
    expect(api.catalogHierarchy).toHaveBeenCalledTimes(2);

    button(fixture, 'Contraure Aragón').click();
    fixture.detectChanges();
    button(fixture, 'Expandir Aragón').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.catalogHierarchy).toHaveBeenCalledTimes(2);
  });

  it('queries municipality descendants server-side with search and pagination and opens their detail', async () => {
    const pageOne = descendantPage([catalogUnit('arenys', 'Arenys de Mar', '08006')], 1, 2);
    const pageTwo = descendantPage([catalogUnit('arrecife', 'Arrecife', '35004')], 2, 2);
    const api = {
      catalogHierarchy: vi.fn().mockResolvedValue(rootHierarchy()),
      catalogDescendants: vi.fn()
        .mockResolvedValueOnce(pageOne)
        .mockResolvedValueOnce(pageOne)
        .mockResolvedValueOnce(pageTwo)
    };
    const { fixture } = await setup(api);
    const opened = vi.fn();
    fixture.componentInstance.openUnit.subscribe(opened);

    button(fixture, 'Tots els municipis 947').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Descendents de Catalunya, no només fills directes.');
    expect(fixture.nativeElement.textContent).toContain('Arenys de Mar');

    const input = fixture.nativeElement.querySelector('.descendants input') as HTMLInputElement;
    input.value = 'Arenys';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    button(fixture, 'Cercar').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.catalogDescendants).toHaveBeenLastCalledWith('catalunya', 'municipality', expect.objectContaining({ search: 'Arenys', page: 1 }));

    const descendantSection = fixture.nativeElement.querySelector('.descendants') as HTMLElement;
    button({ nativeElement: descendantSection }, 'Següent').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Arrecife');
    button(fixture, 'Veure detall').click();
    expect(opened).toHaveBeenCalledWith('arrecife');
  });
});

async function setup(api: { catalogHierarchy: ReturnType<typeof vi.fn>; catalogDescendants: ReturnType<typeof vi.fn> }, rootId = 'catalunya') {
  await TestBed.configureTestingModule({
    imports: [TerritorialHierarchyExplorerComponent],
    providers: [{ provide: TerritorialAdminApiService, useValue: api }]
  }).compileComponents();
  const fixture = TestBed.createComponent(TerritorialHierarchyExplorerComponent);
  fixture.componentRef.setInput('rootId', rootId);
  fixture.componentRef.setInput('countryName', 'Espanya');
  fixture.detectChanges();
  await Promise.resolve();
  fixture.detectChanges();
  return { fixture, api };
}

function teruelHierarchy(): CatalogHierarchy {
  return {
    current: node('teruel', 'Teruel', '44', 'PROVINCE', 'Província', 2, false),
    ancestors: [node('aragon', 'Aragón', '02', 'AUTONOMOUS_COMMUNITY', 'Comunitat autònoma', 3, false)],
    children: page([
      node('ababuj', 'Ababuj', '44001', 'MUNICIPALITY', 'Municipi', 0, true),
      node('abejuela', 'Abejuela', '44002', 'MUNICIPALITY', 'Municipi', 0, true)
    ]),
    descendantTypes: [{ territorialUnitTypeId: 'municipality', typeCode: 'MUNICIPALITY', type: 'Municipi', count: 2, isSelectableLocality: true }]
  };
}

function aragonHierarchy(): CatalogHierarchy {
  return {
    current: node('aragon', 'Aragón', '02', 'AUTONOMOUS_COMMUNITY', 'Comunitat autònoma', 3, false),
    ancestors: [],
    children: page([
      node('huesca', 'Huesca', '22', 'PROVINCE', 'Província', 202, false),
      node('teruel', 'Teruel', '44', 'PROVINCE', 'Província', 2, false),
      node('zaragoza', 'Zaragoza', '50', 'PROVINCE', 'Província', 293, false)
    ]),
    descendantTypes: [{ territorialUnitTypeId: 'municipality', typeCode: 'MUNICIPALITY', type: 'Municipi', count: 497, isSelectableLocality: true }]
  };
}

function rootHierarchy(): CatalogHierarchy {
  return {
    current: node('catalunya', 'Catalunya', '09', 'AUTONOMOUS_COMMUNITY', 'Comunitat autònoma', 4, false),
    ancestors: [],
    children: page([
      node('barcelona', 'Barcelona', '08', 'PROVINCE', 'Província', 311, false),
      node('girona', 'Girona', '17', 'PROVINCE', 'Província', 221, false),
      node('lleida', 'Lleida', '25', 'PROVINCE', 'Província', 231, false),
      node('tarragona', 'Tarragona', '43', 'PROVINCE', 'Província', 184, false)
    ]),
    descendantTypes: [{ territorialUnitTypeId: 'municipality', typeCode: 'MUNICIPALITY', type: 'Municipi', count: 947, isSelectableLocality: true }]
  };
}

function barcelonaHierarchy(): CatalogHierarchy {
  return {
    current: node('barcelona', 'Barcelona', '08', 'PROVINCE', 'Província', 311, false),
    ancestors: [node('catalunya', 'Catalunya', '09', 'AUTONOMOUS_COMMUNITY', 'Comunitat autònoma', 4, false)],
    children: page([node('arenys', 'Arenys de Mar', '08006', 'MUNICIPALITY', 'Municipi', 0, true)]),
    descendantTypes: [{ territorialUnitTypeId: 'municipality', typeCode: 'MUNICIPALITY', type: 'Municipi', count: 311, isSelectableLocality: true }]
  };
}

function node(id: string, name: string, code: string, typeCode: string, type: string, directChildCount: number, selectable: boolean): CatalogHierarchyNode {
  return {
    id, countryId: 'es', parentId: id === 'catalunya' ? null : 'catalunya', territorialUnitTypeId: typeCode.toLowerCase(),
    typeCode, type, name, primaryCode: code, isActive: true, isSelectableLocality: selectable, directChildCount
  };
}

function catalogUnit(id: string, primaryName: string, primaryCode: string): CatalogUnit {
  return {
    id, countryId: 'es', country: 'Espanya', territorialUnitTypeId: 'municipality', typeCode: 'MUNICIPALITY', type: 'Municipi',
    parentId: 'barcelona', parent: 'Barcelona', primaryCode, primaryName, locale: 'es-ES', latitude: null, longitude: null,
    isActive: true, isSelectableLocality: true, hasManualOverride: false, hasManualActiveOverride: false,
    hasManualSelectableOverride: false, hasManualCoordinateOverride: false
  };
}

function page<T>(items: T[]) { return { items, page: 1, pageSize: 50, totalCount: items.length, totalPages: items.length ? 1 : 0 }; }
function descendantPage(items: CatalogUnit[], currentPage: number, totalPages: number) {
  return { items, page: currentPage, pageSize: 1, totalCount: totalPages, totalPages };
}
function button(fixture: { nativeElement: HTMLElement }, label: string): HTMLButtonElement {
  const result = [...fixture.nativeElement.querySelectorAll('button')]
    .find((item) => item.textContent?.trim() === label || item.getAttribute('aria-label') === label) as HTMLButtonElement | undefined;
  if (!result) throw new Error(`No s'ha trobat el botó ${label}`);
  return result;
}
