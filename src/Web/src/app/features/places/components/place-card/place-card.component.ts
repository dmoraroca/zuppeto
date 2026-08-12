import { Component, computed, effect, input, output, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FavoriteToggleButtonComponent } from '../../../../shared/components/favorite-toggle-button/favorite-toggle-button.component';
import { Place } from '../../models/place.model';

@Component({
  selector: 'app-place-card',
  imports: [RouterLink, FavoriteToggleButtonComponent],
  templateUrl: './place-card.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-card.component.scss'
})
export class PlaceCardComponent {
  readonly place = input.required<Place>();
  readonly typeLabel = input.required<string>();
  readonly favorite = input(false);
  readonly selected = input(false);
  readonly favoriteToggled = output<string>();

  private readonly coverLoadFailed = signal(false);

  constructor() {
    effect(() => {
      this.place();
      this.coverLoadFailed.set(false);
    });
  }

  /** Renders `<img>` only when URL is set and loading has not failed (404, network, etc.). */
  protected readonly showCoverImage = computed(() => {
    const url = this.place().imageUrl?.trim();
    return Boolean(url) && !this.coverLoadFailed();
  });

  protected onCoverImageError(): void {
    this.coverLoadFailed.set(true);
  }

  protected onFavoriteToggle(): void {
    this.favoriteToggled.emit(this.place().id);
  }
}
