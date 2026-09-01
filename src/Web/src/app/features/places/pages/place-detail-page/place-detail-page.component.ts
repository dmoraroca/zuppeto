import { Component, computed, effect, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { FavoriteToggleButtonComponent } from '../../../../shared/components/favorite-toggle-button/favorite-toggle-button.component';
import { FavoritesService } from '../../../favorites/services/favorites.service';
import { PlaceCoverImageComponent } from '../../components/place-cover-image/place-cover-image.component';
import { PlaceMapComponent } from '../../components/place-map/place-map.component';
import { PlaceService } from '../../services/place.service';
import {
  hasPublicPetPolicy,
  hasPublicPrice,
  hasPublicRating
} from '../../utils/place-detail-copy';

@Component({
  selector: 'app-place-detail-page',
  imports: [
    RouterLink,
    SiteHeaderComponent,
    SiteFooterComponent,
    FavoriteToggleButtonComponent,
    PlaceCoverImageComponent,
    PlaceMapComponent
  ],
  templateUrl: './place-detail-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-detail-page.component.scss'
})
export class PlaceDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly placeService = inject(PlaceService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly params = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap
  });
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap
  });

  constructor() {
    effect(() => {
      const id = this.params().get('id') ?? '';
      if (id) {
        void this.placeService.loadById(id);
      }
    });
  }

  protected readonly place = computed(() =>
    this.placeService.getPlaceById(this.params().get('id') ?? '')
  );
  protected readonly hasLoaded = computed(() => this.placeService.hasLoaded());
  protected readonly relatedPlaces = computed(() => {
    const currentPlace = this.place();

    if (!currentPlace) {
      return [];
    }

    return this.placeService
      .getPlaces({ city: currentPlace.city })
      .filter((place) => place.id !== currentPlace.id)
      .slice(0, 3);
  });
  protected readonly backToPlacesQueryParams = computed(() => {
    const currentPlace = this.place();

    return currentPlace ? { city: currentPlace.city } : {};
  });
  protected readonly cameFromMap = computed(() => this.queryParams().get('fromMap') === 'true');
  protected readonly hasPetPolicy = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? hasPublicPetPolicy(currentPlace) : false;
  });
  protected readonly hasRating = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? hasPublicRating(currentPlace) : false;
  });
  protected readonly hasPrice = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? hasPublicPrice(currentPlace) : false;
  });
  protected readonly showsGoogleMapsAttribution = computed(() => {
    const provenance = this.place()?.dataProvenance?.trim() ?? '';
    return provenance === 'GooglePlaces' || provenance === 'Mixed';
  });

  protected toggleFavorite(placeId: string): void {
    this.favoritesService.toggle(placeId);
  }

  protected isFavorite(placeId: string): boolean {
    return this.favoritesService.isFavorite(placeId);
  }

  protected getTypeLabel(type: string): string {
    return this.placeService.resolveTypeLabel(type);
  }

  protected get placeAsArray() {
    const currentPlace = this.place();

    // Omit map when there are no plottable coordinates (redacted cache and no valid Google cache).
    if (
      !currentPlace ||
      (currentPlace.excludeFromOsmMap && !currentPlace.requiresGoogleMapForGoogleCoordinates)
    ) {
      return [];
    }

    return [currentPlace];
  }
}
