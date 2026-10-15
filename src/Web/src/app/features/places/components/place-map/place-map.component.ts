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
import { signal } from '@angular/core';

import { Place } from '../../models/place.model';
import { PlaceSearchMapArea } from '../../services/place.service';
import { SPAIN_MAP_CENTER, SPAIN_MAP_ZOOM } from '../../utils/city-map-focus';
import {
  CITY_CLICK_ZOOM,
  CITY_INITIAL_ZOOM,
  PLACE_MARKER_MIN_ZOOM,
  placeMapLayerMode
} from './place-map-zoom';
import { PlaceMapLayerMode } from './place-map-zoom';

type LeafletModule = typeof import('leaflet');
type LeafletMap = import('leaflet').Map;
type LeafletLayerGroup = import('leaflet').LayerGroup;
export interface PlaceMapViewport { north: number; south: number; east: number; west: number; }
export interface PlaceMapMunicipality {
  name: string;
  lat: number;
  lng: number;
  placeCount: number;
}

@Component({
  selector: 'app-place-map',
  templateUrl: './place-map.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-map.component.scss'
})
export class PlaceMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('mapContainer') private readonly mapContainer?: ElementRef<HTMLDivElement>;

  readonly places = input.required<Place[]>();
  readonly areas = input<PlaceSearchMapArea[]>([]);
  readonly selectedPlaceId = input<string | null>(null);
  readonly height = input('24rem');
  readonly emptyTitle = input('No hi ha ubicacions per mostrar');
  readonly emptyCopy = input('Ajusta els filtres per veure llocs al mapa.');
  readonly showToolbarActions = input(true);
  /** When there are no markers, centre the map here (selected city) instead of Europe. */
  readonly focusCenter = input<{ lat: number; lng: number } | null>(null);
  readonly municipality = input<PlaceMapMunicipality | null>(null);
  readonly radiusKm = input(0);
  readonly placeSelected = output<string>();
  readonly selectionCleared = output<void>();
  readonly viewportSearch = output<PlaceMapViewport>();

  private leaflet?: LeafletModule;
  private map?: LeafletMap;
  private markersLayer?: LeafletLayerGroup;
  private readonly markers = new Map<string, import('leaflet').Marker>();
  private mapInitialization?: Promise<void>;
  private lastDataKey = '';
  private lastCameraContextKey = '';
  protected readonly viewportChanged = signal(false);
  protected readonly visibleLayer = signal<PlaceMapLayerMode>('municipality');
  protected readonly currentZoom = signal(CITY_INITIAL_ZOOM);
  protected readonly currentCenter = signal<{ lat: number; lng: number } | null>(null);
  private automaticCameraUntil = 0;

  /** Spain-first listing view (do not fitBounds when a pin is in Australia, etc.). */
  private static readonly defaultMapCenter: [number, number] = SPAIN_MAP_CENTER;
  private static readonly defaultMapZoom = SPAIN_MAP_ZOOM;
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
    if (this.municipality()) {
      this.setMunicipalityView();
    } else if (this.hasPlaces) {
      this.fitMapToPlaces();
    } else {
      this.setEmptyMapView();
    }
  }

  protected clearSelection(): void {
    this.selectionCleared.emit();
    if (this.municipality()) {
      this.setMunicipalityView();
    } else if (this.hasPlaces) {
      this.fitMapToPlaces();
    } else {
      this.setEmptyMapView();
    }
  }

  protected searchCurrentViewport(): void {
    if (!this.map) return;
    const bounds = this.map.getBounds();
    this.viewportChanged.set(false);
    this.viewportSearch.emit({
      north: bounds.getNorth(),
      south: bounds.getSouth(),
      east: bounds.getEast(),
      west: bounds.getWest()
    });
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
        // Leaflet keeps a delayed zoom-transition callback alive after remove().
        // Route changes can otherwise make that callback access an already removed map pane.
        zoomAnimation: false,
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
      this.map.on('zoomend', () => {
        this.redrawMarkers();
        this.markViewportChanged();
      });
      this.map.on('moveend', () => this.markViewportChanged());
    })();

    await this.mapInitialization;
  }

  private renderMap(): void {
    void this.ensureMap().then(() => {
      if (!this.leaflet || !this.map || !this.markersLayer) {
        return;
      }

      const municipality = this.municipality();
      const dataKey = [
        ...this.places().map((place) => place.id),
        ...(municipality ? [municipality.name, municipality.placeCount] : []),
        ...this.areas().map((area) => `${area.territorialUnitId}:${area.placeCount}`)
      ].join('|');
      const dataChanged = dataKey !== this.lastDataKey;
      const cameraContextKey = municipality
        ? `${municipality.lat}:${municipality.lng}:${this.radiusKm()}`
        : '';
      const cameraContextChanged = cameraContextKey !== this.lastCameraContextKey;

      if (cameraContextChanged) {
        municipality ? this.setMunicipalityView() : this.setEmptyMapView();
        this.lastCameraContextKey = cameraContextKey;
      } else if (!municipality && dataChanged && this.hasPlaces) {
        this.fitMapToPlaces();
      } else if (!this.hasPlaces && !municipality) {
        this.setEmptyMapView();
      }

      if (dataChanged || cameraContextChanged) {
        this.redrawMarkers();
        this.lastDataKey = dataKey;
      }

      const selectedPlaceId = this.selectedPlaceId();
      this.applyMarkerSelection(selectedPlaceId);

      if (selectedPlaceId) {
        const selectedPlace = this.places().find((place) => place.id === selectedPlaceId);
        if (selectedPlace) {
          this.markAutomaticCamera();
          this.map.setView([selectedPlace.coordinates!.lat, selectedPlace.coordinates!.lng], 14);
          this.markers.get(selectedPlace.id)?.openPopup();
          return;
        }
      }

    });
  }

  private redrawMarkers(): void {
    if (!this.leaflet || !this.markersLayer) {
      return;
    }

    this.markersLayer.clearLayers();
    this.markers.clear();

    const zoom = this.map?.getZoom() ?? CITY_INITIAL_ZOOM;
    const mode = placeMapLayerMode(zoom, this.hasPlaces);
    this.currentZoom.set(zoom);
    this.visibleLayer.set(mode);
    const center = this.map?.getCenter();
    this.currentCenter.set(center ? { lat: center.lat, lng: center.lng } : null);
    if (mode === 'municipality') {
      this.drawMunicipalities();
      return;
    }
    if (mode === 'clusters') {
      this.drawClusters(zoom);
      return;
    }
    this.drawPlacesWithCollisionClusters(zoom);
  }

  private applyMarkerSelection(selectedPlaceId: string | null): void {
    for (const [placeId, marker] of this.markers) {
      const isSelected = placeId === selectedPlaceId;
      const place = this.places().find((item) => item.id === placeId);
      if (place) marker.setIcon(this.placeIcon(place, isSelected));
      if (isSelected) {
        marker.setZIndexOffset(1000);
      } else {
        marker.setZIndexOffset(0);
      }
    }
  }

  private setEmptyMapView(): void {
    if (!this.map) {
      return;
    }

    const focus = this.focusCenter();
    if (focus) {
      this.markAutomaticCamera();
      this.map.setView([focus.lat, focus.lng], CITY_INITIAL_ZOOM);
      return;
    }

    this.markAutomaticCamera();
    this.map.setView(PlaceMapComponent.defaultMapCenter, PlaceMapComponent.defaultMapZoom);
  }

  private setMunicipalityView(): void {
    if (!this.leaflet || !this.map) return;
    const municipality = this.municipality();
    if (!municipality) return;

    this.markAutomaticCamera();
    if (this.radiusKm() > 0) {
      const bounds = this.leaflet.circle(
        [municipality.lat, municipality.lng],
        { radius: this.radiusKm() * 1000 }
      ).getBounds();
      this.map.fitBounds(bounds, { padding: [28, 28], maxZoom: PLACE_MARKER_MIN_ZOOM - 1 });
      return;
    }

    this.map.setView([municipality.lat, municipality.lng], CITY_INITIAL_ZOOM);
  }

  private fitMapToPlaces(existingBounds?: import('leaflet').LatLngBounds): void {
    if (!this.leaflet || !this.map || !this.hasPlaces) {
      return;
    }

    const bounds =
      existingBounds ??
      this.places().reduce((accumulator, place) => {
        accumulator.extend([place.coordinates!.lat, place.coordinates!.lng]);

        return accumulator;
      }, this.leaflet.latLngBounds([]));

    const spanMeters = this.map.distance(bounds.getSouthWest(), bounds.getNorthEast());
    if (spanMeters > PlaceMapComponent.maxFitSpanMeters) {
      this.markAutomaticCamera();
      this.map.setView(PlaceMapComponent.defaultMapCenter, PlaceMapComponent.defaultMapZoom);
      return;
    }

    this.markAutomaticCamera();
    this.map.fitBounds(bounds, {
      padding: [28, 28],
      maxZoom: this.places().length === 1 ? 15 : 13
    });
  }

  private drawMunicipalities(): void {
    if (!this.leaflet || !this.markersLayer) return;
    const municipality = this.municipality();
    if (municipality) {
      this.drawMunicipalityMarker(municipality);
      return;
    }

    const suppliedAreas = this.areas().filter(area => area.latitude != null && area.longitude != null);
    if (suppliedAreas.length > 0) {
      for (const area of suppliedAreas) {
        this.drawMunicipalityMarker({
          name: area.name,
          lat: area.latitude!,
          lng: area.longitude!,
          placeCount: area.placeCount
        });
      }
      return;
    }
    const groups = new Map<string, Place[]>();
    for (const place of this.places()) groups.set(place.city, [...(groups.get(place.city) ?? []), place]);
    for (const [city, places] of groups) {
      const lat = places.reduce((sum, place) => sum + place.coordinates!.lat, 0) / places.length;
      const lng = places.reduce((sum, place) => sum + place.coordinates!.lng, 0) / places.length;
      this.drawMunicipalityMarker({ name: city, lat, lng, placeCount: places.length });
    }
  }

  private drawMunicipalityMarker(municipality: PlaceMapMunicipality): void {
    if (!this.leaflet || !this.markersLayer) return;
    const countLabel = municipality.placeCount === 1 ? '1 local' : `${municipality.placeCount} locals`;
    const marker = this.leaflet.circleMarker([municipality.lat, municipality.lng], {
      radius: Math.min(22, 10 + Math.sqrt(municipality.placeCount) * 2),
      weight: 3,
      color: '#0f766e',
      fillColor: '#f0fdfa',
      fillOpacity: 0.96,
      className: 'place-map__municipality-marker'
    });
    marker.bindTooltip(
      `<strong>${escapeHtml(municipality.name)}</strong><span>${countLabel}</span>`,
      { permanent: true, direction: 'top', className: 'place-map__municipality-label' }
    );
    marker.on('click', () => this.zoomToMunicipality(municipality));
    marker.on('add', () => this.makeCircleInteractive(
      marker,
      `Apropar-se a ${municipality.name}, ${countLabel}`,
      () => this.zoomToMunicipality(municipality)
    ));
    marker.addTo(this.markersLayer);
  }

  private zoomToMunicipality(municipality: PlaceMapMunicipality): void {
    if (!this.map) return;
    this.markAutomaticCamera();
    this.map.setView([municipality.lat, municipality.lng], CITY_CLICK_ZOOM);
  }

  private drawClusters(_zoom: number): void {
    for (const places of this.groupPlacesByPixels(72)) this.drawCluster(places, true);
  }

  private drawPlacesWithCollisionClusters(zoom: number): void {
    const collisionPixels = zoom >= 15 ? 24 : 36;
    for (const places of this.groupPlacesByPixels(collisionPixels)) {
      if (places.length > 1) this.drawCluster(places, false);
      else this.drawPlaceMarker(places[0]);
    }
  }

  private groupPlacesByPixels(cellSize: number): Place[][] {
    if (!this.map) return [];
    const groups = new Map<string, Place[]>();
    for (const place of this.places()) {
      const point = this.map.latLngToLayerPoint([place.coordinates!.lat, place.coordinates!.lng]);
      const key = `${Math.floor(point.x / cellSize)}:${Math.floor(point.y / cellSize)}`;
      groups.set(key, [...(groups.get(key) ?? []), place]);
    }
    return [...groups.values()];
  }

  private drawCluster(places: Place[], permanentLabel: boolean): void {
    if (!this.leaflet || !this.map || !this.markersLayer || places.length === 0) return;
    const lat = places.reduce((sum, place) => sum + place.coordinates!.lat, 0) / places.length;
    const lng = places.reduce((sum, place) => sum + place.coordinates!.lng, 0) / places.length;
    const countLabel = places.length === 1 ? '1 local' : `${places.length} locals`;
    const marker = this.leaflet.circleMarker([lat, lng], {
      radius: Math.min(20, 8 + Math.sqrt(places.length) * 2),
      color: '#92400e',
      fillColor: '#fbbf24',
      fillOpacity: 0.92,
      weight: 3,
      className: 'place-map__cluster-marker'
    });
    marker.bindTooltip(countLabel, {
      permanent: permanentLabel,
      direction: 'center',
      className: 'place-map__cluster-label'
    });
    const zoomIn = () => {
      this.markAutomaticCamera();
      this.map!.setView([lat, lng], Math.min(this.map!.getZoom() + 2, 17));
    };
    marker.on('click', zoomIn);
    marker.on('add', () => this.makeCircleInteractive(marker, `Apropar cluster de ${countLabel}`, zoomIn));
    marker.addTo(this.markersLayer);
  }

  private drawPlaceMarker(place: Place): void {
    if (!this.leaflet || !this.markersLayer) return;
    const marker = this.leaflet.marker([place.coordinates!.lat, place.coordinates!.lng], {
      icon: this.placeIcon(place, place.id === this.selectedPlaceId()),
      keyboard: true,
      title: `${place.name}: ${this.petStatusLabel(place.petAccessStatus)}`
    });
    marker.bindPopup(
      `<div class="place-map__popup">
        <p class="place-map__popup-type">${escapeHtml(place.categoryLabel || place.type)}</p>
        <strong>${escapeHtml(place.name)}</strong>
        <span>${escapeHtml(place.city)} · ${escapeHtml(this.petStatusLabel(place.petAccessStatus))}</span>
        <span>${place.isPetPolicyVerified ? '✓ Informació verificada' : 'Informació no verificada'}${place.distanceKm != null ? ` · ${place.distanceKm.toFixed(1)} km` : ''}</span>
      </div>`,
      { className: 'place-map__popup-shell' }
    );
    marker.on('click', () => this.placeSelected.emit(place.id));
    marker.on('add', () => {
      const markerElement = marker.getElement();
      markerElement?.setAttribute('role', 'button');
      markerElement?.setAttribute('aria-label', `Seleccionar ${place.name} al mapa`);
      markerElement?.setAttribute('tabindex', '0');
      markerElement?.addEventListener('keydown', (event) => {
        const keyEvent = event as KeyboardEvent;
        if (keyEvent.key === 'Enter' || keyEvent.key === ' ') {
          event.preventDefault();
          this.placeSelected.emit(place.id);
        }
      });
    });
    marker.addTo(this.markersLayer);
    this.markers.set(place.id, marker);
  }

  private makeCircleInteractive(
    marker: import('leaflet').CircleMarker,
    label: string,
    activate: () => void
  ): void {
    const element = marker.getElement();
    element?.setAttribute('role', 'button');
    element?.setAttribute('aria-label', label);
    element?.setAttribute('tabindex', '0');
    element?.addEventListener('keydown', (event) => {
      const keyEvent = event as KeyboardEvent;
      if (keyEvent.key === 'Enter' || keyEvent.key === ' ') {
        event.preventDefault();
        activate();
      }
    });
  }

  private placeIcon(place: Place, selected: boolean): import('leaflet').DivIcon {
    const status = place.petAccessStatus ?? 'Unknown';
    const statusClass = status === 'Allowed' ? 'allowed'
      : status === 'NotAllowed' ? 'denied'
      : status === 'TerraceOnly' || (place.petConditions?.length ?? 0) > 0 ? 'conditional'
      : 'unknown';
    return this.leaflet!.divIcon({
      className: '',
      html: `<span class="place-map__pet-pin place-map__pet-pin--${statusClass}${selected ? ' place-map__pet-pin--selected' : ''}" aria-hidden="true"><span>🐶</span></span>`,
      iconSize: selected ? [42, 50] : [34, 42],
      iconAnchor: selected ? [21, 48] : [17, 40],
      popupAnchor: [0, -38]
    });
  }

  private petStatusLabel(status?: string): string {
    switch (status) {
      case 'Allowed': return 'Accepta mascotes';
      case 'TerraceOnly': return 'Només terrassa';
      case 'NotAllowed': return 'No admet mascotes';
      case 'PendingReview': return 'Pendent de revisió';
      default: return 'Informació desconeguda';
    }
  }

  private markAutomaticCamera(): void {
    this.automaticCameraUntil = Date.now() + 600;
    this.viewportChanged.set(false);
  }

  private markViewportChanged(): void {
    const center = this.map?.getCenter();
    this.currentCenter.set(center ? { lat: center.lat, lng: center.lng } : null);
    if (Date.now() >= this.automaticCameraUntil) this.viewportChanged.set(true);
  }
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>'"]/g, (character) => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;'
  })[character] ?? character);
}
