import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { FavoriteToggleButtonComponent } from '../../../../shared/components/favorite-toggle-button/favorite-toggle-button.component';
import { FavoritesService } from '../../../favorites/services/favorites.service';
import { PlaceService } from '../../services/place.service';

@Component({
  selector: 'app-place-detail-page',
  imports: [RouterLink, SiteHeaderComponent, SiteFooterComponent, FavoriteToggleButtonComponent],
  templateUrl: './place-detail-page.component.html',
  styleUrl: './place-detail-page.component.scss'
})
export class PlaceDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly placeService = inject(PlaceService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly params = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap
  });

  protected readonly place = computed(() =>
    this.placeService.getPlaceById(this.params().get('id') ?? '')
  );

  protected toggleFavorite(placeId: string): void {
    this.favoritesService.toggle(placeId);
  }

  protected isFavorite(placeId: string): boolean {
    return this.favoritesService.isFavorite(placeId);
  }

  protected getTypeLabel(type: string): string {
    return this.placeService.getTypeLabel(type as never);
  }
}
