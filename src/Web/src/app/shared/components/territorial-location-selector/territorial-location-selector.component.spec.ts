import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { TerritorialLocationService } from '../../services/territorial-location.service';
import { TerritorialLocationSelectorComponent } from './territorial-location-selector.component';

describe('TerritorialLocationSelectorComponent', () => {
  it('disables locality without country and clears it when country changes', async () => {
    const api = {
      countries: vi.fn().mockResolvedValue([{ id: 'es', code: 'ES', name: 'Espanya' }]),
      localities: vi.fn().mockResolvedValue([{ id: 'bcn', countryId: 'es', name: 'Barcelona', context: 'Catalunya · Espanya', locale: 'ca-ES' }])
    };
    await TestBed.configureTestingModule({
      imports: [TerritorialLocationSelectorComponent],
      providers: [{ provide: TerritorialLocationService, useValue: api }]
    }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialLocationSelectorComponent);
    fixture.componentRef.setInput('localityId', 'old');
    const localityChanges: string[] = [];
    fixture.componentInstance.localityIdChange.subscribe((value) => localityChanges.push(value));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const selects = fixture.nativeElement.querySelectorAll('select') as NodeListOf<HTMLSelectElement>;
    expect(selects[1].disabled).toBe(true);
    selects[0].value = 'es';
    selects[0].dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(localityChanges).toContain('');
  });

  it('shows Unicode homonyms with hierarchy context returned by backend', async () => {
    const api = {
      countries: vi.fn().mockResolvedValue([{ id: 'de', code: 'DE', name: 'Deutschland' }]),
      localities: vi.fn().mockResolvedValue([
        { id: 'a', countryId: 'de', name: 'München', context: 'Bayern · Deutschland', locale: 'de-DE' },
        { id: 'b', countryId: 'de', name: 'München', context: 'Kreis X · Deutschland', locale: 'de-DE' }
      ])
    };
    await TestBed.configureTestingModule({
      imports: [TerritorialLocationSelectorComponent],
      providers: [{ provide: TerritorialLocationService, useValue: api }]
    }).compileComponents();
    const fixture = TestBed.createComponent(TerritorialLocationSelectorComponent);
    fixture.componentRef.setInput('countryId', 'de');
    fixture.detectChanges();
    await fixture.whenStable();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('München · Bayern · Deutschland');
    expect(fixture.nativeElement.textContent).toContain('München · Kreis X · Deutschland');
  });
});
