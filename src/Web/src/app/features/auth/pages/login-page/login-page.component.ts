import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  ErrorNotificationsService,
  NotificationTone
} from '../../../../core/services/error-notifications.service';
import { CityComboboxComponent } from '../../../../shared/components/city-combobox/city-combobox.component';
import { PLACES_FAKE } from '../../../places/mock/places.fake';
import { Place, PlaceFilters } from '../../../places/models/place.model';
import { PlaceService } from '../../../places/services/place.service';
import { normalizeSearchQuery, placeMatchesFreeTextSearch } from '../../../places/utils/place-text-search';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CityComboboxComponent],
  templateUrl: './login-page.component.html',
  styleUrls: ['./login-page.component.scss']
})
export class LoginPageComponent implements AfterViewInit, OnDestroy {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(ErrorNotificationsService);
  private readonly placeService = inject(PlaceService);
  private readonly previewFiltersState = signal<PlaceFilters>({
    search: '',
    city: '',
    type: 'hotel',
    pet: 'dogs'
  });

  protected readonly previewFilters = this.previewFiltersState.asReadonly();
  protected readonly authProviders = signal<string[]>([]);
  protected readonly googleProvider = signal<{ clientId: string } | null>(null);
  protected readonly linkedInProvider = signal<boolean>(false);
  protected readonly facebookProvider = signal<boolean>(false);
  protected readonly googleButtonVisible = signal(false);
  protected readonly previewCities = computed(() =>
    [...new Set(LOGIN_PREVIEW_PLACES.map((place) => place.city))].sort((a, b) => a.localeCompare(b))
  );
  protected readonly previewTypes = this.placeService.getAvailableTypes();
  protected readonly samplePlaces = computed(() => this.filterPreviewPlaces(this.previewFilters()).slice(0, 4));
  protected readonly loginPreviewRoute = computed(() => {
    const filters = this.previewFilters();
    const queryParams = {
      search: filters.search || null,
      city: filters.city || null,
      type: filters.type || null,
      pet: filters.pet !== 'all' ? filters.pet : null
    };

    return this.router.serializeUrl(
      this.router.createUrlTree(['/places'], {
        queryParams
      })
    );
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  @ViewChild('loginSubmitButton') private loginSubmitButton?: ElementRef<HTMLButtonElement>;
  @ViewChild('googleButtonHost') private googleButtonHost?: ElementRef<HTMLDivElement>;
  @ViewChild('previewMapContainer') private previewMapContainer?: ElementRef<HTMLDivElement>;
  private googleButtonRendered = false;
  private googleScriptPromise: Promise<void> | null = null;
  private previewMap?: import('leaflet').Map;
  private readonly isLocalhost =
    typeof window !== 'undefined' &&
    (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

  constructor() {
    effect(() => {
      this.googleProvider();
      queueMicrotask(() => {
        void this.tryRenderGoogleButtonAsync();
      });
    });

    const federatedError = this.route.snapshot.queryParamMap.get('federatedError');
    if (federatedError) {
      this.notifyUser(
        'Login federat incomplet',
        'LinkedIn no ha retornat una sessió vàlida al backend.',
        'error'
      );
      void this.router.navigate([], {
        relativeTo: this.route,
        replaceUrl: true,
        queryParams: {
          federatedError: null
        },
        queryParamsHandling: 'merge'
      });
    }

    void this.loadProvidersAsync();
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => {
      void this.tryRenderGoogleButtonAsync();
    });
    queueMicrotask(() => {
      void this.ensurePreviewMapAsync();
    });
  }

  ngOnDestroy(): void {
    this.previewMap?.remove();
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifyUser(
        'Revisa el formulari',
        'Cal informar un email vàlid i una contrasenya d’almenys 6 caràcters.',
        'error'
      );
      return;
    }

    const result = await this.authService.login(this.form.getRawValue());

    if (!result.ok) {
      this.notifyUser(
        'Credencials incorrectes',
        'L’usuari o la contrasenya no són correctes. Revisa les dades i torna-ho a provar.',
        'error'
      );
      return;
    }

    const redirectTo = this.route.snapshot.queryParamMap.get('redirectTo');
    void this.router.navigateByUrl(redirectTo || this.authService.getPostLoginRoute());
  }

  protected goToPreviewSearch(): void {
    this.notifyUser(
      'Cerca preparada',
      'Inicia sessió per obrir Llocs amb els filtres seleccionats. Després del login aniràs directament a la cerca.',
      'info'
    );
    void this.router.navigate(['/login'], {
      queryParams: {
        redirectTo: this.loginPreviewRoute()
      }
    });
  }

  protected generateRoutesPreview(): void {
    this.notifyUser(
      'Preview de rutes',
      'Aquesta acció encara no genera rutes reals. Primer inicia sessió; després podràs obrir Llocs amb els filtres del preview.',
      'info'
    );
  }

  protected getTypeLabel(type: string): string {
    return this.placeService.resolveTypeLabel(type);
  }

  protected updatePreviewFilters(partial: Partial<PlaceFilters>): void {
    this.previewFiltersState.update((current) => ({
      ...current,
      ...partial
    }));
  }

  protected onPreviewSearch(event: Event): void {
    this.updatePreviewFilters({
      search: (event.target as HTMLInputElement).value
    });
  }

  protected onPreviewCityChange(city: string): void {
    this.updatePreviewFilters({
      city
    });
  }

  protected onPreviewType(event: Event): void {
    this.updatePreviewFilters({
      type: (event.target as HTMLSelectElement).value
    });
  }

  protected onPreviewPet(event: Event): void {
    this.updatePreviewFilters({
      pet: (event.target as HTMLSelectElement).value as PlaceFilters['pet']
    });
  }

  protected startFacebookLogin(): void {
    const redirectTo = this.route.snapshot.queryParamMap.get('redirectTo');
    window.location.href = this.authService.getFacebookStartUrl(redirectTo);
  }

  protected startLinkedInLogin(): void {
    const redirectTo = this.route.snapshot.queryParamMap.get('redirectTo');
    window.location.href = this.authService.getLinkedInStartUrl(redirectTo);
  }

  private async loadProvidersAsync(): Promise<void> {
    try {
      const providers = await this.authService.getProviders();
      this.authProviders.set(
        providers.filter((provider) => provider.key !== 'password').map((provider) => provider.displayName)
      );
      const google = providers.find((provider) => provider.key === 'google' && provider.configured && provider.clientId);
      const linkedIn = providers.find((provider) => provider.key === 'linkedin' && provider.configured);
      const facebook = providers.find((provider) => provider.key === 'facebook' && provider.configured);
      this.googleProvider.set(
        this.isLocalhost || !google?.clientId ? null : { clientId: google.clientId }
      );
      this.linkedInProvider.set(Boolean(linkedIn));
      this.facebookProvider.set(Boolean(facebook));
      void this.tryRenderGoogleButtonAsync();
    } catch {
      this.authProviders.set(['Google', 'LinkedIn', 'Facebook']);
      this.googleProvider.set(null);
      this.linkedInProvider.set(false);
      this.facebookProvider.set(false);
    }
  }

  private async ensurePreviewMapAsync(): Promise<void> {
    if (this.previewMap || !this.previewMapContainer?.nativeElement) {
      return;
    }

    const leaflet = await import('leaflet');
    const container = this.previewMapContainer.nativeElement;

    // Leaflet can keep container metadata across fast re-renders or HMR in dev mode.
    if ('_leaflet_id' in container) {
      delete (container as HTMLDivElement & { _leaflet_id?: number })._leaflet_id;
    }

    const map = leaflet.map(container, {
      zoomControl: false,
      attributionControl: false,
      scrollWheelZoom: false,
      dragging: false,
      doubleClickZoom: false,
      boxZoom: false,
      keyboard: false,
      touchZoom: false
    });

    leaflet
      .tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19
      })
      .addTo(map);

    map.setView([40.25, -3.7], 6);
    this.previewMap = map;
    setTimeout(() => {
      this.previewMap?.invalidateSize();
    }, 0);
  }

  private async tryRenderGoogleButtonAsync(): Promise<void> {
    const provider = this.googleProvider();
    const host = this.googleButtonHost?.nativeElement;

    if (!provider || !host || this.googleButtonRendered) {
      return;
    }

    await this.loadGoogleScriptAsync();

    if (!window.google?.accounts?.id) {
      return;
    }

    host.innerHTML = '';
    const submitWidth = this.loginSubmitButton?.nativeElement.getBoundingClientRect().width ?? 0;
    const fallbackWidth =
      host.parentElement?.getBoundingClientRect().width ?? host.getBoundingClientRect().width ?? host.clientWidth ?? 320;
    const width = Math.max(280, Math.round(submitWidth || fallbackWidth));

    window.google.accounts.id.initialize({
      client_id: provider.clientId,
      callback: ({ credential }) => {
        void this.handleGoogleCredentialAsync(credential);
      }
    });

    window.google.accounts.id.renderButton(host, {
      theme: 'outline',
      size: 'large',
      shape: 'pill',
      text: 'signin_with',
      width
    });

    this.googleButtonRendered = true;
    this.googleButtonVisible.set(true);
  }

  private loadGoogleScriptAsync(): Promise<void> {
    if (window.google?.accounts?.id) {
      return Promise.resolve();
    }

    if (this.googleScriptPromise) {
      return this.googleScriptPromise;
    }

    this.googleScriptPromise = new Promise<void>((resolve, reject) => {
      const existing = document.querySelector<HTMLScriptElement>('script[data-google-identity]');

      if (existing) {
        existing.addEventListener('load', () => resolve(), { once: true });
        existing.addEventListener('error', () => reject(new Error('No s’ha pogut carregar Google Identity Services.')), {
          once: true
        });
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.setAttribute('data-google-identity', 'true');
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('No s’ha pogut carregar Google Identity Services.'));
      document.head.appendChild(script);
    });

    return this.googleScriptPromise;
  }

  private async handleGoogleCredentialAsync(idToken: string): Promise<void> {
    const result = await this.authService.loginWithGoogle(idToken);

    if (!result.ok) {
      this.notifyUser(
        'Google no disponible',
        'Configura un Google Client ID vàlid i verifica la federació per activar aquest accés.',
        'error'
      );
      return;
    }

    const redirectTo = this.route.snapshot.queryParamMap.get('redirectTo');
    void this.router.navigateByUrl(redirectTo || this.authService.getPostLoginRoute());
  }

  private notifyUser(title: string, message: string, tone: NotificationTone): void {
    this.notifications.notify(title, message, tone);
  }

  private filterPreviewPlaces(filters: PlaceFilters): Place[] {
    const normalizedSearch = normalizeSearchQuery(filters.search);
    const cityFilter = (filters.city ?? '').trim();
    const typeFilter = (filters.type ?? '').trim().toLowerCase();

    return LOGIN_PREVIEW_PLACES.filter((place) => {
      const matchesSearch = placeMatchesFreeTextSearch(place, normalizedSearch);
      const placeCity = (place.city ?? '').trim();
      const matchesCity =
        !cityFilter || placeCity.localeCompare(cityFilter, 'und', { sensitivity: 'base' }) === 0;
      const matchesType = !typeFilter || place.type.toString().toLowerCase() === typeFilter;
      const matchesPet = this.matchesPreviewPet(place, filters.pet);

      return matchesSearch && matchesCity && matchesType && matchesPet;
    });
  }

  private matchesPreviewPet(place: Place, pet: PlaceFilters['pet']): boolean {
    if (pet === 'all') {
      return true;
    }

    if (pet === 'dogs') {
      return place.acceptsDogs;
    }

    if (pet === 'cats') {
      return place.acceptsCats;
    }

    return true;
  }
}

/** Public login preview uses curated fake data (no authenticated API load). */
const LOGIN_PREVIEW_PLACES = PLACES_FAKE;
