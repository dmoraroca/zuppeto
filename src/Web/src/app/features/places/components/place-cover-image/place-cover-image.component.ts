import { Component, computed, effect, input, signal, ChangeDetectionStrategy } from '@angular/core';

export type PlaceCoverImageVariant = 'hero' | 'related';

@Component({
  selector: 'app-place-cover-image',
  templateUrl: './place-cover-image.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-cover-image.component.scss'
})
export class PlaceCoverImageComponent {
  readonly imageUrl = input('');
  readonly alt = input('');
  readonly caption = input('Imatge no disponible');
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

  protected onImageError(): void {
    this.loadFailed.set(true);
  }
}
