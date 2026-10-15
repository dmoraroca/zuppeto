import { Component, computed, effect, input, signal, ChangeDetectionStrategy } from '@angular/core';
import { PlaceType } from '../../models/place.model';

export type PlaceCoverImageVariant = 'hero' | 'related' | 'card';

@Component({
  selector: 'app-place-cover-image',
  templateUrl: './place-cover-image.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-cover-image.component.scss'
})
export class PlaceCoverImageComponent {
  readonly imageUrl = input<string | null | undefined>('');
  readonly placeName = input('');
  readonly alt = input('');
  readonly category = input<PlaceType | string>('service');
  readonly variant = input<PlaceCoverImageVariant>('hero');

  private readonly loadFailed = signal(false);

  constructor() {
    effect(() => {
      this.imageUrl();
      this.loadFailed.set(false);
    });
  }

  /** Show `<img>` only when a URL is set and loading has not failed. */
  protected readonly showImage = computed(() => {
    const url = this.imageUrl()?.trim();
    return Boolean(url) && !this.loadFailed();
  });
  protected readonly resolvedAlt = computed(() =>
    this.alt().trim() || (this.placeName().trim() ? `Fotografia de ${this.placeName().trim()}` : 'Fotografia del lloc')
  );
  protected readonly placeholder = computed(() => {
    switch (this.category().trim().toLocaleLowerCase('ca')) {
      case 'restaurant': return { icon: '🍽', label: 'Restaurant', tone: 'restaurant' };
      case 'bar': return { icon: '◒', label: 'Bar', tone: 'bar' };
      case 'cafe':
      case 'cafeteria': return { icon: '☕', label: 'Cafeteria', tone: 'cafe' };
      case 'hotel': return { icon: '⌂', label: 'Hotel', tone: 'hotel' };
      case 'apartment': return { icon: '⌂', label: 'Allotjament', tone: 'hotel' };
      case 'camping':
      case 'camp_site': return { icon: '△', label: 'Càmping', tone: 'camping' };
      case 'park': return { icon: '♧', label: 'Espai exterior', tone: 'park' };
      case 'veterinary':
      case 'veterinari': return { icon: '✚', label: 'Veterinari', tone: 'veterinary' };
      case 'shop':
      case 'pet_shop':
      case 'comerç': return { icon: '◇', label: 'Comerç', tone: 'shop' };
      default: return { icon: '✦', label: 'Servei', tone: 'service' };
    }
  });

  protected onImageError(): void {
    this.loadFailed.set(true);
  }
}
