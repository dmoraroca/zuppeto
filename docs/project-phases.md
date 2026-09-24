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

## Punt estable conegut

- **Commit `068`** (`fase IV`): entorn **funcional** validat a **Fedora** (web Angular 22, API, stack Docker/F5 local amb Chrome Flatpak Swagger+Web). Serveix de referència segura per continuar la Fase IV; no implica que la fase estigui tancada.

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

- autenticació pròpia i Google OAuth real (**FET**); `LinkedIn` queda descartat per decisió funcional de producte i `Facebook` continua pendent
- rols i permisos (**FET**)
- pàgines internes (**FET**): punt tancat amb el criteri definit per direcció de projecte, incloent base d'accés intern i manteniments d'administració ja operatius.
- gestió de contingut o dades (**EN CURS**): `llocs` i després `favorits`; dins la Iteració 6 territorial, les subfases 0–VI estan completades i Fase VII està en curs amb VII.2–VII.7 completades i VII.8 pendent. VIII–IX no s'han iniciat.
- accessos restringits a determinades funcionalitats (**PENDENT**)
- revisió de documentació pendent (comprovar opcions i buits) (**PENDENT**)
- canvi de contrasenya i operativa bàsica de credencials (**PENDENT**; tot el treball de contrasenya queda empaquetat aquí, sense escindir-lo en un altre punt)

### Resultat esperat

La plataforma ja diferencia entre usuaris públics, usuaris autenticats i àrees internes (**PENDENT**).

### Què s'ha fet en aquest tram de la Fase IV

- `pàgines internes` queda **FET** (accés intern i manteniments principals operatius)
- Fase IV, Iteració 4 — Google OAuth real: **🟢 VALIDADA I TANCADA el 2026-09-17**. Inclou alta Google nova amb rol `USER`, autovinculació segura d'un compte local activat per email verificat, `ExternalIdentity` única, TOTP sense bypass, logout/relogin, regressions, secrets, cleanup i registre Excel.
- Fase IV, Iteració 5 — LinkedIn OAuth: **➖ DESCARTADA PER DECISIÓ FUNCIONAL DE PRODUCTE el 2026-09-18**. La integració es va estudiar i implementar tècnicament abans de la decisió; s'han retirat del producte UI, endpoints, adaptador OIDC i configuració LinkedIn, mantenint la infraestructura federada genèrica útil per Google i proveïdors futurs. No és `FAIL`, `PENDENT` ni `NO VALIDADA`.
- La LinkedIn Page Petiloc es conserva com a canal corporatiu i queda separada del sistema d'autenticació.
- Fase IV, Iteració 6 — **SUBFASE VII EN CURS — VII.2–VII.7 COMPLETADES / VII.8 PENDENT**. Hi ha worker durable, artefacte persistent, configuració i mappings pilot i validació controlada API-worker-PostgreSQL. Les fonts continuen `Pending`: no s’han publicat datasets reals, no s’ha fet backfill i City/GeoNames es preserven.
- Fase IV, Iteració 7 — **EN REVISIÓ**. Proposta pendent de confirmació: substituir «GeoNames + alta lazy» per una API territorial sobre catàleg propi (`Angular → API Petiloc → TerritorialService → PostgreSQL`). GeoNames no s'elimina encara.
- Fase IV, Iteració 8 — **PLANIFICADA**. Selector territorial compartit sobre la futura API pròpia per a registre/login, Perfil, Admin Usuaris, Places, Admin Llocs i filtres.
- login: destí per defecte **Inici** per a tots els rols (ZUP-004); ADMIN obre permisos des del menú, no a la primera pantalla
- `gestió de contingut o dades` continua **EN CURS**: focus actual en `llocs` (cerca lazy + base de proveïdor extern) i `favorits` (Cercar/Netejar sobre BD + caducitat Google 30 dies via Details). Tram llistat/fitxa 2026-09-01: **OK** (foto, paginació, Cercar, tres apartats). Pendent (no ara): recordar take per filtre (20/40/60) si es torna a aplicar; llistat scroll editorial (foto+text, seleccionat a encaixar); Ajuda «Dubtes habituals» amb textos i format diferents. Vegeu `docs/ca/millores-pendents-ca.md`.
- detall funcional i tècnic del tram actual: `docs/ca/funcional-ca.md` (§3.17, §3.12, §12.5, §12.7) i `docs/ca/tecnic-ca.md` (§2.11.3, §2.11.3.1, §2.11.4 procedència `places`, §2.11.5 menús admin, §2.10.3 permisos de build local)

### Roadmap de proveïdors d'identitat

- `Email + contrasenya`: **ACTIU**.
- `Google OAuth/OIDC`: **🟢 ITERACIÓ 4 VALIDADA I TANCADA**.
- `LinkedIn OAuth/OIDC`: **➖ DESCARTAT PER DECISIÓ FUNCIONAL DE PRODUCTE**.
- `Microsoft OAuth/OIDC`: **MILLORA FUTURA**.
- `Sign in with Apple`: **MILLORA FUTURA**.
- `Samsung / LG`: **ESTUDI FUTUR DE VIABILITAT**, sense compromís d'implementació.
- `Facebook`: **PENDENT** fins a la seva iteració; no s'ha implementat en la Iteració 5.

## Fase V · Internacionalització (**PENDENT**)

### Objectiu

Fer el producte multiidioma de manera seriosa, un cop el contingut i l'estructura siguin estables.

### Què entra dins la fase V

- internacionalització de la presentació territorial i de la resta del producte sobre el futur catàleg europeu definit a la Iteració 6 (**PENDENT**); fonts, API i llicències territorials es governen ja dins les Iteracions 6–8
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
- observabilitat mínima (**BASE IMPLEMENTADA**: Serilog correlacionat amb ZUP/rol/navegador i diagnòstic Playwright; centralització de producció **PENDENT**)
