import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { TerritorialLocationService } from '../../services/territorial-location.service';
import { TerritorialLocationSelectorComponent, TerritorialLocationSelection } from './territorial-location-selector.component';

describe('TerritorialLocationSelectorComponent', () => {
  it('disables locality without country and resets selection and results when country changes', async () => {
    const api = locationApi();
    const fixture = await create(api);
    const input = combobox(fixture);
    expect(input.disabled).toBe(true);
    expect(input.placeholder).toBe('Selecciona primer un país');
    expect(fixture.nativeElement.querySelectorAll('select')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(0);

    const localityChanges: string[] = [];
    fixture.componentInstance.localityIdChange.subscribe((value) => localityChanges.push(value));
    await changeCountry(fixture, 'es');
    expect(combobox(fixture).disabled).toBe(false);
    expect(combobox(fixture).placeholder).toBe('Cerca una localitat');
    expect(localityChanges).toContain('');

    type(fixture, 'Vila');
    await flushSearch(fixture);
    clickOption(fixture, 'Vila E2E À');
    await changeCountry(fixture, 'de');
    expect(combobox(fixture).value).toBe('');
    expect(fixture.nativeElement.querySelector('[role="listbox"]')).toBeNull();
  });

  it('debounces search, renders Unicode homonyms with context and selects from the same control', async () => {
    const api = locationApi([
      locality('a', 'de', 'München', 'Bayern · Deutschland'),
      locality('b', 'de', 'München', 'Kreis München · Deutschland')
    ]);
    const fixture = await create(api, 'de');
    const selections: TerritorialLocationSelection[] = [];
    fixture.componentInstance.selectionChange.subscribe((value) => selections.push(value));
    type(fixture, 'M');
    type(fixture, 'Mü');
    type(fixture, 'München');
    await delay(250);
    expect(api.localities).not.toHaveBeenCalled();
    await flushSearch(fixture, 70);

    expect(api.localities).toHaveBeenCalledTimes(1);
    expect(api.localities).toHaveBeenCalledWith('de', 'München');
    expect(fixture.nativeElement.textContent).toContain('München');
    expect(fixture.nativeElement.textContent).toContain('Bayern · Deutschland');
    expect(fixture.nativeElement.textContent).toContain('Kreis München · Deutschland');
    clickOption(fixture, 'Bayern · Deutschland');
    expect(combobox(fixture).value).toBe('München');
    expect(selections.at(-1)).toEqual({ countryId: 'de', country: 'Deutschland', territorialUnitId: 'a', locality: 'München' });
  });

  it('ignores an old country search after the country changes', async () => {
    let resolveOld!: (value: ReturnType<typeof locality>[]) => void;
    const oldResult = new Promise<ReturnType<typeof locality>[]>((resolve) => { resolveOld = resolve; });
    const api = locationApi();
    api.localities.mockImplementation((countryId: string) => countryId === 'es'
      ? oldResult
      : Promise.resolve([locality('berlin', 'de', 'Berlin', 'Berlin · Deutschland')]));
    const fixture = await create(api, 'es');
    type(fixture, 'Vila');
    await flushSearch(fixture);
    await changeCountry(fixture, 'de');
    type(fixture, 'Berlin');
    await flushSearch(fixture);
    expect(fixture.nativeElement.textContent).toContain('Berlin · Deutschland');

    resolveOld([locality('old', 'es', 'Vila antiga', 'Catalunya · Espanya')]);
    await Promise.resolve();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Vila antiga');
    expect(fixture.nativeElement.textContent).toContain('Berlin · Deutschland');
  });

  it('shows loading, empty and error states inside the combobox', async () => {
    const api = locationApi([]);
    const fixture = await create(api, 'es');
    type(fixture, 'Inexistent');
    expect(fixture.nativeElement.textContent).toContain('Cercant localitats…');
    await flushSearch(fixture);
    expect(fixture.nativeElement.textContent).toContain('No s’han trobat localitats.');

    api.localities.mockRejectedValueOnce(new Error('network'));
    type(fixture, 'Error');
    await flushSearch(fixture);
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('No s’han pogut consultar les localitats.');
  });

  it('supports Arrow keys, Enter selection and Escape closing', async () => {
    const api = locationApi([
      locality('a', 'es', 'Àger', 'Noguera · Catalunya · Espanya'),
      locality('b', 'es', 'Vila E2E À', 'Regió E2E À · País E2E À')
    ]);
    const fixture = await create(api, 'es');
    const selected: string[] = [];
    fixture.componentInstance.localityIdChange.subscribe((value) => selected.push(value));
    type(fixture, '');
    await flushSearch(fixture);

    key(fixture, 'ArrowDown');
    key(fixture, 'Enter');
    await fixture.whenStable();
    fixture.detectChanges();
    expect(selected.at(-1)).toBe('b');
    expect(combobox(fixture).value).toBe('Vila E2E À');

    combobox(fixture).dispatchEvent(new FocusEvent('focus'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="listbox"]')).not.toBeNull();
    key(fixture, 'Escape');
    expect(fixture.nativeElement.querySelector('[role="listbox"]')).toBeNull();
  });
});

function locationApi(items = [locality('vila', 'es', 'Vila E2E À', 'Regió E2E À · Espanya')]) {
  return {
    countries: vi.fn().mockResolvedValue([
      { id: 'es', code: 'ES', name: 'Espanya' },
      { id: 'de', code: 'DE', name: 'Deutschland' }
    ]),
    localities: vi.fn().mockResolvedValue(items)
  };
}

function locality(id: string, countryId: string, name: string, context: string) {
  return { id, countryId, name, context, locale: countryId === 'de' ? 'de-DE' : 'ca-ES' };
}

async function create(api: ReturnType<typeof locationApi>, countryId = ''): Promise<ComponentFixture<TerritorialLocationSelectorComponent>> {
  await TestBed.configureTestingModule({
    imports: [TerritorialLocationSelectorComponent],
    providers: [{ provide: TerritorialLocationService, useValue: api }]
  }).compileComponents();
  const fixture = TestBed.createComponent(TerritorialLocationSelectorComponent);
  fixture.componentRef.setInput('countryId', countryId);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  api.localities.mockClear();
  return fixture;
}

function combobox(fixture: ComponentFixture<TerritorialLocationSelectorComponent>): HTMLInputElement {
  return fixture.nativeElement.querySelector('[role="combobox"]') as HTMLInputElement;
}

async function changeCountry(fixture: ComponentFixture<TerritorialLocationSelectorComponent>, value: string): Promise<void> {
  const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
  select.value = value;
  select.dispatchEvent(new Event('change', { bubbles: true }));
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
}

function type(fixture: ComponentFixture<TerritorialLocationSelectorComponent>, value: string): void {
  const input = combobox(fixture);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

async function flushSearch(fixture: ComponentFixture<TerritorialLocationSelectorComponent>, milliseconds = 300): Promise<void> {
  await delay(milliseconds);
  await Promise.resolve();
  fixture.detectChanges();
}

function delay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

function clickOption(fixture: ComponentFixture<TerritorialLocationSelectorComponent>, text: string): void {
  const option = [...fixture.nativeElement.querySelectorAll('[role="option"]')]
    .find((item) => item.textContent?.includes(text)) as HTMLButtonElement;
  option.click();
  fixture.detectChanges();
}

function key(fixture: ComponentFixture<TerritorialLocationSelectorComponent>, value: string): void {
  combobox(fixture).dispatchEvent(new KeyboardEvent('keydown', { key: value, bubbles: true }));
  fixture.detectChanges();
}
