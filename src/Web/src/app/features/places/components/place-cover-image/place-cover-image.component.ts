import { Component, computed, effect, input, signal, ChangeDetectionStrategy } from '@angular/core';
import { placeInitials } from '../../utils/place-initials';

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
  readonly caption = input('NO DISPONIBLE');
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

  protected readonly initials = computed(() => placeInitials(this.placeName() || this.alt()));

  protected onImageError(): void {
    this.loadFailed.set(true);
  }
}
