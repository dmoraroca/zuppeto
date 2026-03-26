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
- mapa funcional a `places`
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
- `app-place-map`

La resta de components es mantenen específics de cada `feature` mentre no tinguem una necessitat real de reutilització.

### Què queda validat en tancar la fase I

Queda validat:

- model TypeScript de `Place`
- `PlaceService` mock
- navegació entre `home`, `places`, `place detail`, `favorites` i `permissions`
- filtres i cerca sobre dades simulades
- component de mapa centralitzat i reutilitzat a `places` i `place detail`
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

- **consolidar la feature `places` amb una UX de cerca més rica**
- **polir la vista mapa ja existent a `places`**
- **millorar la sincronització entre mapa, filtres i resultats**
- **decidir si `places` treballarà amb `llista`, `mapa` o mode mixt**
- **refinar la UX de marcadors, popups i selecció al mapa**
- decidir com escalar la vista mapa quan hi hagi més densitat de dades
- **refinar `favorites` perquè el flux de guardar i revisar llocs sigui més natural**
- **millorar el `place detail` amb millor jerarquia i més context**
- **revisar empty states, filtres actius i textos de suport**
- **polir les seccions de la `home` que ara són correctes però encara provisionals**
- **consolidar quins components compartits val la pena fixar definitivament**
- **enriquir les dades simulades perquè siguin més realistes**
- preparar els serveis mock per substituir-los per API sense reescriure UI
- **introduir una capa base de gestió d'errors**
- **afegir interceptor global per errors HTTP**
- **afegir servei central d'errors o notificacions**
- **definir una UI comuna per mostrar errors i missatges globals**
- **reduir `try/catch` repetits als punts on el problema sigui transversal**
- revisar responsive fi de totes les pantalles
- definir millor `Ajuda`, `Contacta'ns` i les pàgines informatives
- afinar la navegació general perquè cada CTA tingui una funció clara

### Entregables de la fase II

La fase II s'hauria de poder resumir en aquests blocs:

1. **`Places` amb mapa sota filtres**
2. **`Place detail` més complet i més clar**
3. **Base d'autenticació i perfil**
4. `Home` més madura visualment
5. Components compartits consolidats
6. Mocks més rics i preparats per API
7. **Capa base de gestió d'errors**
8. Responsive i UX refinats

### Estat parcial de la fase II

Ara mateix, dins de la fase II, ja tenim avançat:

- **mapa funcional integrat a `places` sota els filtres**
- **component `app-place-map` centralitzat i parametritzable**
- **coordenades simulades precises als `Place`**
- **reutilització del mateix mapa a `place detail`**
- **millora del context de filtres actius i títols dinàmics a `places`**
- **decisió de producte fixada: `places` treballa en mode mixt amb mapa sota filtres i llistat sincronitzat com a base principal de comparació**
- **poliment real de la UX del mapa a `places`**
- **selecció de marcador amb resum contextual del lloc**
- **accions de mapa per veure tots els resultats o treure la selecció**
- **highlight visual del `place-card` associat al marcador seleccionat**
- **header corregit perquè el mapa no el sobreposi en scroll**
- **preview del `hero` més orientada a contingut destacat i no només a ciutats**
- **`home` polida amb millor direcció de producte: hero menys provisional, bloc de recorregut funcional, ciutats amb CTA més clar i tancament orientat a `places` i `favorites`**
- **catàleg de compartits fixat definitivament: `app-section-heading`, `app-generic-info-card`, `app-favorite-toggle-button`, `app-place-card`, `app-place-map` i `app-error-notifications`**
- **mocks de `places` enriquits amb barri, volum de ressenyes, preu orientatiu i política pet per fer més creïbles el llistat i el detall**
- **login fake amb email i password**
- **rols `USER` i `ADMIN` ja operatius**
- **sessió fake mantinguda amb redirecció automàtica a `login`**
- **guards de `auth`, `guest` i `admin` ja aplicats a les rutes**
- **pàgina de `Perfil` fake amb manteniment bàsic de dades**
- **consentiment LGPD/GDPR obligatori per `USER` al manteniment de perfil**
- **visibilitat i accés de `Del desenvolupador` només per `ADMIN`**
- **revisió de `favorites` més natural amb resum, guardat recent, filtres locals i ordenació per reprendre la cerca sense tornar a començar**
- **interceptor HTTP global per capturar errors de backend**
- **servei centralitzat de notificacions d'error**
- **UI global de notificacions per mostrar errors sense repetir lògica a les pàgines**

La resta de punts de la fase II continuen oberts fins que es marquin també en negreta.

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
2. entrar a fase II començant per `places`
3. consolidar `auth/profile`, polir el mapa ja integrat i afegir capa base d'errors
4. no entrar encara ni en backend real ni en multiidioma complet

## Estat actual

La fase I ja queda tancada perquè la web permet:

- navegar per `home`, `places`, `place detail`, `favorites` i `permissions`
- treballar amb dades simulades però estructurades
- reutilitzar components reals i no només markup repetit
- validar fluxos de filtrat, detall i favorits sense backend
- tenir una primera base de mapa funcional amb component centralitzat

El següent tram de treball ja correspon a la fase II.
