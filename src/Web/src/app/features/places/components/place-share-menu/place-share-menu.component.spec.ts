import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PlaceShareMenuComponent } from './place-share-menu.component';

describe('PlaceShareMenuComponent', () => {
  let fixture: ComponentFixture<PlaceShareMenuComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PlaceShareMenuComponent] }).compileComponents();
    fixture = TestBed.createComponent(PlaceShareMenuComponent);
    fixture.componentRef.setInput('target', { id: 'place-1', name: 'AB Boutique Hotel', city: 'Sitges' });
    fixture.detectChanges();
  });

  it('shows all permanent web options with accessible labels', () => {
    const dialog = fixture.nativeElement.querySelector('[role="dialog"]') as HTMLElement;
    const labels = [...dialog.querySelectorAll('a strong, button strong')].map(element => element.textContent?.trim());

    expect(dialog.getAttribute('aria-labelledby')).toBe('place-share-title');
    expect(labels).toContain('WhatsApp');
    expect(labels).toContain('Telegram');
    expect(labels).toContain('Correu electrònic');
    expect(labels).toContain('Copiar l’enllaç');
    expect(dialog.querySelector('[aria-label="Tancar el menú de compartir"]')).not.toBeNull();
  });

  it('emits close when Escape is pressed', () => {
    const closed = vi.fn();
    fixture.componentInstance.closed.subscribe(closed);
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(closed).toHaveBeenCalledOnce();
  });
});
