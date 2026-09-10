# Automatització E2E de Zuppeto

## 1. Objectiu

Automatitzar el joc manual de Zuppeto mantenint la correspondència exacta amb els codis ZUP, els rols i els navegadors. La suite ha de detectar regressions funcionals, vulneracions de permisos, errors JavaScript, errors de xarxa i respostes inesperades del backend.

Aquest document és el registre viu de la iniciativa. Cada canvi material en l'automatització E2E ha d'actualitzar-ne l'estat, les decisions tècniques i els resultats.

## 2. Punt de partida

- El joc manual conté **177 casos per navegador**.
- Chrome ha completat **177/177 (100%)**.
- El projecte ja disposa d'una base Playwright separada a `/e2e`.
- La configuració actual només defineix un projecte Chromium.
- Ja existeixen proves de navegació, rols, seguretat, API, pantalles internes i fluxos d'administració.
- L'Excel `docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx` continua sent la font funcional dels casos manuals.

## 3. Decisió d'arquitectura

Playwright executarà l'aplicació Angular des de fora, contra la web, l'API .NET i PostgreSQL reals de l'entorn de prova. Els tests unitaris d'Angular continuaran separats dels E2E.

Hi haurà una sola implementació per cas funcional i rol. Els projectes de Playwright repetiran aquesta mateixa implementació en cada navegador, sense duplicar el codi dels tests.

La identitat completa d'una execució serà:

`codi ZUP + rol + navegador`

Amb cinc navegadors, els 177 casos generaran 885 execucions independents.

## 4. DDD i SOLID

L'automatització ha de respectar els mateixos criteris arquitectònics que el producte:

- llenguatge ubic amb conceptes com `Place`, `Favorite`, `Viewer`, `Permission` i `ZUP`;
- especificacions primes que expressin comportament funcional;
- escenaris d'aplicació reutilitzables per als fluxos;
- adaptadors per Playwright, API, persistència, navegadors i informes;
- fixtures responsables de sessions i dades de prova;
- dependències orientades cap als contractes i no cap als detalls tècnics;
- tests independents de l'ordre d'execució;
- composició abans que herència i absència de fitxers gegants.

Estructura prevista:

```text
e2e/
├── domain/
├── application/
├── infrastructure/
│   ├── api/
│   ├── database/
│   └── playwright/
├── pages/
├── tests/
│   ├── auth/
│   ├── places/
│   ├── favorites/
│   ├── profile/
│   └── admin/
└── playwright.config.ts
```

## 5. Navegadors

La mateixa suite s'executarà mitjançant projectes Playwright separats:

- Chrome;
- Firefox;
- Edge;
- Brave;
- Opera.

Chrome, Edge, Brave i Opera comparteixen motor Chromium, però s'executaran individualment per detectar diferències de versió, configuració i integració.

IE11 queda pendent de decisió formal. Playwright no l'executa i la web Angular moderna no es dissenya per aquest navegador; el criteri recomanat és marcar-lo `N/A` si no és un requisit explícit del producte.

## 6. Errors detectats automàticament

La fixture transversal `e2e/infrastructure/playwright/test.ts` registra, per cada prova i navegador:

- excepcions JavaScript no controlades (`pageerror`);
- missatges `console.error`;
- promeses rebutjades visibles al navegador;
- peticions de xarxa fallides;
- respostes inesperades `5xx`;
- errors de navegació o càrrega de recursos essencials.

Els errors esperats pel mateix escenari, com un `401`, `403` o `404` provocat expressament, s'han de declarar al test perquè no produeixin falsos `KO`.

La captura és comuna a totes les especificacions: aquestes importen `test` i `expect` des de la fixture del projecte i no directament des de `@playwright/test`. Els errors d'extensions de navegador coneguts (`runtime.lastError` i tancament del port de missatges) s'ignoren perquè no pertanyen a Angular. Les peticions fallides només es consideren per recursos essencials (`document`, `script`, `stylesheet`, `xhr` i `fetch`).

Cada petició web a `/api/` i cada petició feta amb la fixture `request` envia:

- `X-Correlation-ID`;
- `X-Zuppeto-Test-Code`;
- `X-Zuppeto-Test-Role`;
- `X-Zuppeto-Test-Browser`.

L'API valida la longitud i els caràcters d'aquestes capçaleres abans d'incorporar-les a Serilog. Quan el títol d'una prova encara no conté codi ZUP o rol, queda registrat com `UNMAPPED` o `UNSPECIFIED`; la migració dels 177 casos haurà d'eliminar progressivament aquests valors.

`HttpRequestExternalPlaceCallPolicy` interpreta la capçalera de navegador E2E com un context no facturable. Els escenaris E2E no poden iniciar cerques, detalls, fotografies ni enriquiments en segon pla contra Google Places real; han de treballar amb el catàleg persistent o amb adaptadors controlats.

Quan una prova falli, l'informe ha d'incloure:

- codi ZUP;
- rol;
- navegador;
- URL;
- missatge i stack trace;
- captura de pantalla;
- traça de Playwright;
- vídeo només quan sigui necessari.

## 7. Sessions i dades

Es prepararan sessions reutilitzables per `USER`, `ADMIN`, `DEVELOPER`, `VIEWER` i sense sessió. Les proves específiques de login continuaran iniciant la sessió des de la interfície.

Cada prova que escrigui dades haurà de crear registres identificables i temporals, netejar-los en acabar o executar-se contra una base reiniciada des d'un estat conegut. Cap prova destructiva s'executarà contra producció.

## 8. Execució i informes

- Durant el desenvolupament: cas ZUP afectat en un navegador.
- Abans d'integrar: conjunt crític en Chrome.
- Integració contínua: suite completa en Chrome, Firefox i Edge.
- Execució programada: suite completa en tots els navegadors configurats.

Playwright produeix resultats JSON a `test-results/results.json` i un informe HTML. Els errors també adjunten `browser-diagnostics` en JSON, la captura de pantalla i la traça retinguda quan falla la prova. Un procés posterior i únic podrà traslladar-los a l'Excel; els navegadors no escriuran simultàniament sobre el fitxer binari.

També es crearà una comprovació de cobertura que compari el catàleg automatitzat amb les 177 files funcionals i indiqui qualsevol combinació ZUP/rol encara no implementada.

## 9. Estat i decisions pendents

Estat actual: **disseny acordat; observabilitat transversal implementada; migració dels 177 casos i navegadors pendent**.

Abans de començar cal:

1. Fixar la mateixa versió de Playwright al `package-lock.json` i a la imatge Docker. Actualment el lock ha resolt 1.59.1 i Docker declara 1.53.0.
2. Confirmar les rutes dels executables de Chrome, Edge, Brave i Opera als entorns local i CI.
3. Decidir formalment el tractament d'IE11.
4. Definir l'esquema JSON de resultats i el procés d'actualització de l'Excel.
5. Construir un exemple vertical complet amb codi ZUP i rol explícits abans de migrar els 177 casos.
