import { DatePipe } from '@angular/common';
import { Component, computed, input, output, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FavoriteToggleButtonComponent } from '../../../../shared/components/favorite-toggle-button/favorite-toggle-button.component';
import { PlaceCoverImageComponent } from '../place-cover-image/place-cover-image.component';
import { Place } from '../../models/place.model';
import { hasPublicPetPolicy, hasPublicPrice, hasPublicRating } from '../../utils/place-detail-copy';
import { resolvePlaceCardVisualState } from '../../utils/place-card-visual-state';

@Component({
  selector: 'app-place-card',
  imports: [RouterLink, DatePipe, FavoriteToggleButtonComponent, PlaceCoverImageComponent],
  templateUrl: './place-card.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-card.component.scss'
})
export class PlaceCardComponent {
  readonly place = input.required<Place>();
  readonly typeLabel = input.required<string>();
  readonly favorite = input(false);
  readonly showFavoriteAction = input(true);
  readonly selected = input(false);
  readonly showCityExploreLink = input(false);
  readonly detailQueryParams = input<Record<string, string | number | null>>({});
  readonly favoriteToggled = output<string>();
  readonly placeClicked = output<string>();

  protected readonly hasRating = computed(() => hasPublicRating(this.place()));
  protected readonly hasPrice = computed(() => hasPublicPrice(this.place()));
  protected readonly hasPetPolicy = computed(() => hasPublicPetPolicy(this.place()));
  protected readonly visualState = computed(() => resolvePlaceCardVisualState(this.place()));
  protected readonly petStatus = computed(() => {
    switch (this.place().petAccessStatus) {
      case 'Allowed': return { icon: '✓', label: 'Accepta mascotes', tone: 'positive' };
      case 'TerraceOnly': return { icon: '◐', label: 'Només terrassa', tone: 'conditional' };
      case 'NotAllowed': return { icon: '×', label: 'No admet mascotes', tone: 'negative' };
      case 'PendingReview': return { icon: '?', label: 'Pendent de revisió', tone: 'review' };
      default: return { icon: '—', label: 'Informació desconeguda', tone: 'unknown' };
    }
  });

  protected onFavoriteToggle(): void {
    this.favoriteToggled.emit(this.place().id);
  }

  protected onCardSelect(event: Event): void {
    const target = event.target as HTMLElement | null;
    if (target?.closest('a, button')) {
      return;
    }

    this.placeClicked.emit(this.place().id);
  }

  protected onCardKeyboardSelect(event: Event): void {
    event.preventDefault();
    this.placeClicked.emit(this.place().id);
  }
}
