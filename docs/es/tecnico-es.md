# Documento tecnico (ES)

> Estado actual:
> esta version se ha generado a partir del documento tecnico en catalan y queda pendiente de revision y traduccion fina al espanol.

## 1. Introduccio

Aquest document descriu l'estat tecnic actual de **Zuppeto** i com s'esta construint a nivell de frontend.
Ara mateix el projecte treballa amb enfocament `mock-first`, Angular 21 i una arquitectura per `features`.

Objectius:

- explicar l'arquitectura actual de la web
- deixar traçabilitat de les decisions tecniques preses
- documentar l'estat real de la fase I i l'inici de la fase II
- definir el model funcional actual de `places`, `favorites` i `home`
- deixar una base UML clara, similar a la documentacio d'`Escoles Publiques`

## 2. Esquema general de l'app

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[Usuari]</span> --&gt;|<span style="color:#fcd34d;">Navegador</span>| <span style="color:#c4b5fd;">W[Zuppeto Web Angular]</span>
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

- representa l'esquema general actual de Zuppeto a nivell de frontend
- la web Angular delega la logica funcional a les `features`
- les `features` es recolzen en `core`, `shared`, dades simulades i la capa de mapa
- no hi ha backend real encara; el sistema actual treballa sobre mocks i integracio de mapa al client

Flux principal actual:

1. l'usuari entra a la `home`
2. navega cap a `places` des del menu, CTA, ciutats o chips
3. `places` aplica filtres sobre dades simulades
4. els resultats es pinten en llistat i al mapa
5. l'usuari pot obrir el detall d'un lloc o guardar-lo com a favorit

## 3. Estat actual del projecte

Situacio actual:

- fase I tancada
- fase II iniciada
- frontend Angular 21 a `src/Web`
- sense backend real encara
- dades simulades estructurades per models i serveis
- mapa ja integrat com a component reutilitzable

Pantalles actuals:

- `home`
- `places`
- `place detail`
- `favorites`
- `contacte`
- `permissions`

## 4. Arquitectura aplicada

### 4.1 Principis

- arquitectura per `features`
- cada component dins la seva propia carpeta
- cada pagina dins la seva propia carpeta
- separacio clara entre `core`, `shared` i `features`
- reutilitzacio real abans que abstraccio prematura
- dades simulades mentre validem UX i estructura
- preparar UI i serveis per futura substitucio per API

### 4.2 Estructura base

```text
src/app
├── core/
│   └── layout/
│       └── components/
│           ├── site-header/
│           └── site-footer/
├── shared/
│   └── components/
│       ├── section-heading/
│       ├── generic-info-card/
│       ├── favorite-toggle-button/
│       └── ...
└── features/
    ├── home/
    ├── places/
    ├── favorites/
    ├── contact/
    └── permissions/
```

### 4.3 Components compartits consolidats

Els components compartits que ja considerem reutilitzables de veritat son:

- `app-section-heading`
- `app-generic-info-card`
- `app-favorite-toggle-button`
- `app-place-card`
- `app-place-map`

## 5. UML funcional

### 5.1 Casos d'us principals

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">U[Usuari public]</span>

  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">H[Veure portada]</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">P[Cercar llocs]</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">F[Guardar favorits]</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">D[Veure detall d'un lloc]</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">C[Contactar]</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">A[Consultar ajuda]</span>

  <span style="color:#c4b5fd;">P</span> --&gt; <span style="color:#86efac;">PF[Aplicar filtres]</span>
  <span style="color:#c4b5fd;">P</span> --&gt; <span style="color:#67e8f9;">PM[Veure mapa]</span>
  <span style="color:#c4b5fd;">P</span> --&gt; <span style="color:#fcd34d;">PC[Entrar per ciutat o chip]</span>
  <span style="color:#c4b5fd;">D</span> --&gt; <span style="color:#f9a8d4;">DF[Guardar com a favorit]</span></code></pre>

Resum del diagrama:

- mostra els casos d'us principals de la web en l'estat actual
- l'usuari public pot explorar la portada, cercar llocs, veure detall, guardar favorits i consultar ajuda o contacte
- el nucli funcional actual gira al voltant de `places`, filtres, mapa i favorits
- aquest diagrama descriu el comportament visible del producte, no l'arquitectura interna

### 5.2 Components i relacions

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

- mostra com es relacionen les pantalles principals, els components clau i els serveis actuals
- `PlacesPage` es recolza en filtres, mapa i cards per construir la cerca
- `PlaceDetailPage` i `FavoritesPage` reutilitzen peces centrals en lloc de duplicar implementacions
- `PlaceService` consumeix `PLACES_FAKE`, mentre `FavoritesService` governa l'estat fake de favorits

### 5.3 Model de domini actual

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

- descriu el model de domini actual que ja fa servir la web per treballar amb llocs
- `Place` es la peça central i concentra la informacio necessaria per llistat, detall, favorits i mapa
- `PlaceCoordinates` permet situar els llocs al mapa amb coordenades simulades precises
- `PlaceFilters` defineix la forma actual de filtrar els resultats a la pagina `places`

## 6. Features actuals

### 6.1 Home

Responsabilitats actuals:

- presentar la proposta de valor
- oferir navegacio cap a `places`
- mostrar ciutats destacades
- mostrar chips navegables
- previsualitzar contingut destacat del producte

Peces principals:

- `home-hero-section`
- `trending-cities-section`
- `why-yeppet-section`

### 6.2 Places

Responsabilitats actuals:

- filtrar resultats per ciutat, tipus, mascota i cerca
- mostrar resultats en llistat
- mostrar resultats al mapa
- mostrar filtres actius
- permetre navegar al detall d'un lloc

Peces principals:

- `place-filters`
- `place-map`
- `place-card`
- `places-page`
- `place-detail-page`

### 6.3 Favorites

Responsabilitats actuals:

- guardar i treure llocs de favorits
- mostrar el llistat de favorits fake
- reutilitzar el mateix `place-card`

## 7. Serveis i dades simulades

### 7.1 PlaceService

`PlaceService` es el servei principal de la feature `places`.

Responsabilitats:

- obtenir llistat de llocs
- obtenir detall per `id`
- aplicar filtres
- exposar ciutats disponibles
- exposar tipus disponibles
- construir etiquetes de tipus

Font de dades:

- `PLACES_FAKE`

### 7.2 FavoritesService

Responsabilitats:

- mantenir estat fake de favorits
- saber si un lloc esta guardat
- afegir i treure favorits

## 8. Mapa

### 8.1 Implementacio actual

La implementacio actual del mapa es basa en:

- `Leaflet`
- dades d'OpenStreetMap
- `app-place-map` com a component centralitzat
- alimentacio per parametres
- reutilitzacio en `places` i `place detail`

### 8.2 UML del mapa

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">PlacesPage</span> --&gt; <span style="color:#67e8f9;">PlaceMapComponent</span>
  <span style="color:#f9a8d4;">PlaceDetailPage</span> --&gt; <span style="color:#67e8f9;">PlaceMapComponent</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#fcd34d;">Leaflet</span>
  <span style="color:#fcd34d;">Leaflet</span> --&gt; <span style="color:#86efac;">OpenStreetMap tiles</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#c4b5fd;">Place.coordinates</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#fca5a5;">placeSelected</span></code></pre>

Resum del diagrama:

- `PlaceMapComponent` es la peça central del mapa
- tant `PlacesPage` com `PlaceDetailPage` reutilitzen el mateix component
- el component pinta el mapa amb `Leaflet` i fa servir tiles d'OpenStreetMap
- la posicio dels marcadors surt de `Place.coordinates`
- quan es clica un marcador, el component emet `placeSelected`

### 8.3 Passos seguits per muntar el mapa

#### Pas 1. Afegir les llibreries

Es van instal·lar aquestes dependències al projecte Angular:

```bash
cd src/Web
npm install leaflet @types/leaflet
```

Motiu:

- `leaflet` aporta el motor del mapa
- `@types/leaflet` aporta tipus TypeScript per treballar bé al component

#### Pas 2. Importar els estils globals de Leaflet

Es va importar l'estil de Leaflet a nivell global perquè el mapa i els controls es pintessin correctament:

```scss
@import 'leaflet/dist/leaflet.css';
```

Ubicacio actual:

- `src/Web/src/styles.scss`

#### Pas 3. Estendre el model `Place`

Per poder mostrar llocs al mapa, es va ampliar el model de domini de frontend amb coordenades.

Fragment actual:

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
  type: PlaceType;
  shortDescription: string;
  description: string;
  imageUrl: string;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  rating: number;
  tags: string[];
  address: string;
  petNotes: string;
  features: string[];
  coordinates: PlaceCoordinates;
}
```

Ubicacio actual:

- `src/Web/src/app/features/places/models/place.model.ts`

#### Pas 4. Afegir coordenades precises als mocks

Cada `Place` fake incorpora coordenades `lat` i `lng`.

Exemple actual:

```ts
coordinates: {
  lat: 41.390205,
  lng: 2.191987
}
```

Decisio:

- les coordenades s'han posat amb decimals suficients per donar una posicio precisa
- el sistema no depèn d'un geocoder extern en aquesta fase
- els punts es controlen manualment per mantenir el mock estable

Ubicacio actual:

- `src/Web/src/app/features/places/mock/places.fake.ts`

#### Pas 5. Crear un component centralitzat i parametritzable

El mapa no s'ha muntat directament dins `places-page` ni dins `place-detail-page`.
S'ha encapsulat a un component reutilitzable:

- `app-place-map`

Inputs principals:

- `places`
- `selectedPlaceId`
- `height`
- `emptyTitle`
- `emptyCopy`

Output principal:

- `placeSelected`

Definicio actual simplificada:

```ts
readonly places = input.required<Place[]>();
readonly selectedPlaceId = input<string | null>(null);
readonly height = input('24rem');
readonly emptyTitle = input('No hi ha ubicacions per mostrar');
readonly emptyCopy = input('Ajusta els filtres per veure llocs al mapa.');
readonly placeSelected = output<string>();
```

Ubicacio actual:

- `src/Web/src/app/features/places/components/place-map/place-map.component.ts`

#### Pas 6. Inicialitzar Leaflet de manera lazy

Leaflet no es carrega abans d'hora. El component fa `import('leaflet')` quan el mapa realment es necessita.

Fragment actual:

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

- reduir càrrega inicial
- evitar inicialitzar mapa si no hi ha llocs a mostrar
- mantenir el component reutilitzable i més eficient

#### Pas 7. Connectar els tiles d'OpenStreetMap

El component pinta el mapa amb un `tileLayer` públic d'OpenStreetMap:

```ts
this.leaflet
  .tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors'
  })
  .addTo(this.map);
```

Motiu:

- per fase actual no cal Google Maps
- no cal billing per validar UX
- és suficient per mostrar llocs, ciutat i context geogràfic

#### Pas 8. Pintar marcadors a partir dels llocs filtrats

Cada `Place` es converteix en un `circleMarker`.

Fragment actual:

```ts
const marker = this.leaflet.circleMarker([place.coordinates.lat, place.coordinates.lng], {
  radius: isSelected ? 11 : 8,
  weight: isSelected ? 3 : 2,
  color: isSelected ? '#065f46' : '#0f766e',
  fillColor: isSelected ? '#2dd4bf' : '#99f6e4',
  fillOpacity: isSelected ? 0.95 : 0.85
});
```

Decisio:

- s'han usat `circleMarker` en lloc d'icones clàssiques per mantenir una estètica més neta
- el marcador seleccionat es pinta diferent per millorar lectura visual

#### Pas 9. Ajustar la vista automàticament

El component calcula `bounds` i ajusta el mapa segons els resultats:

```ts
this.map.fitBounds(bounds, {
  padding: [28, 28],
  maxZoom: this.places().length === 1 ? 15 : 13
});
```

Si hi ha un `selectedPlaceId`, el component centra directament aquell lloc:

```ts
this.map.setView([selectedPlace.coordinates.lat, selectedPlace.coordinates.lng], 15);
```

Motiu:

- a `places` el mapa s'adapta al conjunt de resultats
- al detall, el mapa ha d'obrir centrat en el lloc seleccionat

#### Pas 10. Reutilitzar el component a les pantalles

##### A `places`

El component es passa la llista filtrada de llocs:

```html
<app-place-map
  [places]="places()"
  height="25rem"
  emptyTitle="Cap resultat al mapa"
  emptyCopy="Quan els filtres retornin llocs, també els veuràs aquí."
  (placeSelected)="openPlaceFromMap($event)"
/>
```

##### A `place detail`

El mateix component es reutilitza, però amb un sol lloc i `selectedPlaceId`:

```html
<app-place-map
  [places]="placeAsArray"
  [selectedPlaceId]="selectedPlace.id"
  height="20rem"
  emptyTitle="No hi ha ubicació disponible"
  emptyCopy="Aquest lloc no té coordenades per mostrar al mapa."
/>
```

Benefici:

- una sola implementacio
- dues pantalles diferents
- comportament consistent

#### Pas 11. Gestionar l'estat buit

El component no intenta inicialitzar Leaflet si no hi ha llocs. En aquest cas mostra un bloc buit controlat:

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

Motiu:

- evitar errors innecessaris
- donar feedback clar a l'usuari
- mantenir el component segur també quan els filtres no retornen dades

### 8.4 Fitxers implicats en la implementacio del mapa

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

### 8.5 Decisions actuals

- el mapa viu a `places`, no a la portada
- el mapa es tracta com una part funcional de cerca
- el component ha de ser parametritzable
- els llocs tenen coordenades simulades precises

### 8.6 Punts pendents de refinament

- millor UX de marcadors
- popups mes bons
- mes criteri quan hi hagi moltes dades
- possible mode `llista / mapa / mixt`

## 9. Fase I tancada

La fase I queda tancada amb:

- `home` funcional
- `places` funcional
- `place detail` funcional
- `favorites` funcional
- `contacte` i `permissions`
- mapa base funcional
- navegacio real amb dades simulades

## 10. Proper pas tecnic

La feina activa ara mateix correspon a la fase II:

- polir la UX de `places`
- refinar el mapa ja integrat
- millorar el `place detail`
- polir `favorites`
- enriquir els mocks
- introduir capa base de gestio d'errors
- afegir interceptor global per errors HTTP

## 11. Referencia de fases

La planificacio detallada i l'estat viu de les fases es manté a:

- [`project-phases.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/project-phases.md)
