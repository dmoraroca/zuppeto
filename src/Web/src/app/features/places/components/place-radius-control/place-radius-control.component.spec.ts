import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PlaceRadiusControlComponent } from './place-radius-control.component';

describe('PlaceRadiusControlComponent', () => {
  let fixture: ComponentFixture<PlaceRadiusControlComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PlaceRadiusControlComponent] }).compileComponents();
    fixture = TestBed.createComponent(PlaceRadiusControlComponent);
    fixture.componentRef.setInput('value', 10);
    fixture.detectChanges();
  });

  it('previews pointer movement and only confirms on change', () => {
    const emitted: number[] = [];
    fixture.componentInstance.valueConfirmed.subscribe(value => emitted.push(value));
    const slider = fixture.nativeElement.querySelector('input[type="range"]') as HTMLInputElement;
    slider.value = '3';
    slider.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.place-radius__value').textContent).toContain('30');
    expect(emitted).toEqual([]);

    slider.dispatchEvent(new Event('change'));
    expect(emitted).toEqual([30]);
  });

  it('keeps municipality separate from kilometre values and exposes an accessible slider', () => {
    const emitted: number[] = [];
    fixture.componentInstance.valueConfirmed.subscribe(value => emitted.push(value));
    (fixture.nativeElement.querySelector('.place-radius__municipality') as HTMLButtonElement).click();

    expect(emitted).toEqual([0]);
    const slider = fixture.nativeElement.querySelector('[aria-label="Radi de cerca en quilòmetres"]');
    expect(slider.getAttribute('aria-valuetext')).toBe('10 quilòmetres');
  });

  it('dims and blocks changes while nearby search is disabled', () => {
    const emitted: number[] = [];
    fixture.componentInstance.valueConfirmed.subscribe(value => emitted.push(value));
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const section = fixture.nativeElement.querySelector('.place-radius') as HTMLElement;
    const municipality = fixture.nativeElement.querySelector('.place-radius__municipality') as HTMLButtonElement;
    const slider = fixture.nativeElement.querySelector('input[type="range"]') as HTMLInputElement;
    expect(section.classList.contains('place-radius--disabled')).toBe(true);
    expect(municipality.disabled).toBe(true);
    expect(slider.disabled).toBe(true);

    municipality.click();
    slider.value = '3';
    slider.dispatchEvent(new Event('change'));
    expect(emitted).toEqual([]);
  });
});
