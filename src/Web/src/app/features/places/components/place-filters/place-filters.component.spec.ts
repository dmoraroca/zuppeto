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

  it('only exposes actionable public pet policies and removes the public verified control', () => {
    fixture.componentRef.setInput('filters', {
      ...fixture.componentInstance.filters(),
      petAccessStatuses: ['NotAllowed', 'Unknown', 'PendingReview']
    });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Accepta mascotes');
    expect(text).toContain('Només terrassa');
    expect(text).toContain('Amb condicions');
    expect(text).not.toContain('No admet');
    expect(text).not.toContain('Desconegut');
    expect(text).not.toContain('Pendent de revisió');
    expect(text).not.toContain('Només informació verificada');
    expect(text).not.toContain('Totes');
    expect(text).not.toContain('Altres');
    expect(fixture.nativeElement.querySelector('[role="switch"]')).toBeNull();
  });

  it('uses collapsible groups and round selection controls', () => {
    expect(fixture.nativeElement.querySelectorAll('details.place-filters__group').length).toBe(3);
    expect(fixture.nativeElement.querySelectorAll('.place-filters__control-dot').length).toBeGreaterThan(0);
  });

  it('emits independently selectable species controls', () => {
    const emitted: unknown[] = [];
    fixture.componentInstance.filtersChanged.subscribe((value) => emitted.push(value));

    const species = [...fixture.nativeElement.querySelectorAll('.place-filters__species label')] as HTMLLabelElement[];
    (species.find((label) => label.textContent?.includes('Gossos'))!.querySelector('input') as HTMLInputElement).click();
    fixture.componentRef.setInput('filters', {
      ...fixture.componentInstance.filters(), pet: 'dogs', petSpecies: ['dogs']
    });
    fixture.detectChanges();
    (species.find((label) => label.textContent?.includes('Gats'))!.querySelector('input') as HTMLInputElement).click();

    expect(emitted).toContainEqual({ petSpecies: ['dogs'], pet: 'dogs' });
    expect(emitted).toContainEqual({ petSpecies: ['dogs', 'cats'], pet: 'all' });
  });

  it('shows principal categories first and keeps selected categories visible when collapsed', () => {
    const types = Array.from({ length: 9 }, (_, index) => ({
      value: `category-${index}`,
      label: `Categoria ${index + 1}`
    }));
    fixture.componentRef.setInput('types', types);
    fixture.componentRef.setInput('filters', {
      ...fixture.componentInstance.filters(),
      types: ['category-8']
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.place-filters__categories input').length).toBe(8);
    expect(fixture.nativeElement.textContent).toContain('Categoria 9');

    (fixture.nativeElement.querySelector('.place-filters__show-more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.place-filters__categories input').length).toBe(9);

    (fixture.nativeElement.querySelector('.place-filters__show-more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Categoria 9');
  });
});
