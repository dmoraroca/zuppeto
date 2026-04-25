# Technisches Dokument (DE)

## 1. Einleitung

Dieses Dokument beschreibt, wie **Zuppeto** technisch aufgebaut ist.
Im aktuellen Stand ist das Projekt eine Angular-21-Webanwendung mit `mock-first`-Ansatz, feature-basierter Architektur
und einer ersten funktionalen Kartenbasis mit `Leaflet` und `OpenStreetMap`.

Ziele:

- die aktuelle Frontend-Architektur dokumentieren
- Rueckverfolgbarkeit von Komponenten, Services und technischen Entscheidungen hinterlassen
- das aktuelle Datenmodell erklaeren
- die Implementierung der Karte beschreiben
- eine klare und wartbare technische UML-Basis hinterlassen

## 2. Technisches Gesamtschema

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[Benutzer]</span> --&gt;|<span style="color:#fcd34d;">Browser</span>| <span style="color:#c4b5fd;">W[Zuppeto Web Angular]</span>
  <span style="color:#c4b5fd;">W</span> --&gt;|<span style="color:#fca5a5;">Mock Services</span>| <span style="color:#86efac;">M[(Simulierte Daten)]</span>
  <span style="color:#c4b5fd;">W</span> --&gt;|<span style="color:#fcd34d;">Karte</span>| <span style="color:#67e8f9;">MAP[Leaflet + OpenStreetMap]</span>

  <span style="color:#5eead4; font-weight:700;">subgraph</span> <span style="color:#f9a8d4;">FE[Angular Frontend]</span>
    <span style="color:#93c5fd;">C[core]</span>
    <span style="color:#93c5fd;">S[shared]</span>
    <span style="color:#93c5fd;">F[features]</span>
  <span style="color:#5eead4; font-weight:700;">end</span>

  <span style="color:#c4b5fd;">W</span> --&gt; <span style="color:#93c5fd;">F</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#93c5fd;">C</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#93c5fd;">S</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#86efac;">M</span>
  <span style="color:#93c5fd;">F</span> --&gt; <span style="color:#67e8f9;">MAP</span></code></pre>

Zusammenfassung des Diagramms:

- die Angular-Webanwendung ist der Einstiegspunkt fuer den Benutzer
- die Logik ist ueber `features` verteilt
- `core` enthaelt globale Bausteine und `shared` wiederverwendbare Teile
- die aktuellen Services arbeiten gegen simulierte Daten
- die Karte wird als querliegende, wiederverwendbare Faehigkeit behandelt

## 3. Angewandte Architektur

### 3.1 Prinzipien

- feature-basierte Architektur
- jede Komponente in ihrem eigenen Ordner
- jede Seite in ihrem eigenen Ordner
- klare Trennung zwischen `core`, `shared` und `features`
- echte Wiederverwendung vor verfruehter Abstraktion
- simulierte Daten, waehrend UX und Struktur validiert werden
- UI und Services fuer einen spaeteren Austausch durch API vorbereiten

### 3.2 Basisstruktur

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

### 3.3 Konsolidierte Shared Components

Die gemeinsam genutzten Komponenten, die bereits als wirklich wiederverwendbar gelten, sind:

- `app-section-heading`
- `app-generic-info-card`
- `app-favorite-toggle-button`
- `app-place-card`
- `app-place-map`

## 4. Technische UML

### 4.1 Komponenten und Beziehungen

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

Zusammenfassung des Diagramms:

- es zeigt die Hauptseiten und wie sie sich auf Komponenten und Services stuetzen
- `PlacesPage` zentralisiert Suche, Filter, Karte und Liste
- `PlaceDetailPage` und `FavoritesPage` verwenden zentrale Bausteine wieder
- `PlaceService` arbeitet gegen `PLACES_FAKE` und `FavoritesService` verwaltet den Favoritenstatus

### 4.2 Aktuelles Domaenenmodell

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

Zusammenfassung des Diagramms:

- `Place` ist das zentrale Frontend-Modell
- dieses Modell deckt bereits Liste, Detail, Favoriten und Karte ab
- `PlaceCoordinates` erlaubt die Darstellung des Ortes auf der Karte
- `PlaceFilters` definiert den aktuellen Vertrag fuer die Filterung

### 4.3 UML der Karte

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">PlacesPage</span> --&gt; <span style="color:#67e8f9;">PlaceMapComponent</span>
  <span style="color:#f9a8d4;">PlaceDetailPage</span> --&gt; <span style="color:#67e8f9;">PlaceMapComponent</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#fcd34d;">Leaflet</span>
  <span style="color:#fcd34d;">Leaflet</span> --&gt; <span style="color:#86efac;">OpenStreetMap-Kacheln</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#c4b5fd;">Place.coordinates</span>
  <span style="color:#67e8f9;">PlaceMapComponent</span> --&gt; <span style="color:#fca5a5;">placeSelected</span></code></pre>

Zusammenfassung des Diagramms:

- `PlaceMapComponent` ist der zentrale Kartenbaustein
- dieselbe Komponente wird in `places` und im Detail wiederverwendet
- die Komponente rendert die Karte mit `Leaflet` und laedt OpenStreetMap-Kacheln
- die Koordinaten kommen direkt aus `Place.coordinates`
- beim Klick auf einen Marker emittiert die Komponente `placeSelected`

## 5. Aktuelle Features

### 5.1 Home

Hauptbausteine:

- `home-hero-section`
- `trending-cities-section`
- `why-yeppet-section`

Relevante technische Entscheidungen:

- die Seite wird aus typisierten Fake-Daten aufgebaut
- das `hero` kapselt Navigation nach `places`
- die grossen Bloecke der `home` leben in getrennten Komponenten

### 5.2 Places

Hauptbausteine:

- `place-filters`
- `place-map`
- `place-card`
- `places-page`
- `place-detail-page`

Relevante technische Entscheidungen:

- `places-page` zentralisiert Query-Parameter, aktive Filter und Ergebnisse
- `place-map` ist wiederverwendbar und parametrisierbar
- `place-card` wird in Liste und Favoriten wiederverwendet

### 5.3 Favorites

Hauptbausteine:

- `favorites-page`
- `favorite-toggle-button`
- `favorites.service`

Relevante technische Entscheidungen:

- der Status wird lokal und simuliert gehalten
- der Fluss kann spaeter durch echte Persistenz ersetzt werden

## 6. Services und simulierte Daten

### 6.1 PlaceService

`PlaceService` ist der zentrale Service der Feature `places`.

Verantwortlichkeiten:

- Ortsliste abrufen
- Detail nach `id` abrufen
- Filter anwenden
- verfuegbare Staedte bereitstellen
- verfuegbare Typen bereitstellen
- Typ-Labels aufbauen

Datenquelle:

- `PLACES_FAKE`

### 6.2 FavoritesService

Verantwortlichkeiten:

- Fake-Status der Favoriten halten
- wissen, ob ein Ort gespeichert ist
- Favoriten hinzufuegen und entfernen

## 7. Implementierung der Karte

### 7.1 Verwendete Bibliotheken

Diese Abhaengigkeiten wurden installiert:

```bash
cd src/Web
npm install leaflet @types/leaflet
```

Begruendung:

- `leaflet` liefert den Kartenmotor
- `@types/leaflet` liefert TypeScript-Typen

### 7.2 Globale Kartenstile

Der Leaflet-Stil wurde global importiert:

```scss
@import 'leaflet/dist/leaflet.css';
```

Ort:

- `src/Web/src/styles.scss`

### 7.3 Erweiterung des Modells `Place`

Das Modell `Place` wurde erweitert, um Koordinaten aufzunehmen:

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

Ort:

- `src/Web/src/app/features/places/models/place.model.ts`

### 7.4 Koordinaten in den Mocks

Jeder Fake-`Place` enthaelt praezise Koordinaten:

```ts
coordinates: {
  lat: 41.390205,
  lng: 2.191987
}
```

Entscheidung:

- die Koordinaten werden manuell kontrolliert
- wir haengen in dieser Phase nicht von externem Geocoding ab
- die Genauigkeit ist ausreichend, um realistisches Verhalten zu sehen

Ort:

- `src/Web/src/app/features/places/mock/places.fake.ts`

### 7.5 Zentralisierte Komponente `app-place-map`

Die Karte wurde nicht direkt in den Seiten implementiert.
Sie wurde in eine wiederverwendbare Komponente gekapselt.

Inputs:

- `places`
- `selectedPlaceId`
- `height`
- `emptyTitle`
- `emptyCopy`

Output:

- `placeSelected`

Vereinfachter Ausschnitt:

```ts
readonly places = input.required<Place[]>();
readonly selectedPlaceId = input<string | null>(null);
readonly height = input('24rem');
readonly emptyTitle = input('Es gibt keine Positionen zum Anzeigen');
readonly emptyCopy = input('Passe die Filter an, um Orte auf der Karte zu sehen.');
readonly placeSelected = output<string>();
```

Ort:

- `src/Web/src/app/features/places/components/place-map/place-map.component.ts`

### 7.6 Lazy-Laden von Leaflet

Leaflet wird lazy geladen, wenn die Komponente die Karte rendern muss:

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

Begruendung:

- die Bibliothek soll nicht zu frueh geladen werden
- die Karte soll nicht initialisiert werden, wenn es keine Ergebnisse gibt
- die Komponente bleibt dadurch effizienter

### 7.7 OpenStreetMap-Kacheln

Die Karte wird mit einem oeffentlichen `tileLayer` von OpenStreetMap gerendert:

```ts
this.leaflet
  .tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors'
  })
  .addTo(this.map);
```

### 7.8 Marker und Auswahl

Jeder `Place` wird in einen `circleMarker` umgewandelt:

```ts
const marker = this.leaflet.circleMarker([place.coordinates.lat, place.coordinates.lng], {
  radius: isSelected ? 11 : 8,
  weight: isSelected ? 3 : 2,
  color: isSelected ? '#065f46' : '#0f766e',
  fillColor: isSelected ? '#2dd4bf' : '#99f6e4',
  fillOpacity: isSelected ? 0.95 : 0.85
});
```

Wenn auf einen Marker geklickt wird:

```ts
marker.on('click', () => this.placeSelected.emit(place.id));
```

### 7.9 View-Anpassung

Wenn ein Ort ausgewaehlt ist:

```ts
this.map.setView([selectedPlace.coordinates.lat, selectedPlace.coordinates.lng], 15);
```

Wenn nicht, passt sich die Karte an die gesamte Ergebnismenge an:

```ts
this.map.fitBounds(bounds, {
  padding: [28, 28],
  maxZoom: this.places().length === 1 ? 15 : 13
});
```

### 7.10 Wiederverwendung auf den Seiten

In `places`:

```html
<app-place-map
  [places]="places()"
  height="25rem"
  emptyTitle="Keine Ergebnisse auf der Karte"
  emptyCopy="Sobald die Filter Orte zurueckgeben, siehst du sie auch hier."
  (placeSelected)="openPlaceFromMap($event)"
/>
```

In `place detail`:

```html
<app-place-map
  [places]="placeAsArray"
  [selectedPlaceId]="selectedPlace.id"
  height="20rem"
  emptyTitle="Keine Position verfuegbar"
  emptyCopy="Dieser Ort hat keine Koordinaten, die auf der Karte angezeigt werden koennen."
/>
```

### 7.11 Leerzustand der Komponente

Wenn es keine Orte gibt, initialisiert die Komponente die Karte nicht und zeigt einen kontrollierten Leerzustand:

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

### 7.12 Betroffene Dateien

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

## 8. Aktuelle Entscheidungen

- die Karte lebt in `places`, nicht auf der Startseite
- die Karte wird als funktionaler Teil der Suche behandelt
- die Komponente muss parametrisierbar sein
- die Orte haben praezise simulierte Koordinaten
- die `home` konzentriert nicht die Ergebnislogik

## 9. Offene Verfeinerungspunkte

- bessere UX fuer Marker
- bessere Popups
- mehr Kriterien, wenn viele Daten vorhanden sind
- moeglicher Modus `Liste / Karte / Gemischt`
- Basisschicht fuer Fehlerbehandlung in Phase II

## 10. Dokumentreferenzen

Funktionales Dokument:

- [`funktional-de.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/de/funktional-de.md)

Phasen-Dokument:

- [`project-phases.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/project-phases.md)
