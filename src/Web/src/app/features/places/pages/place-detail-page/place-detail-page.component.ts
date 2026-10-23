import { Component, computed, DestroyRef, effect, ElementRef, HostListener, inject, signal, ChangeDetectionStrategy, ViewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';

import { PlaceMapComponent } from '../../components/place-map/place-map.component';
import { PlaceShareMenuComponent } from '../../components/place-share-menu/place-share-menu.component';
import { PlaceService } from '../../services/place.service';
import { PlaceMediaAsset } from '../../models/place.model';
import { PlaceDetailLifeLineAnimation } from './place-detail-life-line-animation';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { WestiePreferencesService } from '../../../../shared/services/westie-preferences.service';
import { WestieBarkService } from '../../../../shared/services/westie-bark.service';
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
    PlaceMapComponent,
    PlaceShareMenuComponent
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
  private readonly westieBark = inject(WestieBarkService);
  private readonly lifeLineAnimation = new PlaceDetailLifeLineAnimation(
    this.host.nativeElement,
    () => this.playWestieBark()
  );
  private readonly route = inject(ActivatedRoute);
  private readonly placeService = inject(PlaceService);
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
  private shareFeedbackTimer: ReturnType<typeof setTimeout> | null = null;
  protected readonly shareMenuOpen = signal(false);

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
    ) return;
    void this.westieBark.playAtStartOfRoute();
  }

  private unlockWestieBarkFromUserAction(): void {
    if (!this.showWestie() || !this.westiePreferences.soundEnabled()) return;
    void this.westieBark.unlockFromUserAction();
  }

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.lifeLineAnimation.disconnect();
      this.westieBark.stop();
      if (this.shareFeedbackTimer) clearTimeout(this.shareFeedbackTimer);
    });
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
  protected readonly shareFeedback = signal('');
  protected readonly directionsUrl = computed(() => {
    const place = this.place();
    const lat = place?.coordinates?.lat;
    const lng = place?.coordinates?.lng;
    if (
      !Number.isFinite(lat)
      || !Number.isFinite(lng)
      || Math.abs(lat!) > 90
      || Math.abs(lng!) > 180
    ) return '';
    return this.buildDirectionsUrl(lat!, lng!);
  });

  private buildDirectionsUrl(latitude: number, longitude: number): string {
    const query = new URLSearchParams({ api: '1', destination: `${latitude},${longitude}` });
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
    this.unlockWestieBarkFromUserAction();
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

  protected sharePlace(): void {
    if (!this.place() || this.shareMenuOpen()) return;
    this.shareMenuOpen.set(true);
  }

  protected closeShareMenu(): void {
    this.shareMenuOpen.set(false);
    setTimeout(() => document.querySelector<HTMLElement>('.site-header__context-action')?.focus(), 0);
  }

  @HostListener('document:pointerdown')
  protected handleGlobalPointerDown(): void {
    this.unlockWestieBarkFromUserAction();
  }

  protected setShareFeedback(message: string): void {
    this.shareFeedback.set(message);
    if (this.shareFeedbackTimer) clearTimeout(this.shareFeedbackTimer);
    this.shareFeedbackTimer = setTimeout(() => this.shareFeedback.set(''), 2400);
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
