import { Component, computed, DestroyRef, effect, ElementRef, HostListener, inject, signal, ChangeDetectionStrategy, ViewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';

import { FavoritesService } from '../../../favorites/services/favorites.service';
import { PlaceMapComponent } from '../../components/place-map/place-map.component';
import { PlaceService } from '../../services/place.service';
import { PlaceMediaAsset } from '../../models/place.model';
import { PlaceDetailLifeLineAnimation } from './place-detail-life-line-animation';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { WestiePreferencesService } from '../../../../shared/services/westie-preferences.service';
import {
  contextTagsExcludingCity,
  hasPublicPetPolicy,
  hasPublicPrice,
  hasPublicRating,
  isLocationStubCopy,
  publicCategoryLabel,
  publicFeatureChips,
  publicQuickContext,
  shouldConfirmPetsByPhone
} from '../../utils/place-detail-copy';

@Component({
  selector: 'app-place-detail-page',
  imports: [
    RouterLink,
    SiteHeaderComponent,
    PlaceMapComponent
  ],
  templateUrl: './place-detail-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: [
    './place-detail-approved.component.scss',
    './place-detail-overlays.component.scss'
  ]
})
export class PlaceDetailPageComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);
  private readonly westiePreferences = inject(WestiePreferencesService);
  private readonly lifeLineAnimation = new PlaceDetailLifeLineAnimation(
    this.host.nativeElement,
    () => this.playWestieBark()
  );
  private readonly route = inject(ActivatedRoute);
  private readonly placeService = inject(PlaceService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly params = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap
  });
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap
  });
  private readonly loadedPlace = signal<ReturnType<PlaceService['getPlaceById']>>(undefined);
  private readonly detailState = signal<'loading' | 'loaded' | 'missing'>('loading');
  private loadSequence = 0;
  protected readonly galleryAssets = signal<PlaceMediaAsset[]>([]);
  protected readonly galleryTotal = signal(0);
  protected readonly galleryBusy = signal(false);
  protected readonly showWestie = this.westiePreferences.visible;
  private uploadTrigger: HTMLElement | null = null;
  private carouselTouchStartX: number | null = null;
  private favoriteFeedbackTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly westieBarkUrl: string | null = null;
  private westieBark: HTMLAudioElement | null = null;

  @ViewChild('animatedWestie')
  private set animatedWestie(element: ElementRef<HTMLImageElement> | undefined) {
    if (element) this.lifeLineAnimation.connect(element.nativeElement);
    else this.lifeLineAnimation.disconnect();
  }

  private playWestieBark(): void {
    if (
      !this.showWestie()
      || !this.westiePreferences.soundEnabled()
      || document.hidden
      || !this.westieBarkUrl
    ) return;

    this.westieBark ??= new Audio(this.westieBarkUrl);
    this.westieBark.volume = 0.45;
    this.westieBark.currentTime = 0;
    void this.westieBark.play().catch(() => undefined);
  }

  constructor() {
    this.destroyRef.onDestroy(() => this.lifeLineAnimation.disconnect());
    effect(() => {
      const id = this.params().get('id') ?? '';
      const sequence = ++this.loadSequence;
      this.detailState.set('loading');
      this.loadedPlace.set(undefined);
      this.activePhotoIndex.set(-1);
      if (!id) { this.detailState.set('missing'); return; }
      void this.placeService.loadById(id).then((place) => {
        if (sequence !== this.loadSequence) return;
        this.loadedPlace.set(place);
        this.detailState.set(place ? 'loaded' : 'missing');
        this.galleryAssets.set(place?.media ?? []);
        this.galleryTotal.set(place?.media?.length ?? 0);
        if (place) void this.loadGalleryPage(place.id, 0);
      });
    });
  }

  protected readonly place = this.loadedPlace.asReadonly();
  protected readonly canWriteFavorites = this.favoritesService.canWrite;
  protected readonly hasLoaded = computed(() => this.detailState() !== 'loading');
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
    const params = this.queryParams();
    const preserved: Record<string, string> = {};
    for (const key of params.keys) {
      if (key === 'fromMap') continue;
      const value = params.get(key);
      if (value) preserved[key] = value;
    }

    if (Object.keys(preserved).length === 0) {
      const currentPlace = this.place();
      if (currentPlace) preserved['city'] = currentPlace.city;
    }
    return preserved;
  });
  protected readonly cameFromMap = computed(() => this.queryParams().get('fromMap') === 'true');
  protected readonly hasPetPolicy = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? hasPublicPetPolicy(currentPlace) : false;
  });
  protected readonly confirmPetsByPhone = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? shouldConfirmPetsByPhone(currentPlace) : false;
  });
  protected readonly hasRating = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? hasPublicRating(currentPlace) : false;
  });
  protected readonly ratingStarFills = computed(() => {
    const rating = this.place()?.rating ?? 0;
    return [0, 1, 2, 3, 4].map((offset) =>
      Math.round(Math.min(1, Math.max(0, rating - offset)) * 100)
    );
  });
  protected readonly hasPrice = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? hasPublicPrice(currentPlace) : false;
  });
  protected readonly showsGoogleMapsAttribution = computed(() => {
    const provenance = this.place()?.dataProvenance?.trim() ?? '';
    return provenance === 'GooglePlaces' || provenance === 'Mixed';
  });
  protected readonly showsOpenStreetMapAttribution = computed(() =>
    this.place()?.dataProvenance?.trim() === 'OpenStreetMap'
  );
  protected readonly contextDescription = computed(() => {
    const currentPlace = this.place();
    if (!currentPlace || isLocationStubCopy(
      currentPlace.description,
      currentPlace.city,
      currentPlace.address,
      currentPlace.name
    )) {
      return '';
    }

    return publicQuickContext(currentPlace.description.trim(), currentPlace.name);
  });
  protected readonly contextTags = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? contextTagsExcludingCity(currentPlace.tags, currentPlace.city) : [];
  });
  protected readonly featureChips = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? publicFeatureChips(currentPlace) : [];
  });
  protected readonly categoryEyebrow = computed(() => {
    const currentPlace = this.place();
    return currentPlace ? publicCategoryLabel(currentPlace) : '';
  });
  protected readonly hasQuickContext = computed(
    () => this.contextDescription().length > 0 || this.contextTags().length > 0
  );
  protected readonly hasMeaningfulHighlights = computed(() =>
    this.featureChips().length > 0
    || this.contextTags().length > 0
    || this.contextDescription().length >= 40
  );
  protected readonly formattedAddress = computed(() => {
    const place = this.place();
    if (!place) return '';
    const address = place.address.trim().replace(/,\s*$/u, '');
    const city = place.city.trim();
    if (!city) return address;
    const foldedAddress = address.toLocaleLowerCase('ca');
    const foldedCity = city.toLocaleLowerCase('ca');
    return foldedAddress === foldedCity || foldedAddress.endsWith(`, ${foldedCity}`)
      ? address
      : `${address}, ${city}`;
  });
  protected readonly petStatusLabel = computed(() => {
    switch (this.place()?.petAccessStatus) {
      case 'Allowed': return 'Accepta mascotes';
      case 'TerraceOnly': return 'Només terrassa';
      case 'NotAllowed': return 'No admet mascotes';
      case 'PendingReview': return 'Pendent de revisió';
      default: return 'Desconegut';
    }
  });
  protected readonly petStatusDescription = computed(() => {
    const place = this.place();
    switch (place?.petAccessStatus) {
      case 'Allowed': return place.isPetPolicyVerified
        ? 'Aquest establiment ha confirmat que admet mascotes.'
        : 'Consta que admet mascotes, però la informació encara no està verificada.';
      case 'TerraceOnly': return place.isPetPolicyVerified
        ? 'L’accés amb mascotes està confirmat només a la terrassa.'
        : 'Consta accés només a la terrassa, pendent de verificació.';
      case 'NotAllowed': return place.isPetPolicyVerified
        ? 'Aquest establiment ha confirmat que no admet mascotes.'
        : 'Consta que no admet mascotes, però la informació encara no està verificada.';
      case 'PendingReview': return 'La política d’accés amb mascotes està pendent de revisió.';
      default: return 'Encara no tenim informació confirmada sobre l’accés amb mascotes.';
    }
  });
  protected readonly petStatusTone = computed(() => {
    switch (this.place()?.petAccessStatus) {
      case 'Allowed': return this.place()?.isPetPolicyVerified ? 'positive' : 'pending';
      case 'NotAllowed': return this.place()?.isPetPolicyVerified ? 'negative' : 'pending';
      case 'TerraceOnly':
      case 'PendingReview': return 'pending';
      default: return 'unknown';
    }
  });
  protected readonly galleryMedia = computed(() =>
    this.galleryAssets().filter((asset) =>
      asset.verificationStatus === 'Verified' && asset.servedUrl.trim().length > 0
    )
  );
  protected readonly activeHeroPhotoIndex = computed(() => {
    const media = this.galleryMedia();
    const selected = this.activePhotoIndex();
    if (selected >= 0 && selected < media.length) return selected;
    const primary = media.findIndex(asset => asset.isPrimary);
    return primary >= 0 ? primary : 0;
  });
  protected readonly primaryMedia = computed(() => this.galleryMedia()[this.activeHeroPhotoIndex()] ?? null);
  protected readonly heroMedia = computed(() => this.galleryMedia().slice(0, 5));
  protected readonly activePhotoIndex = signal(-1);
  protected readonly lightboxIndex = signal<number | null>(null);
  protected readonly galleryOpen = signal(false);
  protected readonly uploadOpen = signal(false);
  protected readonly selectedPhotos = signal<SelectedPhoto[]>([]);
  protected readonly rightsAccepted = signal(false);
  protected readonly uploadBusy = signal(false);
  protected readonly uploadMessage = signal('');
  protected readonly uploadError = signal('');
  protected readonly favoriteFeedback = signal('');
  protected readonly directionsUrl = computed(() => {
    const place = this.place();
    if (!place) return '';
    const destination = place.coordinates
      ? `${place.coordinates.lat},${place.coordinates.lng}`
      : `${place.address}, ${place.city}`;
    return this.buildDirectionsUrl(destination);
  });

  protected openDirections(event: MouseEvent): void {
    const fallbackUrl = this.directionsUrl();
    if (!fallbackUrl) return;

    const navigationWindow = window.open(fallbackUrl, '_blank');
    if (!navigationWindow) return;

    event.preventDefault();
    navigationWindow.opener = null;
    if (!navigator.geolocation) return;

    navigator.geolocation.getCurrentPosition(
      ({ coords }) => {
        const place = this.place();
        if (!place) return;
        const destination = place.coordinates
          ? `${place.coordinates.lat},${place.coordinates.lng}`
          : `${place.address}, ${place.city}`;
        navigationWindow.location.href = this.buildDirectionsUrl(
          destination,
          `${coords.latitude},${coords.longitude}`
        );
      },
      () => undefined,
      { enableHighAccuracy: false, timeout: 5000, maximumAge: 60000 }
    );
  }

  private buildDirectionsUrl(destination: string, origin?: string): string {
    const query = new URLSearchParams({ api: '1', destination });
    if (origin) query.set('origin', origin);
    return `https://www.google.com/maps/dir/?${query.toString()}`;
  }

  protected mediaSourceLabel(source: string): string {
    switch (source) {
      case 'PetiLoc': return 'PetiLoc';
      case 'Owner': return 'Propietari';
      case 'User': return 'Usuari';
      case 'WikimediaCommons': return 'Wikimedia Commons';
      case 'Mapillary': return 'Mapillary';
      case 'OfficialWebsite': return 'Web oficial';
      default: return source;
    }
  }

  protected toggleFavorite(placeId: string): void {
    const wasFavorite = this.favoritesService.isFavorite(placeId);
    this.favoritesService.toggle(placeId);
    this.favoriteFeedback.set(wasFavorite
      ? 'Eliminat dels teus favorits.'
      : 'Desat als teus favorits.');
    if (this.favoriteFeedbackTimer) clearTimeout(this.favoriteFeedbackTimer);
    this.favoriteFeedbackTimer = setTimeout(() => this.favoriteFeedback.set(''), 2400);
  }

  protected isFavorite(placeId: string): boolean {
    return this.favoritesService.isFavorite(placeId);
  }

  protected getTypeLabel(type: string): string {
    return this.placeService.resolveTypeLabel(type);
  }

  protected conditionLabels(conditions: { code: string; value?: string | null }[]): string {
    return conditions.map((condition) => condition.value || condition.code).join(' · ');
  }

  protected selectPhotos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const existing = this.selectedPhotos();
    const candidates = Array.from(input.files ?? []);
    this.uploadError.set('');
    if (existing.length + candidates.length > 5) {
      this.uploadError.set('Pots seleccionar un màxim de 5 fotografies per enviament.');
      input.value = '';
      return;
    }
    const invalid = candidates.find((file) => !file.type.startsWith('image/') || file.size > 10_000_000);
    if (invalid) {
      this.uploadError.set(`${invalid.name}: cal una imatge de com a màxim 10 MB.`);
      input.value = '';
      return;
    }
    this.selectedPhotos.set([...existing, ...candidates.map((file) => ({ file, previewUrl: URL.createObjectURL(file) }))]);
    input.value = '';
  }

  protected removePhoto(index: number): void {
    const photos = [...this.selectedPhotos()];
    const [removed] = photos.splice(index, 1);
    if (removed) URL.revokeObjectURL(removed.previewUrl);
    this.selectedPhotos.set(photos);
  }

  protected async uploadPhotos(): Promise<void> {
    const place = this.place();
    if (!place || !this.selectedPhotos().length || !this.rightsAccepted()) return;
    this.uploadBusy.set(true);
    this.uploadError.set('');
    this.uploadMessage.set('');
    try {
      await this.placeService.uploadPhotos(place.id, this.selectedPhotos().map((photo) => photo.file),
        'Declaro que soc titular dels drets de les fotografies o que disposo d’autorització suficient perquè PetiLoc les emmagatzemi, redimensioni, converteixi, publiqui i moderi. Text provisional pendent de revisió jurídica.');
      this.selectedPhotos().forEach((photo) => URL.revokeObjectURL(photo.previewUrl));
      this.selectedPhotos.set([]);
      this.rightsAccepted.set(false);
      this.uploadMessage.set('Fotografies enviades. Queden pendents de moderació i encara no són públiques.');
    } catch (error) {
      this.uploadError.set(error instanceof Error ? error.message : 'No s’han pogut enviar les fotografies.');
    } finally {
      this.uploadBusy.set(false);
    }
  }

  protected async loadMorePhotos(): Promise<void> {
    const place = this.place();
    if (!place || this.galleryBusy()) return;
    await this.loadGalleryPage(place.id, this.galleryAssets().length);
  }

  private async loadGalleryPage(placeId: string, skip: number): Promise<void> {
    this.galleryBusy.set(true);
    try {
      const page = await this.placeService.loadMediaPage(placeId, skip, 20);
      this.galleryTotal.set(page.total);
      this.galleryAssets.update((existing) => skip === 0 ? page.items : [
        ...existing,
        ...page.items.filter((asset) => !existing.some((current) => current.id === asset.id))
      ]);
    } finally { this.galleryBusy.set(false); }
  }

  protected openLightbox(index: number): void {
    this.lightboxIndex.set(index);
    setTimeout(() => document.querySelector<HTMLElement>('.place-detail-page__lightbox-close')?.focus(), 0);
  }
  protected openHeroPhoto(index: number): void {
    this.activePhotoIndex.set(index);
    this.openLightbox(index);
  }
  protected closeLightbox(): void { this.lightboxIndex.set(null); }
  protected openGallery(): void {
    this.galleryOpen.set(true);
    setTimeout(() => document.getElementById('place-gallery-title')?.focus(), 0);
  }
  protected closeGallery(): void { this.galleryOpen.set(false); }

  protected openUploadModal(event?: Event): void {
    this.uploadTrigger = event?.currentTarget instanceof HTMLElement ? event.currentTarget : null;
    this.uploadOpen.set(true);
    setTimeout(() => document.querySelector<HTMLElement>('.place-detail-page__upload-close')?.focus(), 0);
  }

  protected closeUploadModal(): void {
    this.uploadOpen.set(false);
    setTimeout(() => this.uploadTrigger?.focus(), 0);
  }

  protected carouselTouchStart(event: TouchEvent): void {
    this.carouselTouchStartX = event.changedTouches[0]?.clientX ?? null;
  }

  protected carouselTouchEnd(event: TouchEvent): void {
    const endX = event.changedTouches[0]?.clientX;
    if (this.carouselTouchStartX == null || endX == null) return;
    const distance = endX - this.carouselTouchStartX;
    this.carouselTouchStartX = null;
    if (Math.abs(distance) >= 42) this.movePhoto(distance > 0 ? -1 : 1);
  }

  @HostListener('document:keydown', ['$event'])
  protected handleGlobalKeydown(event: KeyboardEvent): void {
    if (event.key === 'Tab' && (this.uploadOpen() || this.galleryOpen() || this.lightboxIndex() !== null)) {
      const modalSelector = this.uploadOpen()
        ? '.place-detail-page__upload'
        : this.galleryOpen()
          ? '.place-detail-page__gallery'
          : '.place-detail-page__lightbox';
      const focusable = Array.from(document.querySelectorAll<HTMLElement>(
        `${modalSelector} button:not([disabled]), ${modalSelector} a[href], ${modalSelector} input:not([disabled]), ${modalSelector} [tabindex]:not([tabindex="-1"])`
      )).filter(element => element.offsetParent !== null);
      const first = focusable[0];
      const last = focusable.at(-1);
      if (first && last && event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (first && last && !event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }
    if (event.key === 'Escape') {
      if (this.lightboxIndex() !== null) this.closeLightbox();
      else if (this.uploadOpen()) this.closeUploadModal();
      else if (this.galleryOpen()) this.closeGallery();
      return;
    }
    if (this.lightboxIndex() === null) return;
    if (event.key === 'ArrowLeft') this.movePhoto(-1);
    if (event.key === 'ArrowRight') this.movePhoto(1);
  }
  protected movePhoto(offset: number): void {
    const count = this.galleryMedia().length;
    if (!count) return;
    const current = this.lightboxIndex() ?? this.activePhotoIndex();
    const next = (current + offset + count) % count;
    if (this.lightboxIndex() == null) this.activePhotoIndex.set(next); else this.lightboxIndex.set(next);
  }

  protected async sharePlace(): Promise<void> {
    const place = this.place();
    if (!place) return;
    const data = { title: place.name, text: `${place.name} · ${place.city}`, url: location.href };
    if (navigator.share) await navigator.share(data).catch(() => undefined);
    else await navigator.clipboard?.writeText(location.href);
  }

  protected async reportPhoto(assetId: string): Promise<void> {
    const place = this.place();
    const reason = window.prompt('Explica breument per què cal revisar o retirar aquesta fotografia:')?.trim();
    if (!place || !reason) return;
    try {
      await this.placeService.reportPhoto(place.id, assetId, reason);
      this.uploadMessage.set('Denúncia registrada. ADMIN revisarà la fotografia.');
    } catch {
      this.uploadError.set('No s’ha pogut registrar la denúncia. Torna-ho a provar.');
    }
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

interface SelectedPhoto { file: File; previewUrl: string; }
