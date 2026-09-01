import {
  AfterViewInit,
  Component,
  ElementRef,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  input,
  output,
  ChangeDetectionStrategy
} from '@angular/core';

import { Place } from '../../models/place.model';
import { SPAIN_MAP_CENTER, SPAIN_MAP_ZOOM } from '../../utils/city-map-focus';

type LeafletModule = typeof import('leaflet');
type LeafletMap = import('leaflet').Map;
type LeafletLayerGroup = import('leaflet').LayerGroup;

@Component({
  selector: 'app-place-map',
  templateUrl: './place-map.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-map.component.scss'
})
export class PlaceMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('mapContainer') private readonly mapContainer?: ElementRef<HTMLDivElement>;

  readonly places = input.required<Place[]>();
  readonly selectedPlaceId = input<string | null>(null);
  readonly height = input('24rem');
  readonly emptyTitle = input('No hi ha ubicacions per mostrar');
  readonly emptyCopy = input('Ajusta els filtres per veure llocs al mapa.');
  readonly showToolbarActions = input(true);
  /** When there are no markers, centre the map here (selected city) instead of Europe. */
  readonly focusCenter = input<{ lat: number; lng: number } | null>(null);
  readonly placeSelected = output<string>();
  readonly selectionCleared = output<void>();

  private leaflet?: LeafletModule;
  private map?: LeafletMap;
  private markersLayer?: LeafletLayerGroup;
  private readonly markers = new Map<string, import('leaflet').CircleMarker>();
  private mapInitialization?: Promise<void>;
  private lastPlaceIdsKey = '';

  /** Spain-first listing view (do not fitBounds when a pin is in Australia, etc.). */
  private static readonly defaultMapCenter: [number, number] = SPAIN_MAP_CENTER;
  private static readonly defaultMapZoom = SPAIN_MAP_ZOOM;
  /** City-level zoom when a city is selected but there are no place pins. */
  private static readonly cityFocusZoom = 13;
  private static readonly selectedPinFill = '#22c55e';
  private static readonly selectedPinStroke = '#15803d';
  private static readonly idlePinFill = '#99f6e4';
  private static readonly idlePinStroke = '#0f766e';
  /** If visible pins span more than this, keep the camera on Spain instead of the world. */
  private static readonly maxFitSpanMeters = 1_200_000;

  async ngAfterViewInit(): Promise<void> {
    await this.ensureMap();
    this.renderMap();
  }

  ngOnChanges(_changes: SimpleChanges): void {
    this.renderMap();
  }

  ngOnDestroy(): void {
    this.map?.remove();
    this.map = undefined;
    this.markersLayer = undefined;
    this.mapInitialization = undefined;
  }

  protected get hasPlaces(): boolean {
    return this.places().length > 0;
  }

  protected get placesCountLabel(): string {
    return this.places().length === 1 ? '1 ubicació visible' : `${this.places().length} ubicacions visibles`;
  }

  protected get selectedPlace() {
    const selectedPlaceId = this.selectedPlaceId();

    return selectedPlaceId ? this.places().find((place) => place.id === selectedPlaceId) ?? null : null;
  }

  protected focusAllPlaces(): void {
    if (this.hasPlaces) {
      this.fitMapToPlaces();
    } else {
      this.setEmptyMapView();
    }
  }

  protected clearSelection(): void {
    this.selectionCleared.emit();
    if (this.hasPlaces) {
      this.fitMapToPlaces();
    } else {
      this.setEmptyMapView();
    }
  }

  private async ensureMap(): Promise<void> {
    const container = this.mapContainer?.nativeElement;
    if (this.map || !container) {
      return;
    }

    if (this.mapInitialization) {
      await this.mapInitialization;
      return;
    }

    this.mapInitialization = (async () => {
      this.leaflet = await import('leaflet');

      // Leaflet can keep container metadata across fast re-renders or HMR in dev mode.
      if ('_leaflet_id' in container) {
        delete (container as HTMLDivElement & { _leaflet_id?: number })._leaflet_id;
      }

      this.map = this.leaflet.map(container, {
        zoomControl: true,
        // Wheel zoom only while the pointer is over the map (Leaflet targets the container).
        scrollWheelZoom: true
      });

      this.leaflet
        .tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
          maxZoom: 19,
          attribution: '&copy; OpenStreetMap contributors'
        })
        .addTo(this.map);

      this.markersLayer = this.leaflet.layerGroup().addTo(this.map);
    })();

    await this.mapInitialization;
  }

  private renderMap(): void {
    void this.ensureMap().then(() => {
      if (!this.leaflet || !this.map || !this.markersLayer) {
        return;
      }

      if (!this.hasPlaces) {
        this.markersLayer.clearLayers();
        this.markers.clear();
        this.lastPlaceIdsKey = '';
        this.setEmptyMapView();
        return;
      }

      const placeIdsKey = this.places()
        .map((place) => place.id)
        .join(',');
      const placesChanged = placeIdsKey !== this.lastPlaceIdsKey;
      if (placesChanged) {
        this.redrawMarkers();
        this.lastPlaceIdsKey = placeIdsKey;
      }

      const selectedPlaceId = this.selectedPlaceId();
      this.applyMarkerSelection(selectedPlaceId);

      if (selectedPlaceId) {
        const selectedPlace = this.places().find((place) => place.id === selectedPlaceId);
        if (selectedPlace) {
          this.map.setView([selectedPlace.coordinates.lat, selectedPlace.coordinates.lng], 14);
          this.markers.get(selectedPlace.id)?.openPopup();
          return;
        }
      }

      if (placesChanged) {
        this.fitMapToPlaces();
      }
    });
  }

  private redrawMarkers(): void {
    if (!this.leaflet || !this.markersLayer) {
      return;
    }

    this.markersLayer.clearLayers();
    this.markers.clear();

    for (const place of this.places()) {
      const marker = this.leaflet.circleMarker([place.coordinates.lat, place.coordinates.lng], {
        radius: 8,
        weight: 2,
        color: PlaceMapComponent.idlePinStroke,
        fillColor: PlaceMapComponent.idlePinFill,
        fillOpacity: 0.85
      });

      marker.bindPopup(
        `
            <div class="place-map__popup">
              <strong>${place.name}</strong>
              <span>${place.city}</span>
            </div>
          `,
        {
          className: 'place-map__popup-shell'
        }
      );
      marker.on('click', () => this.placeSelected.emit(place.id));
      marker.addTo(this.markersLayer);
      this.markers.set(place.id, marker);
    }
  }

  private applyMarkerSelection(selectedPlaceId: string | null): void {
    for (const [placeId, marker] of this.markers) {
      const isSelected = placeId === selectedPlaceId;
      marker.setStyle({
        radius: isSelected ? 12 : 8,
        weight: isSelected ? 3 : 2,
        color: isSelected ? PlaceMapComponent.selectedPinStroke : PlaceMapComponent.idlePinStroke,
        fillColor: isSelected ? PlaceMapComponent.selectedPinFill : PlaceMapComponent.idlePinFill,
        fillOpacity: isSelected ? 1 : 0.85
      });
      marker.setRadius(isSelected ? 12 : 8);
      if (isSelected) {
        marker.bringToFront();
      }
    }
  }

  private setEmptyMapView(): void {
    if (!this.map) {
      return;
    }

    const focus = this.focusCenter();
    if (focus) {
      this.map.setView([focus.lat, focus.lng], PlaceMapComponent.cityFocusZoom);
      return;
    }

    this.map.setView(PlaceMapComponent.defaultMapCenter, PlaceMapComponent.defaultMapZoom);
  }

  private fitMapToPlaces(existingBounds?: import('leaflet').LatLngBounds): void {
    if (!this.leaflet || !this.map || !this.hasPlaces) {
      return;
    }

    const bounds =
      existingBounds ??
      this.places().reduce((accumulator, place) => {
        accumulator.extend([place.coordinates.lat, place.coordinates.lng]);

        return accumulator;
      }, this.leaflet.latLngBounds([]));

    const spanMeters = this.map.distance(bounds.getSouthWest(), bounds.getNorthEast());
    if (spanMeters > PlaceMapComponent.maxFitSpanMeters) {
      this.map.setView(PlaceMapComponent.defaultMapCenter, PlaceMapComponent.defaultMapZoom);
      return;
    }

    this.map.fitBounds(bounds, {
      padding: [28, 28],
      maxZoom: this.places().length === 1 ? 15 : 13
    });
  }
}
