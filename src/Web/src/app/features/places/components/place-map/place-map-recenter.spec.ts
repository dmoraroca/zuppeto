import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { PlaceMapComponent } from './place-map.component';
import { CITY_INITIAL_ZOOM } from './place-map-zoom';

describe('PlaceMapComponent recenter', () => {
  it('returns to the selected municipality without starting a viewport search', async () => {
    await TestBed.configureTestingModule({ imports: [PlaceMapComponent] }).compileComponents();
    const fixture = TestBed.createComponent(PlaceMapComponent);
    fixture.componentRef.setInput('places', []);
    fixture.componentRef.setInput('municipality', {
      id: 'arenys', name: 'Arenys de Mar', lat: 41.5803, lng: 2.5508, placeCount: 0
    });
    fixture.componentRef.setInput('radiusKm', 0);
    const setView = vi.fn();
    const remove = vi.fn();
    const component = fixture.componentInstance as unknown as {
      map: { setView: typeof setView; remove: typeof remove };
      leaflet: object;
      focusAllPlaces(): void;
      viewportSearch: { subscribe(listener: () => void): void };
    };
    component.map = { setView, remove };
    component.leaflet = {};
    const viewportSearch = vi.fn();
    component.viewportSearch.subscribe(viewportSearch);

    component.focusAllPlaces();

    expect(setView).toHaveBeenCalledWith(
      [41.5803, 2.5508],
      CITY_INITIAL_ZOOM,
      { animate: false }
    );
    expect(viewportSearch).not.toHaveBeenCalled();
  });

  it('returns directly to the nearby municipality even when a radius is active', async () => {
    await TestBed.configureTestingModule({ imports: [PlaceMapComponent] }).compileComponents();
    const fixture = TestBed.createComponent(PlaceMapComponent);
    fixture.componentRef.setInput('places', []);
    fixture.componentRef.setInput('municipality', {
      id: 'sant-vicenc', name: 'Sant Vicenç de Montalt', lat: 41.578258, lng: 2.509518, placeCount: 0
    });
    fixture.componentRef.setInput('radiusKm', 10);
    const setView = vi.fn();
    const remove = vi.fn();
    const component = fixture.componentInstance as unknown as {
      map: { setView: typeof setView; remove: typeof remove };
      focusAllPlaces(): void;
    };
    component.map = { setView, remove };

    component.focusAllPlaces();

    expect(setView).toHaveBeenCalledWith(
      [41.578258, 2.509518],
      CITY_INITIAL_ZOOM,
      { animate: false }
    );
  });

  it('returns to Madrid when no municipality is selected', async () => {
    await TestBed.configureTestingModule({ imports: [PlaceMapComponent] }).compileComponents();
    const fixture = TestBed.createComponent(PlaceMapComponent);
    fixture.componentRef.setInput('places', []);
    const setView = vi.fn();
    const remove = vi.fn();
    const component = fixture.componentInstance as unknown as {
      map: { setView: typeof setView; remove: typeof remove };
      focusAllPlaces(): void;
    };
    component.map = { setView, remove };

    component.focusAllPlaces();

    expect(setView).toHaveBeenCalledWith([40.4168, -3.7038], CITY_INITIAL_ZOOM);
  });
});
