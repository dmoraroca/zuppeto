# Zuppeto · Fases del projecte

Aquest document defineix com s'organitza el desenvolupament de Zuppeto per fases, què s'espera de cada etapa i l'estat real de cadascuna.

## Objectiu general

Zuppeto ha de créixer com una plataforma pet-friendly per descobrir llocs, estades i serveis que accepten mascotes. El desenvolupament es farà de manera incremental:

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

## Criteris actius de treball

- Quan es consulti l'`estat`, la referència principal és aquest document
- Les explicacions funcionals, decisions i detall d'abast s'han de documentar a `docs/ca/funcional-ca.md`
- En tancar un punt de treball (**tanquem punt**), s'ha d'actualitzar també la documentació en català del que s'ha fet: a `docs/ca/funcional-ca.md` si afecta producte, comportament d'usuari, abast o decisions visibles; a `docs/ca/tecnic-ca.md` si afecta arquitectura, stack, fitxers, configuració, persistència o patrons; sovint cal tocar els dos (per exemple infra sense canvi visible: resum al funcional + detall al tècnic). L'assistència de desenvolupament ha de proposar o aplicar aquests textos en el mateix tancament de punt, sense substituir el criteri humà de revisió
- Cada fase i cada punt rellevant s'han de marcar explícitament com a (**PENDENT**), (**EN CURS**) o (**FET**); el pas a (**FET**) el fixa qui porta el projecte en dir-ho explícitament en tancar el punt, no per inferència ni sense aquesta confirmació
- Els punts marcats en negreta compten com a fets o consolidats mentre no estiguin normalitzats amb etiqueta explícita
- Els punts sense negreta compten com a pendents o oberts mentre no estiguin normalitzats amb etiqueta explícita
- Si no queden punts objectiu pendents dins d'una fase, la fase es considera acabada
- La Fase III s'ha de construir amb `DDD` com a base arquitectònica
- El disseny i la implementació han de seguir `SOLID` de manera estricta
- L'ordre de treball de la Fase III és: tancar el model de domini, després contractes i necessitats de persistència, després model relacional a `PostgreSQL`, `Entity Framework`, mapatges, migracions i connexió amb l'API
- Es prioritzaran patrons de disseny quan aportin mantenibilitat, claredat i facilitat d'evolució
- Si apareix una solució més moderna, més simple o tecnològicament millor, s'ha de plantejar abans d'implementar-la

## Fase I · Frontend base funcional amb dades simulades (**FET**)

### Objectiu

Construir una web Angular funcional, visualment coherent i preparada per créixer, però encara sense backend real.

### Què entra dins la fase I

- estructura base del projecte (**FET**)
- web Angular actual (**FET**)
- disseny i UX de la `home` (**FET**)
- navegació inicial (**FET**)
- dades fake (**FET**)
- components reutilitzables (**FET**)
- base de `features`, `shared` i `core` (**FET**)

## Fase II · Consolidació funcional i refinament (**FET**)

### Objectiu

Convertir la base funcional de la fase I en una aplicació frontend més completa i més refinada a nivell de producte.

### Què entra dins la fase II

- consolidar la feature `places` amb una UX de cerca més rica (**FET**)
- polir la vista mapa ja existent a `places` (**FET**)
- millorar la sincronització entre mapa, filtres i resultats (**FET**)
- decidir si `places` treballarà amb `llista`, `mapa` o mode mixt (**FET**)
- refinar la UX de marcadors, popups i selecció al mapa (**FET**)
- decidir com escalar la vista mapa quan hi hagi més densitat de dades (**FET**)
- refinar `favorites` perquè el flux de guardar i revisar llocs sigui més natural (**FET**)
- millorar el `place detail` amb millor jerarquia i més context (**FET**)
- revisar empty states, filtres actius i textos de suport (**FET**)
- polir les seccions de la `home` que ara són correctes però encara provisionals (**FET**)
- consolidar quins components compartits val la pena fixar definitivament (**FET**)
- enriquir les dades simulades perquè siguin més realistes (**FET**)
- preparar els serveis mock per substituir-los per API sense reescriure UI (**FET**)
- introduir una capa base de gestió d'errors (**FET**)
- afegir interceptor global per errors HTTP (**FET**)
- afegir servei central d'errors o notificacions (**FET**)
- definir una UI comuna per mostrar errors i missatges globals (**FET**)
- reduir `try/catch` repetits als punts on el problema sigui transversal (**FET**)
- revisar responsive fi de totes les pantalles (**FET**)
- definir millor `Ajuda`, `Contacta'ns` i les pàgines informatives (**FET**)
- afinar la navegació general perquè cada CTA tingui una funció clara (**FET**)

### Resultat esperat

Una aplicació frontend sòlida que simula millor el comportament real del producte i està llesta per començar a parlar amb backend.

## Fase III · Backend real i persistència (**FET**)

### Objectiu

Passar de frontend mock-first a un sistema real amb backend i base de dades.

### Què entra dins la fase III

- disseny del model de domini real (**FET**)
- contractes de repositori i necessitats de persistència (**FET**)
- model relacional a `PostgreSQL` (**FET**)
- persistència amb `Entity Framework` última versió (**FET**)
- configuració de mapatge, migracions i repositoris (**FET**)
- backend `.NET` (**FET**)
- API per `places`, `favorites`, `users`, `reviews` (**FET**)
- substitució progressiva de serveis mock per serveis reals (**FET**)

### Resultat esperat

Zuppeto deixa de ser una simulació i passa a tenir dades persistides i fluxos reals (**FET**).

## Fase IV · Permisos, administració i operativa (**EN CURS**)

### Objectiu

Separar clarament les zones públiques de les zones internes o controlades per permisos.

### Què entra dins la fase IV

- autenticació pròpia i federada (`Google`, `LinkedIn`, `Facebook` i altres proveïdors OAuth/OIDC) (**FET**)
- rols i permisos (**FET**)
- pàgines internes (**FET**): punt tancat amb el criteri definit per direcció de projecte, incloent base d'accés intern i manteniments d'administració ja operatius.
- gestió de contingut o dades (**EN CURS**): `llocs` i després `favorits`; catàleg territorial (**prioritat Espanya**): detall a `docs/ca/funcional-ca.md` (§3.15.1).
- accessos restringits a determinades funcionalitats (**PENDENT**)
- revisió de documentació pendent (comprovar opcions i buits) (**PENDENT**)
- canvi de contrasenya i operativa bàsica de credencials (**PENDENT**; tot el treball de contrasenya queda empaquetat aquí, sense escindir-lo en un altre punt)

### Resultat esperat

La plataforma ja diferencia entre usuaris públics, usuaris autenticats i àrees internes (**PENDENT**).

### Què s'ha fet en aquest tram de la Fase IV

- `pàgines internes` queda **FET** (accés intern i manteniments principals operatius)
- `gestió de contingut o dades` continua **EN CURS**: focus actual en `llocs` (cerca lazy + base de proveïdor extern), després `favorits`
- detall funcional i tècnic del tram actual: `docs/ca/funcional-ca.md` (§3.17) i `docs/ca/tecnic-ca.md` (§2.11.3)

## Fase V · Internacionalització (**PENDENT**)

### Objectiu

Fer el producte multiidioma de manera seriosa, un cop el contingut i l'estructura siguin estables.

### Què entra dins la fase V

- **Territori i GeoNames** (extensió cap a la UE, API, llicències): criteri a `docs/ca/funcional-ca.md` (§3.15.1); traçabilitat tècnica a `docs/ca/tecnic-ca.md` (**PENDENT**)
- estratègia d'i18n (**PENDENT**)
- idiomes d'Europa (**PENDENT**)
- àrab (**PENDENT**)
- xinès (**PENDENT**)
- suport RTL (**PENDENT**)
- revisió de longituds de text (**PENDENT**)
- SEO per idioma (**PENDENT**)
- revisió profunda de privadesa i complint normativa (**PENDENT**); esbossos i apunts inicials a `docs/ca/privacitat-ca.md` (esborrany a consolidar en aquesta fase)
- documentar classes i funcions (comentaris de capçalera) a tota la solucio, incloent JavaScript/TypeScript i CSS/SCSS (**PENDENT**)

### Resultat esperat

Zuppeto pot operar en diversos idiomes sense haver d'improvisar textos dispersos dins components (**PENDENT**).

## Fase VI · Poliment i desplegament (**PENDENT**)

### Objectiu

Preparar el producte per sortir a un entorn real.

### Què entra dins la fase VI

- optimització visual final (**PENDENT**)
- revisió de responsive complet (**PENDENT**)
- revisió de rendiment (**PENDENT**)
- QA (**PENDENT**)
- desplegament (**PENDENT**)
- observabilitat mínima (**PENDENT**)
