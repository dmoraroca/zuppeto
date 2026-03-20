# YepPet · Fases del projecte

Aquest document defineix com s'organitza el desenvolupament de YepPet per fases, què s'espera de cada etapa i l'estat real de cadascuna.

## Objectiu general

YepPet ha de créixer com una plataforma pet-friendly per descobrir llocs, estades i serveis que accepten mascotes. El desenvolupament es farà de manera incremental:

- primer validant experiència i estructura amb frontend i dades fake
- després consolidant models i navegació funcional
- i finalment passant a backend real, permisos i internacionalització

## Principis de treball

- Frontend primer, backend després
- Dades fake mentre validem UX i estructura
- Arquitectura per `features`
- Components separats per responsabilitat
- Cada component dins la seva pròpia carpeta
- Reutilització real abans que duplicació
- Internacionalització al final, no al principi

## Fase I · Frontend base funcional amb dades simulades

### Objectiu

Construir una web Angular funcional, visualment coherent i preparada per créixer, però encara sense backend real.

### Què entra dins la fase I

- estructura base del projecte
- web Angular actual
- disseny i UX de la `home`
- navegació inicial
- dades fake
- components reutilitzables
- base de `features`, `shared` i `core`

### Estat actual de la fase I

La fase I queda completada amb aquesta base:

- projecte base a `yeppet`
- Angular 21 a `src/Web`
- `header` i `footer`
- `home` separada en seccions
- dades simulades per a la `home`
- feature `places`
- llistat de llocs amb filtres
- detall de lloc
- feature `favorites` amb estat fake
- navegació funcional des de la `home`
- `Trending cities` connectat a `places` per ciutat
- components separats en carpetes pròpies
- components compartits reutilitzables
- pàgina `permissions`
- primera base visual del producte

### Components compartits reutilitzables acordats

Per a la fase I consolidem aquests components com a reutilitzables reals:

- `app-section-heading`
- `app-generic-info-card`
- `app-favorite-toggle-button`
- `app-place-card`

La resta de components es mantenen específics de cada `feature` mentre no tinguem una necessitat real de reutilització.

### Què queda validat en tancar la fase I

Queda validat:

- model TypeScript de `Place`
- `PlaceService` mock
- navegació entre `home`, `places`, `place detail`, `favorites` i `permissions`
- filtres i cerca sobre dades simulades
- estat fake de favorits
- base responsive per `home`, `places`, `detail` i `favorites`
- coherència general de copies en català

### Criteri de finalització de la fase I

La fase I es considerarà acabada quan:

- es pugui navegar per `home`, `places`, `place detail` i `favorites`
- la navegació funcioni sense backend
- totes les dades surtin de mocks estructurats
- la UI sigui responsive
- la base de components sigui prou neta per créixer

## Fase II · Consolidació funcional i refinament

### Objectiu

Convertir la base funcional de la fase I en una aplicació frontend més completa i més refinada a nivell de producte.

### Què entra dins la fase II

- ampliar les features principals
- refinar filtres, cerca i empty states
- afinar navegació i continguts
- revisar millor reutilització de components
- preparar la web per substituir mocks per API sense reescriure UI

### Resultat esperat

Una aplicació frontend sòlida que simula millor el comportament real del producte i està llesta per començar a parlar amb backend.

## Fase III · Backend real i persistència

### Objectiu

Passar de frontend mock-first a un sistema real amb backend i base de dades.

### Què entra dins la fase III

- disseny d'entitats reals
- backend `.NET`
- PostgreSQL
- API per `places`, `favorites`, `users`, `reviews`
- substitució progressiva de serveis mock per serveis reals

### Resultat esperat

YepPet deixa de ser una simulació i passa a tenir dades persistides i fluxos reals.

## Fase IV · Permisos, administració i operativa

### Objectiu

Separar clarament les zones públiques de les zones internes o controlades per permisos.

### Què entra dins la fase IV

- autenticació
- rols i permisos
- pàgines internes
- gestió de contingut o dades
- accessos restringits a determinades funcionalitats

### Resultat esperat

La plataforma ja diferencia entre usuaris públics, usuaris autenticats i àrees internes.

## Fase V · Internacionalització

### Objectiu

Fer el producte multiidioma de manera seriosa, un cop el contingut i l'estructura siguin estables.

### Què entra dins la fase V

- estratègia d'i18n
- idiomes d'Europa
- àrab
- xinès
- suport RTL
- revisió de longituds de text
- SEO per idioma

### Resultat esperat

YepPet pot operar en diversos idiomes sense haver d'improvisar textos dispersos dins components.

## Fase VI · Poliment i desplegament

### Objectiu

Preparar el producte per sortir a un entorn real.

### Què entra dins la fase VI

- optimització visual final
- revisió de responsive complet
- revisió de rendiment
- QA
- desplegament
- observabilitat mínima

## Decisió actual

Ara mateix el focus correcte és aquest:

1. donar per tancada la fase I
2. consolidar i polir les pantalles ja creades
3. preparar la transició cap a la fase II
4. no entrar encara ni en backend real ni en multiidioma complet

## Estat actual

La fase I ja queda tancada perquè la web permet:

- navegar per `home`, `places`, `place detail`, `favorites` i `permissions`
- treballar amb dades simulades però estructurades
- reutilitzar components reals i no només markup repetit
- validar fluxos de filtrat, detall i favorits sense backend
