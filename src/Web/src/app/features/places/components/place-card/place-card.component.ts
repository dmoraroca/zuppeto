import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FavoriteToggleButtonComponent } from '../../../../shared/components/favorite-toggle-button/favorite-toggle-button.component';
import { Place } from '../../models/place.model';

@Component({
  selector: 'app-place-card',
  imports: [RouterLink, FavoriteToggleButtonComponent],
  templateUrl: './place-card.component.html',
  styleUrl: './place-card.component.scss'
})
export class PlaceCardComponent {
  readonly place = input.required<Place>();
  readonly typeLabel = input.required<string>();
  readonly favorite = input(false);
  readonly favoriteToggled = output<string>();

  protected onFavoriteToggle(): void {
    this.favoriteToggled.emit(this.place().id);
  }
}
