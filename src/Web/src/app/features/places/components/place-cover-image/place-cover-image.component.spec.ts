import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PlaceCoverImageComponent } from './place-cover-image.component';

describe('PlaceCoverImageComponent', () => {
  let fixture: ComponentFixture<PlaceCoverImageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PlaceCoverImageComponent] }).compileComponents();
    fixture = TestBed.createComponent(PlaceCoverImageComponent);
    fixture.componentRef.setInput('placeName', 'Can Pep');
  });

  it('shows a valid image without the fallback', () => {
    fixture.componentRef.setInput('imageUrl', '/images/place.jpg');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('img')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('img')?.getAttribute('loading')).toBe('lazy');
    expect(fixture.nativeElement.querySelector('img')?.getAttribute('alt')).toBe('Fotografia de Can Pep');
    expect(fixture.nativeElement.querySelector('.place-cover__placeholder')).toBeNull();
  });

  it.each([null, '', '   '])('shows the fallback for an unavailable URL (%s)', (imageUrl) => {
    fixture.componentRef.setInput('imageUrl', imageUrl);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.querySelector('.place-cover__placeholder-icon')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.place-cover__caption')?.textContent.trim()).toBe('Il·lustració PetiLoc');
  });

  it('removes a failed img and activates the same fallback', () => {
    fixture.componentRef.setInput('imageUrl', '/images/missing.jpg');
    fixture.detectChanges();
    const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;

    image.dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.querySelector('.place-cover__placeholder')).not.toBeNull();
  });

  it('uses a category illustration without implying a real photograph', () => {
    fixture.componentRef.setInput('placeName', 'DMR School');
    fixture.componentRef.setInput('imageUrl', '');
    fixture.componentRef.setInput('category', 'restaurant');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.place-cover__placeholder-icon')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.place-cover__category')?.textContent.trim()).toBe('Restaurant');
    expect(fixture.nativeElement.querySelector('.place-cover__caption')?.textContent.trim()).toBe('Il·lustració PetiLoc');
    expect(fixture.nativeElement.querySelector('.place-cover__placeholder')?.getAttribute('aria-label'))
      .toContain('Il·lustració PetiLoc de categoria Restaurant');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
  });
});
