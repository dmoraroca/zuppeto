# Document tecnic (CA)

## 1. Introduccio

Aquest document descriu com esta construida **YepPet** a nivell tecnic.
En l'estat actual, el projecte es una web Angular 21 amb enfocament `mock-first`, arquitectura per `features`
i una primera base funcional de mapa amb `Leaflet` i `OpenStreetMap`.

Objectius:

- documentar l'arquitectura actual del frontend
- deixar traçabilitat de components, serveis i decisions tecniques
- explicar el model de dades actual
- descriure la base d'autenticacio fake i control d'acces
- descriure la implementacio del mapa
- deixar una base UML tecnica clara i mantenible

## 2. Esquema tecnic general

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[Usuari]</span> --&gt;|<span style="color:#fcd34d;">Navegador</span>| <span style="color:#c4b5fd;">W[YepPet Web Angular]</span>
  <span style="color:#c4b5fd;">W</span> --&gt;|<span style="color:#fca5a5;">Mock Services</span>| <span style="color:#86efac;">M[(Dades simulades)]</span>
  <span style="color:#c4b5fd;">W</span> --&gt;|<span style="color:#fcd34d;">Mapa</span>| <span style="color:#67e8f9;">MAP[Leaflet + OpenStreetMap]</span>

  <span style="color:#5eead4; font-weight:700;">subgraph</span> <span style="color:#f9a8d4;">FE[Frontend Angular]</span>
    <span style="color:#93c5fd;">C[core]</span>
    <span style="color:#93c5fd;">S[shared]</span>
    <span style="color:#93c5fd;">F[features]</span>
  <span style="color:#5eead4; font-weight:700;">end</span>

  <span style="color:#c4b5fd;">W</span> --&gt; <span style="color:#93c5fd;">F</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#93c5fd;">C</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#93c5fd;">S</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#86efac;">M</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#67e8f9;">MAP</span></code></pre>

Resum del diagrama:

- la web Angular es el punt d'entrada de l'usuari
- la logica es distribueix per `features`
- `core` conté peces globals i `shared` peces reutilitzables
- els serveis actuals treballen contra dades simulades
- el mapa es tracta com una capacitat transversal reutilitzable

## 3. Arquitectura aplicada

### 3.1 Principis

- arquitectura per `features`
- cada component dins la seva propia carpeta
- cada pagina dins la seva propia carpeta
- separacio clara entre `core`, `shared` i `features`
- reutilitzacio real abans que abstraccio prematura
- dades simulades mentre validem UX i estructura
- preparar UI i serveis per futura substitucio per API

### 3.2 Estructura base

```text
src/app
├── core/
│   ├── guards/
│   ├── interceptors/
│   └── layout/
│       └── components/
│           ├── site-header/
│           ├── site-footer/
│           └── error-notifications/
├── shared/
│   └── components/
│       ├── section-heading/
│       ├── generic-info-card/
│       ├── favorite-toggle-button/
│       └── ...
└── features/
    ├── auth/
    ├── home/
    ├── places/
    ├── favorites/
    ├── contact/
    └── permissions/
```

### 3.3 Components compartits consolidats

Els components compartits que ja considerem reutilitzables de veritat son:

- `app-section-heading`
- `app-generic-info-card`
- `app-favorite-toggle-button`
- `app-place-card`
- `app-place-map`
- `app-error-notifications`

Criteri de consolidacio:

- es reutilitzen a mes d'una `feature` o resolen una necessitat transversal real
- tenen una API prou estable per no dependre d'una sola pagina
- el valor compartit compensa mantenir-los fora de la `feature`

No consolidem de moment com a compartits:

- `home-hero-section`
- `trending-cities-section`
- `why-yeppet-section`
- `place-filters`
- `favorites-page`

Aquests continuen sent especifics de la seva `feature` fins que aparegui una necessitat clara de reutilitzacio.

## 4. UML tecnic

### 4.1 Components i relacions

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#c4b5fd;">HP[HomePage]</span>
  <span style="color:#c4b5fd;">HS[HomeHeroSection]</span>
  <span style="color:#c4b5fd;">TC[TrendingCitiesSection]</span>
  <span style="color:#c4b5fd;">WY[WhyYepPetSection]</span>

  <span style="color:#93c5fd;">PP[PlacesPage]</span>
  <span style="color:#93c5fd;">PFI[PlaceFilters]</span>
  <span style="color:#67e8f9;">PM[PlaceMap]</span>
  <span style="color:#93c5fd;">PC[PlaceCard]</span>

  <span style="color:#f9a8d4;">PD[PlaceDetailPage]</span>
  <span style="color:#f9a8d4;">FP[FavoritesPage]</span>

  <span style="color:#fcd34d;">PS[PlaceService]</span>
  <span style="color:#fcd34d;">FS[FavoritesService]</span>
  <span style="color:#86efac;">MOCK[(PLACES_FAKE)]</span>

  <span style="color:#c4b5fd;">HP</span> --&gt; <span style="color:#c4b5fd;">HS</span>
  <span style="color:#c4b5fd;">HP</span> --&gt; <span style="color:#c4b5fd;">TC</span>
  <span style="color:#c4b5fd;">HP</span> --&gt; <span style="color:#c4b5fd;">WY</span>

  <span style="color:#93c5fd;">PP</span> --&gt; <span style="color:#93c5fd;">PFI</span>
  <span style="color:#93c5fd;">PP</span> --&gt; <span style="color:#67e8f9;">PM</span>
  <span style="color:#93c5fd;">PP</span> --&gt; <span style="color:#93c5fd;">PC</span>
  <span style="color:#f9a8d4;">PD</span> --&gt; <span style="color:#67e8f9;">PM</span>
  <span style="color:#f9a8d4;">FP</span> --&gt; <span style="color:#93c5fd;">PC</span>

  <span style="color:#93c5fd;">PP</span> --&gt; <span style="color:#fcd34d;">PS</span>
  <span style="color:#f9a8d4;">PD</span> --&gt; <span style="color:#fcd34d;">PS</span>
  <span style="color:#f9a8d4;">FP</span> --&gt; <span style="color:#fcd34d;">PS</span>
  <span style="color:#93c5fd;">PP</span> --&gt; <span style="color:#fcd34d;">FS</span>
  <span style="color:#f9a8d4;">PD</span> --&gt; <span style="color:#fcd34d;">FS</span>
  <span style="color:#f9a8d4;">FP</span> --&gt; <span style="color:#fcd34d;">FS</span>

  <span style="color:#fcd34d;">PS</span> --&gt; <span style="color:#86efac;">MOCK</span></code></pre>

Resum del diagrama:

- mostra les pantalles principals i com es recolzen en components i serveis
- `PlacesPage` centralitza la cerca, filtres, mapa i llistat
- `PlaceDetailPage` i `FavoritesPage` reutilitzen peces centrals
- `PlaceService` treballa contra `PLACES_FAKE` i `FavoritesService` manté l'estat de favorits

### 4.2 Model de domini actual

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">classDiagram</span>
  <span style="color:#c4b5fd;">class</span> <span style="color:#93c5fd;">Place</span> {
    <span style="color:#fcd34d;">+string</span> id
    <span style="color:#fcd34d;">+string</span> name
    <span style="color:#fcd34d;">+string</span> city
    <span style="color:#fcd34d;">+string</span> country
    <span style="color:#fcd34d;">+PlaceType</span> type
    <span style="color:#fcd34d;">+string</span> shortDescription
    <span style="color:#fcd34d;">+string</span> description
    <span style="color:#fcd34d;">+string</span> imageUrl
    <span style="color:#fcd34d;">+boolean</span> acceptsDogs
    <span style="color:#fcd34d;">+boolean</span> acceptsCats
    <span style="color:#fcd34d;">+number</span> rating
    <span style="color:#fcd34d;">+string[]</span> tags
    <span style="color:#fcd34d;">+string</span> address
    <span style="color:#fcd34d;">+string</span> petNotes
    <span style="color:#fcd34d;">+string[]</span> features
    <span style="color:#fcd34d;">+PlaceCoordinates</span> coordinates
  }

  <span style="color:#c4b5fd;">class</span> <span style="color:#67e8f9;">PlaceCoordinates</span> {
    <span style="color:#fcd34d;">+number</span> lat
    <span style="color:#fcd34d;">+number</span> lng
  }

  <span style="color:#c4b5fd;">class</span> <span style="color:#86efac;">PlaceFilters</span> {
    <span style="color:#fcd34d;">+string</span> search
    <span style="color:#fcd34d;">+string</span> city
    <span style="color:#fcd34d;">+string</span> type
    <span style="color:#fcd34d;">+PetFilter</span> pet
  }

  <span style="color:#93c5fd;">Place</span> --&gt; <span style="color:#67e8f9;">PlaceCoordinates</span></code></pre>

Resum del diagrama:

- `Place` es el model central del frontend
- aquest model ja cobreix llistat, detall, favorits i mapa
- `PlaceCoordinates` permet representar el lloc sobre el mapa
- `PlaceFilters` defineix el contracte actual de filtratge
- els mocks actuals ja inclouen context de barri, ressenyes, preu i política pet

### 4.3 UML del mapa

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">PlacesPage</span> --&gt; <span style="color:#67e8f9;">PlaceMapComponent</span>
  <span style="color:#f9a8d4;">PlaceDetailPage</span> --&gt; <span style="color:#67e8f9;">PlaceMapComponent</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#fcd34d;">Leaflet</span>
  <span style="color:#fcd34d;">Leaflet</span> --&gt; <span style="color:#86efac;">OpenStreetMap tiles</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#c4b5fd;">Place.coordinates</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#fca5a5;">placeSelected</span></code></pre>

Resum del diagrama:

- `PlaceMapComponent` es la peça central del mapa
- el mateix component es reutilitza a `places` i al detall
- el component pinta el mapa amb `Leaflet` i carrega tiles d'OpenStreetMap
- les coordenades surten directament de `Place.coordinates`
- en clicar un marcador, el component emet `placeSelected`

### 4.4 UML d'autenticacio i control d'acces

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">LOGIN[LoginPage]</span> --&gt; <span style="color:#c4b5fd;">AUTH[AuthService]</span>
  <span style="color:#c4b5fd;">AUTH</span> --&gt; <span style="color:#86efac;">MOCK[(AUTH_USERS_FAKE)]</span>
  <span style="color:#c4b5fd;">AUTH</span> --&gt; <span style="color:#67e8f9;">LS[localStorage]</span>
  <span style="color:#f9a8d4;">ROUTES[app.routes]</span> --&gt; <span style="color:#fcd34d;">AG[authGuard]</span>
  <span style="color:#f9a8d4;">ROUTES</span> --&gt; <span style="color:#fcd34d;">GG[guestGuard]</span>
  <span style="color:#f9a8d4;">ROUTES</span> --&gt; <span style="color:#fcd34d;">ADG[adminGuard]</span>
  <span style="color:#fcd34d;">AG</span> --&gt; <span style="color:#c4b5fd;">AUTH</span>
  <span style="color:#fcd34d;">GG</span> --&gt; <span style="color:#c4b5fd;">AUTH</span>
  <span style="color:#fcd34d;">ADG</span> --&gt; <span style="color:#c4b5fd;">AUTH</span>
  <span style="color:#86efac;">PROFILE[ProfilePage]</span> --&gt; <span style="color:#c4b5fd;">AUTH</span>
  <span style="color:#93c5fd;">HEADER[SiteHeader]</span> --&gt; <span style="color:#c4b5fd;">AUTH</span></code></pre>

Resum del diagrama:

- `AuthService` centralitza la sessio fake, el rol actual i l'actualitzacio de perfil
- `authGuard`, `guestGuard` i `adminGuard` governen l'acces a les rutes
- la sessio es manté a `localStorage` per simular persistencia bàsica
- `LoginPage`, `ProfilePage` i `SiteHeader` consumeixen el mateix estat d'autenticacio

## 5. Features actuals

### 5.1 Home

Peces principals:

- `home-hero-section`
- `trending-cities-section`
- `why-yeppet-section`

Decisions tecniques rellevants:

- la pagina es construeix a partir de dades fake tipades
- el `hero` encapsula navegacio cap a `places`
- els blocs grans de la `home` viuen en components separats

### 5.2 Places

Peces principals:

- `place-filters`
- `place-map`
- `place-card`
- `places-page`
- `place-detail-page`

Decisions tecniques rellevants:

- `places-page` centralitza query params, filtres actius i resultats
- `place-map` es reutilitzable i parametritzable
- `place-card` es reutilitza a llistat i favorits

### 5.3 Favorites

Peces principals:

- `favorites-page`
- `favorite-toggle-button`
- `favorites.service`

Decisions tecniques rellevants:

- l'estat es manté local i simulat
- `favorites-page` afegeix una capa local de revisio amb cerca, filtres i ordenacio sense tocar el model base
- el guardat mes recent queda al davant i actua com a punt de reentrada rapida
- el flux es pot substituir despres per persistencia real

### 5.4 Auth

Peces principals:

- `login-page`
- `profile-page`
- `auth.service`
- `authGuard`
- `guestGuard`
- `adminGuard`

Decisions tecniques rellevants:

- la sessio actual es fake i es manté a `localStorage`
- el login treballa contra `AUTH_USERS_FAKE`
- `USER` i `ADMIN` comparteixen base de sessio però divergeixen en permisos i rutes
- `Perfil` funciona com a pantalla de manteniment fake abans de connectar backend real

## 6. Serveis i dades simulades

### 6.1 PlaceService

`PlaceService` es el servei principal de la feature `places`.

Responsabilitats:

- obtenir llistat de llocs
- obtenir detall per `id`
- aplicar filtres
- exposar ciutats disponibles
- exposar tipus disponibles
- construir etiquetes de tipus
- filtrar també per context textual enriquit com el barri

Font de dades:

- `PLACES_FAKE`

Preparacio per API:

- `PlaceService` ja no depen directament del mock
- consumeix un port injectable (`PLACE_SOURCE`)
- el mock actual entra per `MockPlaceSourceService`

### 6.2 FavoritesService

Responsabilitats:

- mantenir estat fake de favorits
- saber si un lloc esta guardat
- afegir i treure favorits
- mantenir l'ordre de recencia dels llocs guardats

Notes tecniques:

- els `id` es persisteixen a `localStorage`
- en afegir un favorit, aquest puja al primer lloc de la llista
- `favorites-page` construeix la revisio local a partir del conjunt de favorits recuperat per `PlaceService`
- `FavoritesService` treballa contra un port injectable (`FAVORITES_STORE`)
- el mock actual entra per `MockFavoritesStoreService`

### 6.3 AuthService

Responsabilitats:

- validar credencials fake
- obrir i tancar sessio
- exposar usuari actual, rol i estat autenticat
- decidir la ruta per defecte despres del login
- actualitzar el perfil fake de l'usuari actiu

Fonts de dades:

- `AUTH_USERS_FAKE`
- `localStorage`

Notes tecniques:

- `login` valida `email` i `password` contra usuaris simulats
- `logout` esborra la sessio local
- `updateProfile` actualitza l'usuari actual i persisteix l'estat simulat
- `AuthService` ja no depen directament de `AUTH_USERS_FAKE` ni de `localStorage`
- treballa contra un port injectable (`AUTH_STORE`)
- el mock actual entra per `MockAuthStoreService`

## 7. Responsive fi de pantalles

En aquesta iteracio s'ha fet un repàs de responsive fi sense canviar l'arquitectura de pantalles.

Cobertura principal:

- `site-header`
- `site-footer`
- `login-page`
- `profile-page`
- `contact-page`
- `places-page`
- `place-detail-page`
- `favorites-page`
- `place-card`
- `place-filters`

Criteri aplicat:

- evitar que accions i navegacio quedin massa estretes en mobil
- forcar amplada completa en botons i grups d'accions quan la columna cau a una sola peça
- reduir paddings i radis en pantalles estretes
- evitar que targetes i mètriques depenguin d'una composicio de desktop

## 8. Implementacio de l'autenticacio fake

### 8.1 Usuaris mock

Els accessos de prova actuals son:

```text
ADMIN
email: admin@admin.adm
password: Admin123

USER
email: user@user.com
password: Admin123
```

Ubicacio:

- `src/Web/src/app/features/auth/mock/auth-users.fake.ts`

### 8.2 Persistencia de sessio

La sessio fake es guarda a `localStorage` amb una clau fixa:

```ts
const STORAGE_KEY = 'yeppet-auth-user';
```

Comportament:

- si hi ha usuari guardat, l'app restaura la sessio en carregar
- si no hi ha sessio, les rutes protegides redirigeixen a `login`
- en `logout`, la clau s'elimina

### 8.3 Guards de navegacio

L'app aplica tres guards:

```text
- authGuard
- guestGuard
- adminGuard
```

Responsabilitats:

- `authGuard`: protegeix rutes que requereixen sessio
- `guestGuard`: evita entrar a `login` si ja hi ha sessio
- `adminGuard`: restringeix `permissions` a rol `ADMIN`

### 8.4 Flux de redireccio

Quan una ruta protegida es demana sense sessio, el sistema construeix:

```text
/login?redirectTo=/ruta-original
```

Despres del login:

- si existeix `redirectTo`, s'usa aquesta ruta
- si no existeix:
  - `USER` va a `/perfil`
  - `ADMIN` va a `/permissions`

### 8.5 Perfil i consentiment

La pagina `Perfil` permet mantenir:

- nom
- ciutat
- pais
- bio
- foto de perfil opcional
- consentiment de manteniment de dades

Regles actuals:

- si no hi ha foto, es mostra un placeholder `NONE`
- `USER` ha d'acceptar el consentiment per poder guardar
- `ADMIN` queda exempt segons el criteri funcional actual

### 8.6 Punts pendents

La base actual prepara pero no implementa encara:

- autenticacio real contra API
- refresh tokens o expiracio real de sessio
- recuperacio real de contrasenya
- login social
- persistencia real de perfil i favorits per usuari
## 7. Implementacio del mapa

### 7.1 Llibreries utilitzades

Es van instal·lar aquestes dependencies:

```bash
cd src/Web
npm install leaflet @types/leaflet
```

Motiu:

- `leaflet` aporta el motor del mapa
- `@types/leaflet` aporta tipus TypeScript

### 7.2 Estils globals del mapa

Es va importar l'estil de Leaflet a nivell global:

```scss
@import 'leaflet/dist/leaflet.css';
```

Ubicacio:

- `src/Web/src/styles.scss`

### 7.3 Extensio del model `Place`

El model de `Place` es va ampliar per incloure coordenades i context fake mes creible:

```ts
export interface PlaceCoordinates {
  lat: number;
  lng: number;
}

export interface Place {
  id: string;
  name: string;
  city: string;
  country: string;
  neighborhood: string;
  type: PlaceType;
  shortDescription: string;
  description: string;
  imageUrl: string;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  rating: number;
  reviewCount: number;
  priceLabel: string;
  petPolicyLabel: string;
  tags: string[];
  address: string;
  petNotes: string;
  features: string[];
  coordinates: PlaceCoordinates;
}
```

Ubicacio:

- `src/Web/src/app/features/places/models/place.model.ts`

### 7.4 Coordenades als mocks

Cada `Place` fake incorpora coordenades precises i mes context funcional:

```ts
coordinates: {
  lat: 41.390205,
  lng: 2.191987
}
```

També hi afegim:

- `neighborhood`
- `reviewCount`
- `priceLabel`
- `petPolicyLabel`

Decisio:

- les coordenades es controlen manualment
- no depenem de geocoding extern en aquesta fase
- la precisio es suficient per veure comportament realista

Ubicacio:

- `src/Web/src/app/features/places/mock/places.fake.ts`

### 7.5 Component centralitzat `app-place-map`

El mapa no s'ha implementat directament dins les pagines.
S'ha encapsulat en un component reutilitzable.

Inputs:

- `places`
- `selectedPlaceId`
- `height`
- `emptyTitle`
- `emptyCopy`

Output:

- `placeSelected`

Fragment simplificat:

```ts
readonly places = input.required<Place[]>();
readonly selectedPlaceId = input<string | null>(null);
readonly height = input('24rem');
readonly emptyTitle = input('No hi ha ubicacions per mostrar');
readonly emptyCopy = input('Ajusta els filtres per veure llocs al mapa.');
readonly placeSelected = output<string>();
```

Ubicacio:

- `src/Web/src/app/features/places/components/place-map/place-map.component.ts`

### 7.6 Carrega lazy de Leaflet

Leaflet es carrega de forma lazy quan el component necessita pintar el mapa:

```ts
private async ensureMap(): Promise<void> {
  if (this.map || !this.mapContainer?.nativeElement || !this.hasPlaces) {
    return;
  }

  this.leaflet = await import('leaflet');
  this.map = this.leaflet.map(this.mapContainer.nativeElement, {
    zoomControl: true,
    scrollWheelZoom: false
  });
}
```

Motiu:

- evitar carregar la llibreria massa aviat
- no inicialitzar mapa si no hi ha resultats
- mantenir el component mes eficient

### 7.7 Tiles d'OpenStreetMap

El mapa es pinta amb un `tileLayer` public d'OpenStreetMap:

```ts
this.leaflet
  .tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors'
  })
  .addTo(this.map);
```

### 7.8 Marcadors i seleccio

Cada `Place` es transforma en un `circleMarker`:

```ts
const marker = this.leaflet.circleMarker([place.coordinates.lat, place.coordinates.lng], {
  radius: isSelected ? 11 : 8,
  weight: isSelected ? 3 : 2,
  color: isSelected ? '#065f46' : '#0f766e',
  fillColor: isSelected ? '#2dd4bf' : '#99f6e4',
  fillOpacity: isSelected ? 0.95 : 0.85
});
```

Quan es clica un marcador:

```ts
marker.on('click', () => this.placeSelected.emit(place.id));
```

### 7.9 Ajust de vista

Si hi ha un lloc seleccionat:

```ts
this.map.setView([selectedPlace.coordinates.lat, selectedPlace.coordinates.lng], 15);
```

Si no, el mapa s'ajusta al conjunt de resultats:

```ts
this.map.fitBounds(bounds, {
  padding: [28, 28],
  maxZoom: this.places().length === 1 ? 15 : 13
});
```

### 7.10 Reutilitzacio a les pantalles

A `places`:

```html
<app-place-map
  [places]="places()"
  height="25rem"
  emptyTitle="Cap resultat al mapa"
  emptyCopy="Quan els filtres retornin llocs, també els veuràs aquí."
  (placeSelected)="openPlaceFromMap($event)"
/>
```

A `place detail`:

```html
<app-place-map
  [places]="placeAsArray"
  [selectedPlaceId]="selectedPlace.id"
  height="20rem"
  emptyTitle="No hi ha ubicacio disponible"
  emptyCopy="Aquest lloc no te coordenades per mostrar al mapa."
/>
```

### 7.11 Estat buit del component

Quan no hi ha llocs, el component no inicialitza el mapa i mostra un bloc buit controlat:

```html
@if (hasPlaces) {
  <div class="place-map__canvas" #mapContainer [style.height]="height()"></div>
} @else {
  <div class="place-map__empty">
    <h3>{{ emptyTitle() }}</h3>
    <p>{{ emptyCopy() }}</p>
  </div>
}
```

### 7.12 Fitxers implicats

```text
src/Web/package.json
src/Web/src/styles.scss
src/Web/src/app/features/places/models/place.model.ts
src/Web/src/app/features/places/mock/places.fake.ts
src/Web/src/app/features/places/components/place-map/place-map.component.ts
src/Web/src/app/features/places/components/place-map/place-map.component.html
src/Web/src/app/features/places/components/place-map/place-map.component.scss
src/Web/src/app/features/places/pages/places-page/places-page.component.html
src/Web/src/app/features/places/pages/place-detail-page/place-detail-page.component.html
```

## 8. Decisions actuals

- el mapa viu a `places`, no a la portada
- el mapa es tracta com una part funcional de cerca
- el component ha de ser parametritzable
- els llocs tenen coordenades simulades precises
- la `home` no concentra la logica de resultats

## 9. Capa base d'errors

La fase II ja incorpora una base comuna per gestionar errors sense repetir logica a cada pantalla.

Peces principals:

- `errorInterceptor`
- `ErrorNotificationsService`
- `app-error-notifications`

### 9.1 UML de la capa d'errors

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">HTTP[HttpClient request]</span> --&gt; <span style="color:#fca5a5;">INT[errorInterceptor]</span>
  <span style="color:#fca5a5;">INT</span> --&gt; <span style="color:#fcd34d;">SRV[ErrorNotificationsService]</span>
  <span style="color:#fcd34d;">SRV</span> --&gt; <span style="color:#67e8f9;">UI[app-error-notifications]</span>
  <span style="color:#67e8f9;">UI</span> --&gt; <span style="color:#c4b5fd;">USR[Usuari]</span></code></pre>

Resum del diagrama:

- qualsevol peticio HTTP pot passar per l'interceptor
- quan hi ha error, l'interceptor delega el missatge al servei central
- el servei manté l'estat de notificacions
- la UI global les pinta sense que cada pagina hagi d'implementar la mateixa logica

### 9.2 Interceptor HTTP

L'interceptor es registra globalment a `app.config.ts` amb `provideHttpClient`:

```ts
provideHttpClient(withInterceptors([errorInterceptor]))
```

Responsabilitat:

- capturar errors HTTP
- delegar el tractament d'usuari al servei central
- reemetre l'error per no amagar-lo a cap capa superior

Ubicacio:

- `src/Web/src/app/core/interceptors/error.interceptor.ts`

### 9.3 Servei central de notificacions

`ErrorNotificationsService` manté una llista reactiva de notificacions mitjançant `signal`.

Responsabilitats:

- traduir `HttpErrorResponse` a missatges entenedors
- generar notificacions uniformes
- tancar-les manualment o automaticament

Alguns casos coberts:

- sense connexio
- `401`
- `403`
- `404`
- errors `500+`
- error inesperat generic

Ubicacio:

- `src/Web/src/app/core/services/error-notifications.service.ts`

### 9.4 UI global d'errors

La UI global es munta a nivell d'`app` i no depen de cap pagina concreta:

```html
<app-error-notifications />
<router-outlet />
```

Aixo permet:

- veure errors des de qualsevol pantalla
- no repetir banners o toasts a `home`, `places` o `detail`
- preparar l'entrada a backend real amb una estrategia comuna

Fitxers implicats:

- `src/Web/src/app/app.config.ts`
- `src/Web/src/app/app.ts`
- `src/Web/src/app/app.html`
- `src/Web/src/app/core/interceptors/error.interceptor.ts`
- `src/Web/src/app/core/services/error-notifications.service.ts`
- `src/Web/src/app/core/layout/components/error-notifications/error-notifications.component.ts`
- `src/Web/src/app/core/layout/components/error-notifications/error-notifications.component.html`
- `src/Web/src/app/core/layout/components/error-notifications/error-notifications.component.scss`

## 10. Punts pendents de refinament

- millor UX de marcadors
- popups mes bons
- mes criteri quan hi hagi moltes dades
- mode mixt ja fixat: mapa sota filtres i llistat sincronitzat com a patró estable de `places`

## 11. Referencia documental

Document funcional:

- [`funcional-ca.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/ca/funcional-ca.md)

Document de fases:

- [`project-phases.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/project-phases.md)
