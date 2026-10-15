import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TerritorialLocationService } from '../../../../shared/services/territorial-location.service';
import { PlaceFiltersComponent } from './place-filters.component';

describe('PlaceFiltersComponent Phase VI', () => {
  let fixture: ComponentFixture<PlaceFiltersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlaceFiltersComponent],
      providers: [{
        provide: TerritorialLocationService,
        useValue: { countries: async () => [], localities: async () => [] }
      }]
    }).compileComponents();
    fixture = TestBed.createComponent(PlaceFiltersComponent);
    fixture.componentRef.setInput('filters', {
      search: '', country: '', city: '', type: '', pet: 'all', petSpecies: [], types: [],
      petAccessStatuses: [], verifiedOnly: false, searchSort: 'name', radiusKm: 0
    });
    fixture.componentRef.setInput('cities', []);
    fixture.componentRef.setInput('types', [
      { value: 'restaurant', label: 'Restaurants' },
      { value: 'bar', label: 'Bars' }
    ]);
    fixture.detectChanges();
  });

  it('exposes accessible multiselect categories and emits one selection', () => {
    const emitted: unknown[] = [];
    fixture.componentInstance.filtersChanged.subscribe((value) => emitted.push(value));
    const checkbox = fixture.nativeElement.querySelector('.place-filters__categories input') as HTMLInputElement;
    checkbox.click();

    expect(emitted).toContainEqual({ types: ['restaurant'], type: '' });
    expect(fixture.nativeElement.textContent).toContain('0 seleccionades');
  });

  it('shows all radius values and keeps Unknown separate from NotAllowed', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Només municipi');
    expect(text).toContain('50 km');
    expect(text).toContain('No admet');
    expect(text).toContain('Desconegut');
    expect(text).toContain('Només informació verificada');
    expect(text).not.toContain('Totes');
    expect(text).not.toContain('Altres');
    expect(fixture.nativeElement.querySelector('[role="switch"]')).not.toBeNull();
  });

  it('emits the compact verified switch and independently selectable species chips', () => {
    const emitted: unknown[] = [];
    fixture.componentInstance.filtersChanged.subscribe((value) => emitted.push(value));

    (fixture.nativeElement.querySelector('[role="switch"]') as HTMLInputElement).click();
    const species = [...fixture.nativeElement.querySelectorAll('.place-filters__species label')] as HTMLLabelElement[];
    (species.find((label) => label.textContent?.includes('Gossos'))!.querySelector('input') as HTMLInputElement).click();
    fixture.componentRef.setInput('filters', {
      ...fixture.componentInstance.filters(), pet: 'dogs', petSpecies: ['dogs']
    });
    fixture.detectChanges();
    (species.find((label) => label.textContent?.includes('Gats'))!.querySelector('input') as HTMLInputElement).click();

    expect(emitted).toContainEqual({ verifiedOnly: true });
    expect(emitted).toContainEqual({ petSpecies: ['dogs'], pet: 'dogs' });
    expect(emitted).toContainEqual({ petSpecies: ['dogs', 'cats'], pet: 'all' });
  });
});
