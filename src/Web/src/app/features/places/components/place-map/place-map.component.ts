import {
  AfterViewInit,
  Component,
  computed,
  ElementRef,
  inject,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  input,
  output,
  ChangeDetectionStrategy
} from '@angular/core';
import { signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';

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
import { clampMapHeight, mapHeightLimits } from './place-map-layout';

type LeafletModule = typeof import('leaflet');
type LeafletMap = import('leaflet').Map;
type LeafletLayerGroup = import('leaflet').LayerGroup;
export interface PlaceMapViewport { north: number; south: number; east: number; west: number; }
export interface PlaceMapMunicipality {
  id: string;
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
  @ViewChild('mapShell') private readonly mapShell?: ElementRef<HTMLElement>;

  private readonly document = inject(DOCUMENT);

  readonly places = input.required<Place[]>();
  readonly areas = input<PlaceSearchMapArea[]>([]);
  readonly selectedPlaceId = input<string | null>(null);
  readonly height = input('24rem');
  readonly emptyTitle = input('No hi ha ubicacions per mostrar');
  readonly emptyCopy = input('Ajusta els filtres per veure llocs al mapa.');
  readonly showToolbarActions = input(true);
  readonly compactPresentation = input(false);
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
  private activeMunicipalityKey = '';
  private initialCameraApplied = false;
  private hadPlaces = false;
  private resizeObserver?: ResizeObserver;
  private resizeFrame?: number;
  private markerRedrawFrame?: number;
  private resizingPointerId: number | null = null;
  private resizeStartY = 0;
  private resizeStartHeight = 0;
  private layoutCamera?: { center: import('leaflet').LatLng; zoom: number };
  private stableCamera?: { center: import('leaflet').LatLng; zoom: number };
  private layoutCameraReleaseTimer?: number;
  protected readonly viewportChanged = signal(false);
  protected readonly visibleLayer = signal<PlaceMapLayerMode>('municipality');
  protected readonly currentZoom = signal(CITY_INITIAL_ZOOM);
  protected readonly currentCenter = signal<{ lat: number; lng: number } | null>(null);
  protected readonly mapHeightPx = signal<number | null>(null);
  protected readonly fullscreen = signal(false);
  protected readonly fallbackFullscreen = signal(false);
  protected readonly canvasHeight = computed(() => {
    const resized = this.mapHeightPx();
    return resized === null ? this.height() : `${resized}px`;
  });
  private automaticCameraUntil = 0;

  private readonly onFullscreenChange = (): void => {
    if (this.stableCamera) this.layoutCamera = this.stableCamera;
    this.fullscreen.set(this.document.fullscreenElement === this.mapShell?.nativeElement);
    this.scheduleMapResize();
    this.releaseLayoutCameraSoon();
  };

  private readonly onDocumentKeydown = (event: KeyboardEvent): void => {
    if (event.key === 'Escape' && this.fallbackFullscreen()) {
      this.captureLayoutCamera();
      this.fallbackFullscreen.set(false);
      this.fullscreen.set(false);
      this.scheduleMapResize();
    }
  };

  /** Spain-first listing view (do not fitBounds when a pin is in Australia, etc.). */
  private static readonly defaultMapCenter: [number, number] = SPAIN_MAP_CENTER;
  private static readonly defaultMapZoom = SPAIN_MAP_ZOOM;
  /** If visible pins span more than this, keep the camera on Spain instead of the world. */
  private static readonly maxFitSpanMeters = 1_200_000;

  async ngAfterViewInit(): Promise<void> {
    await this.ensureMap();
    const canvas = this.mapContainer?.nativeElement;
    if (canvas && typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => this.scheduleMapResize());
      this.resizeObserver.observe(canvas);
    }
    this.document.addEventListener('fullscreenchange', this.onFullscreenChange);
    this.document.addEventListener('keydown', this.onDocumentKeydown);
    this.renderMap();
  }

  ngOnChanges(_changes: SimpleChanges): void {
    this.renderMap();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.document.removeEventListener('fullscreenchange', this.onFullscreenChange);
    this.document.removeEventListener('keydown', this.onDocumentKeydown);
    if (this.resizeFrame !== undefined) cancelAnimationFrame(this.resizeFrame);
    if (this.markerRedrawFrame !== undefined) cancelAnimationFrame(this.markerRedrawFrame);
    if (this.layoutCameraReleaseTimer !== undefined) window.clearTimeout(this.layoutCameraReleaseTimer);
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
    if (!this.map || !this.leaflet) return;
    if (this.municipality()) {
      const municipality = this.municipality()!;
      this.markAutomaticCamera();
      this.map.panTo([municipality.lat, municipality.lng]);
    } else if (this.hasPlaces) {
      const bounds = this.places().reduce((accumulator, place) => {
        accumulator.extend([place.coordinates!.lat, place.coordinates!.lng]);
        return accumulator;
      }, this.leaflet.latLngBounds([]));
      this.markAutomaticCamera();
      this.map.panTo(bounds.getCenter());
    } else {
      const focus = this.focusCenter();
      this.markAutomaticCamera();
      this.map.panTo(focus ? [focus.lat, focus.lng] : PlaceMapComponent.defaultMapCenter);
    }
  }

  protected clearSelection(): void {
    this.selectionCleared.emit();
    this.map?.closePopup();
  }

  protected beginResize(event: PointerEvent): void {
    if (this.fullscreen()) return;
    const canvas = this.mapContainer?.nativeElement;
    const handle = event.currentTarget as HTMLElement | null;
    if (!canvas || !handle) return;
    this.resizingPointerId = event.pointerId;
    this.captureLayoutCamera();
    this.resizeStartY = event.clientY;
    this.resizeStartHeight = canvas.getBoundingClientRect().height;
    try {
      handle.setPointerCapture?.(event.pointerId);
    } catch {
      // Synthetic accessibility tests do not create an active native pointer to capture.
    }
    event.preventDefault();
  }

  protected resizeFromPointer(event: PointerEvent): void {
    if (this.resizingPointerId !== event.pointerId) return;
    this.setMapHeight(this.resizeStartHeight + event.clientY - this.resizeStartY);
  }

  protected finishResize(event: PointerEvent): void {
    if (this.resizingPointerId !== event.pointerId) return;
    const handle = event.currentTarget as HTMLElement | null;
    if (handle?.hasPointerCapture?.(event.pointerId)) handle.releasePointerCapture(event.pointerId);
    this.resizingPointerId = null;
    this.scheduleMapResize();
    this.releaseLayoutCameraSoon();
  }

  protected resizeFromKeyboard(event: KeyboardEvent): void {
    const current = this.mapContainer?.nativeElement.getBoundingClientRect().height ?? 0;
    const amount = event.shiftKey ? 64 : 24;
    const requested = event.key === 'ArrowUp' ? current - amount
      : event.key === 'ArrowDown' ? current + amount
      : event.key === 'Home' ? this.minimumMapHeight()
      : event.key === 'End' ? this.maximumMapHeight()
      : null;
    if (requested === null) return;
    event.preventDefault();
    this.captureLayoutCamera();
    this.setMapHeight(requested);
    this.releaseLayoutCameraSoon();
  }

  protected async toggleFullscreen(): Promise<void> {
    const shell = this.mapShell?.nativeElement;
    if (!shell) return;
    this.captureLayoutCamera();

    if (this.fullscreen()) {
      if (this.document.fullscreenElement === shell) {
        await this.document.exitFullscreen();
      } else {
        this.fallbackFullscreen.set(false);
        this.fullscreen.set(false);
        this.scheduleMapResize();
        this.releaseLayoutCameraSoon();
      }
      return;
    }

    if (this.document.fullscreenEnabled && shell.requestFullscreen) {
      try {
        await shell.requestFullscreen();
        return;
      } catch {
        // A fixed overlay preserves the same interaction when the browser denies Fullscreen API.
      }
    }
    this.fallbackFullscreen.set(true);
    this.fullscreen.set(true);
    this.scheduleMapResize();
    this.releaseLayoutCameraSoon();
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
      const loaded = await import('leaflet') as LeafletModule & { default?: LeafletModule };
      this.leaflet = typeof loaded.map === 'function' ? loaded : loaded.default;
      if (!this.leaflet) throw new Error('No s’ha pogut inicialitzar Leaflet.');

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
      this.map.on('moveend', () => {
        this.recordStableCamera();
        this.scheduleMarkerRedraw();
        this.markViewportChanged();
      });
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
      const municipalityKey = municipality
        ? `${municipality.id}:${municipality.lat}:${municipality.lng}`
        : '';
      const municipalityChanged = municipalityKey !== '' && municipalityKey !== this.activeMunicipalityKey;

      if (municipalityChanged) {
        // The municipality usually arrives after the first async search. It must
        // establish the territorial camera even if Leaflet already showed Spain.
        this.setMunicipalityView();
        this.initialCameraApplied = true;
      } else if (!this.initialCameraApplied) {
        if (municipality) this.setMunicipalityView();
        else if (this.hasPlaces) this.fitMapToPlaces();
        else this.setEmptyMapView();
        this.initialCameraApplied = true;
      } else if (!municipality && !this.hadPlaces && this.hasPlaces) {
        this.fitMapToPlaces();
      }
      this.activeMunicipalityKey = municipalityKey;
      this.hadPlaces = this.hasPlaces;

      if (dataChanged || municipalityChanged) {
        this.redrawMarkers();
        this.lastDataKey = dataKey;
      }

      const selectedPlaceId = this.selectedPlaceId();
      this.applyMarkerSelection(selectedPlaceId);

      if (selectedPlaceId) {
        const selectedPlace = this.places().find((place) => place.id === selectedPlaceId);
        if (selectedPlace) {
          this.markAutomaticCamera();
          // Selecting a Place may move the centre to reveal it, but must never
          // replace the zoom chosen manually by the user.
          this.map.panTo([selectedPlace.coordinates!.lat, selectedPlace.coordinates!.lng]);
          this.markers.get(selectedPlace.id)?.openPopup();
          return;
        }
      }

    });
  }

  private setMapHeight(requestedHeight: number): void {
    this.mapHeightPx.set(clampMapHeight(requestedHeight, window.innerWidth, window.innerHeight));
  }

  protected minimumMapHeight(): number {
    return mapHeightLimits(window.innerWidth, window.innerHeight).min;
  }

  protected maximumMapHeight(): number {
    return mapHeightLimits(window.innerWidth, window.innerHeight).max;
  }

  private scheduleMapResize(): void {
    if (this.resizeFrame !== undefined) cancelAnimationFrame(this.resizeFrame);
    this.resizeFrame = requestAnimationFrame(() => {
      this.resizeFrame = undefined;
      const preservedCenter = this.layoutCamera?.center ?? this.map?.getCenter();
      const preservedZoom = this.layoutCamera?.zoom ?? this.map?.getZoom();
      this.markAutomaticCamera();
      this.map?.invalidateSize({ animate: false, pan: false });
      if (preservedCenter && preservedZoom !== undefined) {
        this.map?.setView(preservedCenter, preservedZoom, { animate: false });
      }
      const center = this.map?.getCenter();
      this.currentCenter.set(center ? { lat: center.lat, lng: center.lng } : null);
      this.scheduleMarkerRedraw();
    });
  }

  private scheduleMarkerRedraw(): void {
    if (this.markerRedrawFrame !== undefined) cancelAnimationFrame(this.markerRedrawFrame);
    this.markerRedrawFrame = requestAnimationFrame(() => {
      this.markerRedrawFrame = undefined;
      this.redrawMarkers();
      this.applyMarkerSelection(this.selectedPlaceId());
    });
  }

  private captureLayoutCamera(): void {
    if (!this.map) return;
    this.layoutCamera = this.stableCamera ?? { center: this.map.getCenter(), zoom: this.map.getZoom() };
    if (this.layoutCameraReleaseTimer !== undefined) window.clearTimeout(this.layoutCameraReleaseTimer);
  }

  private releaseLayoutCameraSoon(): void {
    if (this.layoutCameraReleaseTimer !== undefined) window.clearTimeout(this.layoutCameraReleaseTimer);
    this.layoutCameraReleaseTimer = window.setTimeout(() => {
      this.layoutCamera = undefined;
      this.layoutCameraReleaseTimer = undefined;
    }, 150);
  }

  private recordStableCamera(): void {
    if (!this.map) return;
    this.stableCamera = { center: this.map.getCenter(), zoom: this.map.getZoom() };
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
    this.recordStableCamera();
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
          id: area.territorialUnitId,
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
      this.drawMunicipalityMarker({ id: city, name: city, lat, lng, placeCount: places.length });
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
    for (const places of this.groupPlacesByPixels(72, false)) this.drawCluster(places, true);
  }

  private drawPlacesWithCollisionClusters(zoom: number): void {
    const collisionPixels = zoom >= 15 ? 24 : 36;
    for (const places of this.groupPlacesByPixels(collisionPixels, true)) {
      if (places.length > 1) this.drawCluster(places, false);
      else this.drawPlaceMarker(places[0]);
    }
  }

  private groupPlacesByPixels(cellSize: number, viewportOnly: boolean): Place[][] {
    if (!this.map) return [];
    const groups = new Map<string, Place[]>();
    const renderBounds = this.map.getBounds().pad(0.15);
    for (const place of this.places()) {
      if (viewportOnly && !renderBounds.contains([place.coordinates!.lat, place.coordinates!.lng])) continue;
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
