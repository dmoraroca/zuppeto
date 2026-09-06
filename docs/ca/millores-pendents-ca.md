# Millores pendents (CA)

Backlog de millores detectades mentre es treballa o es fan proves manuals.
No substitueix `docs/project-phases.md` ni el joc de proves Excel: aquí només anotem idees per comentar i prioritzar després.

**Format d’entrada:** data i hora (Europe/Madrid), després la descripció. Si la millora ja està feta, al final de l’entrada es marca **FET**.

---

## 2026-09-04 21:06 CEST

**Àmbit:** Consola admin · check «Accepto les condicions» · actor Administrador · ZUP-112 (en curs)

**Què es veu:** a crear/modificar (usuaris, rols, etc.) l’administrador ha de marcar el check de privacitat i el Desar el comprova. Al **perfil**, l’admin ja n’està exempt (no es mostra el check i no es valida).

**Millora:** si qui opera és **Administrador**, el check **no cal**: no mostrar-lo i **no comprovar-lo** (Desar no l’exigeix). Mateix criteri que al perfil. Policy `AdminExemptPrivacyConsentPolicy` (`isAdmin` → consentiment no requerit).

**Estat:** FET (2026-09-04 23:52 CEST).

---

## 2026-09-04 21:05 CEST

**Àmbit:** Admin · Usuaris · combo Rol · ZUP-112 (en curs)

**Què es veu:** el desplegable de **Rol** (detall / modificar) no té el mateix estil que els altres combos del formulari.

**Millora:** igualar l’aspecte del combo de rol amb la resta de llistes desplegables. **No implementar ara.**

**Estat:** pendent; no implementar ara.

---

## 2026-09-04 20:54 CEST

**Àmbit:** Login / alta d’usuari · activació de compte · ZUP-110 (en curs)

**Què es veu:** un compte creat (p. ex. des d’admin) pot entrar de seguida amb email i contrasenya. No hi ha pas d’activar l’email.

**Millora:** activació **via email**. Si el compte **no està actiu**, no pot iniciar sessió. En intentar-ho, una **notificació d’avís** ha d’explicar que cal activar el correu (enllaç o instrucció d’activar via email). Relacionat amb el punt previst de confirmació de compte per email (funcional §8). **No implementar ara.**

**Estat:** pendent; no implementar ara.

---

## 2026-09-02 18:50 CEST

**Àmbit:** Contacte (`/contacte`) · copy · ZUP-097

**Què es veu:** textos de fase / intern: «peça provisional», «aquesta fase», «resposta simulada», correus `.fake`. No són copy de producte per a l’usuari.

**Millora:** reescriure els textos (hero, canal recomanat i targetes) perquè expliquin de forma clara com contactar (suport, col·laboracions, noves ciutats), sense llenguatge de prototip. Relacionat amb el redisseny de Contacte (entrada 2026-08-31, ZUP-013). **No implementar ara.**

**Estat:** pendent; no implementar ara.

---

## 2026-09-02 18:27 CEST

**Àmbit:** Ajuda (`/ajuda`) · Dubtes habituals · ZUP-092 **KO**

**Què es veu:** tres targetes (Accés, Favorits, Mapa) que expliquen **estat intern de fase** (abans: login fake, favorits no persistits). No són dubtes d’usuari; el copy caduca i no ajuda a usar el producte.

**Millora:** canviar els textos i **fer l’apartat diferent**: dubtes reals (com guardar un lloc, per què no surt un pin, com filtrar…) o un altre format. El recorregut en tres passos i els enllaços a Llocs / Favorits / Contacte es queden. **No implementar ara.**

**Estat:** pendent; no implementar ara.

---

## 2026-09-02 11:51 CEST

**Àmbit:** Llocs · llistat (`/places`) · **important**

**Referència visual:** scroll tipus Sport.es («qui és qui»): en baixar, cada local és un **bloc ample** foto gran + text (sovint foto i text intercanviats), sense requadre tipus targeta petita.

**Millora:** el llistat de locals amb aquest ritme editorial (fitxes que van sortint en fer scroll). El **seleccionat** (pin verd / targeta activa) s’ha d’acabar d’encaixar en el mateix llenguatge visual (destacat similar, no una caixa a part). Mapa i paginació 20: es decideix en implementar (el mapa no s’ha de perdre de vista).

**Estat:** pendent; **important**; no implementar ara.

---

## 2026-09-01 23:47 CEST

**Àmbit:** Llocs · llistat (`/places`) i detall (`/places/:id`)

**Què queda OK ara:** portada JPEG (placeholder si falta), paginació 20, mapa = visibles, selecció pin/targeta, filtres que s’apliquen amb **Cercar**, tres apartats a la fitxa, Details/Photos i web oficial només per confirmar xips (p. ex. terrassa). Tasca d’aquest tram: **OK**, es tanca.

**Millora (interessant, no ara):** per cada combinació de filtre, **recordar quants resultats s’han arribat a mostrar**. Exemple: un filtre arrenca amb 20; l’usuari amplia a 40 i després a 60. Si **torna a fer el mateix filtre**, ha de veure **els mateixos 60**, no tornar a 20. Un filtre nou (o Netejar) torna a 20. Acotada: també una volta lleugera de llistat/detall (polir, no redissenyar).

**Estat:** pendent; no implementar ara.

---

## 2026-08-31 23:57 CEST

**Àmbit:** detall de lloc (`/places/:id`) · Què hi trobaràs · ZUP-058 **KO**

**Què es veu:** la secció surt **buida** (caixa blanca). No hi ha característiques a la BD (sovint llocs Google/caché).

**Millora (producte):** si no tenim dades, **consultar la web / detall del local** i omplir aquí el que cal per decidir si hi vas amb mascota, p. ex.:

- gos **dins** del local
- **només terrassa**
- **no es permet**
- altres dades útils del mateix tipus

No deixar la targeta buida. No implementar ara (cal font: Places Details, web del lloc, o alta manual).

**Estat:** **FET** (2026-09-01): llistat paginat 20, fitxes verticals, requadres buits amagats, Details/fotos amb caché 30 dies quan Google està actiu. ZUP-058: retest USER quan n’hi hagi característiques (Development sense Google pot continuar sense xips).

---

## 2026-08-31 23:54 CEST

**Àmbit:** detall de lloc (`/places/:id`) · Abans d’anar-hi · Política pet · ZUP-057

**Què es veu:** a **Política pet** surt text tècnic (`Google Places (cache)`), no una política de mascotes. L’usuari no ho ha de veure.

**Millora:** no mostrar procedència/caché Google com a política pet. Només política real (gossos/gats, notes). Si no n’hi ha, no mostrar la línia (o un text neutre), mai «Google Places (cache)».

**Estat:** **FET** (2026-09-01): el DTO ja no envia procedència/caché com a política pet; la línia s’amaga si no hi ha política real.

---

## 2026-08-31 23:51 CEST

**Àmbit:** Llocs (`/places`) · mapa · selecció · ZUP-054

**Què es veu:** al mapa només es pot tenir **un** lloc seleccionat. Amb un sol pin, «Treure selecció» aporta poc.

**Millora:** poder seleccionar **n** pins alhora (comparar uns quants llocs). «Treure selecció» treu tot el grup (o un a un). El pin no s’esborra del mapa, només el focus.

**Estat:** pendent; no implementar ara.

---

## 2026-08-31 17:53 CEST

**Àmbit:** Contacte (`/contacte`) · login · ZUP-013

**Què es veu:** des del login, «Demana usuari o recuperació de contrasenya» obre `/contacte`. La pàgina **carrega**, però **no s’entén** i **no agrada**: és una fitxa genèrica (partnerships, feedback, ciutats, correus `.fake`), no una via per demanar usuari ni recuperar contrasenya.

**Millora:** redissenyar Contacte perquè sigui clara i útil. El text del login i el que trobes a la pàgina han de coincidir (demanar accés / recuperar contrasenya, o canviar l’enllaç si Contacte és una altra cosa).

**Estat:** pendent; no implementar ara.

---

## 2026-08-31 17:31 CEST

**Àmbit:** accés denegat · `/permissions` (i vistes només ADMIN) · USER · ZUP-151

**Què es veu:** USER obre `/permissions` i **se’n va a Inici**. El criteri de la prova (no accedir) es compleix.

**Millora:** l’avís d’accés restringit ha de ser **clarament visible** (toast que es noti, o missatge a Inici). Ara és fàcil no veure’l i sembla només una redirecció silenciosa.

**Estat:** pendent; no implementar ara.

---

## 2026-08-30 23:48 CEST

**Àmbit:** procés · proves manuals Chrome + USER · refactor

**Acordat:** la **refactorització de codi** (SOLID, patrons, neteja estructural) es fa **després** de tancar el test / el filtre **USER**, no a mig prova.

**Per què:** durant USER cal estabilitat per retestar (ZUP-076 i següents). Refactor a mig test barreja comportament i estructura.

**Estat:** anotació de procés; aplicar a partir d’ara.

---

## 2026-08-30 23:50 CEST

**Àmbit:** perfil (`/perfil`) · requadre de camps obligatoris · resum «Falten: …»

**Millora:** fer el **summary espectacular** (UX visual; no només una línia de text).

**Estat:** pendent; no implementar ara.

---

## 2026-08-30 21:07 CEST

**Àmbit:** Perfil (`/perfil`) · USER · email i contrasenya

**Què es veu (ZUP-076 KO):** la fitxa carrega (avatar, email visible, rol, formulari), però **no es pot canviar l’email ni la contrasenya**.

**Millora / correcció:** al perfil USER, poder **canviar email** i **canviar contrasenya** (contrasenya actual + nova + confirmació).

**Estat:** implementat (`PUT /api/users/{id}/account`, camps al formulari de `/perfil`). Retest ZUP-076 OK (31-08-2026, Chrome/USER). **FET**

---

## 2026-08-30 20:58 CEST

**Àmbit:** proves per navegador · E2E (després de Chrome manual)

**Acordat:** Chrome es fa **manual** (joc Excel, tots els rols). La resta de navegadors no es repetiran a mà fila a fila.

**Millora (primera a fer quan tanquem les proves manuals Chrome):** E2E per **cada navegador** de l’Excel (motor/perfil real, no “tot és Chromium”). Brave ja ha fallat; Edge també s’ha de cobrir. Smoke manual curt a Brave i Edge (login, llocs, mapa, favorits, perfil). IE 11: N/A.

**Prioritat:** **la primera** del backlog de millores, just després de les proves manuals Chrome.

**Estat:** pendent; no implementar ara.

---

## 2026-08-30 20:23 CEST

**Àmbit:** Favorits (`/favorites`) · botó de la targeta

**Què es veu:** a Favorits, el botó de baix a la dreta de la targeta diu **Guardat**.

**Millora:** en aquesta pantalla hauria de dir **Treure** (o equivalent), perquè l’acció és treure el lloc de favorits, no “guardar-lo” de nou.

**Estat:** **FET** (2026-09-02): el botó actiu diu **Treure** (mateix text a Llocs, Favorits i detall).

---

## 2026-08-29 00:06 CEST

**Àmbit:** detall de lloc (`/places/:id`) · imatge de portada

**Què es veu (ZUP-055):** la fitxa carrega (nom, descripció, mètriques), però **la imatge no surt perquè el lloc no en té** (`CoverImageUrl` buit). El detall pinta sempre un `<img>`; a les targetes del llistat ja hi ha placeholder si no hi ha URL.

**Millora:** al detall (i llocs relacionats, si cal), mateix criteri que la targeta: si no hi ha imatge, mostrar placeholder, no un `img` buit/trencat.

**Estat:** placeholder «Imatge no disponible» a la fitxa i als relacionats. No s’havia d’implementar durant les proves (només anotar); s’ha fet per error de l’agent. **FET**

---

## 2026-08-26 23:12 CEST

**Àmbit:** detall de lloc des del mapa (`/places/:id`) · UX navegació

**Què funciona:** des de Llocs, amb selecció al mapa, «Veure detall» obre la fitxa (ZUP-053 OK).

**Millora:** el comportament/UX de la fitxa quan s’arriba des del mapa no acaba d’encaixar (p. ex. imatge trencada/placeholder, botó «Has arribat des del mapa», layout/accions). Revisar flux mapa → detall → tornar al llistat perquè sigui més clar i coherent. Relacionat també: «Veure detall» al popup del marcador (ara només a la targeta sota el mapa).

**Estat:** pendent; no implementar ara.

---

## 2026-08-26 23:08 CEST

**Àmbit:** mapa · popup del marcador (Leaflet)

**Què es veu:** al clicar un marcador, el popup mostra tipus, nom, ciutat/país i adreça, però el text d’adreça surt **redundant** (p. ex. CP/ciutat/país repetits a la mateixa línia).

**Millora:** netejar el text del popup (i, si cal, la targeta «Seleccionat al mapa») perquè l’adreça sigui llegible i sense duplicats. Acordat: **ja es canviarà** (no bloqueja proves ZUP-052).

**Estat:** pendent; no implementar ara.

---

## 2026-08-26 22:39 CEST

**Àmbit:** cerca de llocs / Google Places / Development / proves manuals

**Acord de producte (pendent d’implementar en commit propi, després de ZUP-046 / proves):**

1. **Development / proves:** per defecte **només consulta BD** (catàleg + snapshots). Sense Text Search Google mentre es proven pantalles (evitar quota).
2. Caché de cerca + caché de local (`place_id`, ~30 dies) com ja documentat a funcional §12.5.1; Google no filtra pels nostres IDs — l’estalvi és no cridar.
3. Quan Google torni a ser actiu (no-dev o flag): BD primer; si falta cobertura → Text Search; descartar `place_id` ja existents; upsert només nous. Refresh caducat → Places Details per `place_id`.
4. Traça/control de crides Google vs hits de caché: millora futura (ara només `place_search_queries`).

**Estat:** acordat; **Development aplicat** (`GooglePlaces:Enabled=false`, `PreferExternalSearchFirst=false` a `appsettings.Development.json` + guard al Text Search). Place Details / Photos per portada (pàgina visible + fitxa) **implementat** (2026-09-01), sense activar Text Search. Híbrid BD+nous via Text Search: pendent després de proves USER.

---

## 2026-08-13 20:58 CEST

**Àmbit:** cerca de llocs / Google Places / tipus `Bar`

**Millora:** quan es filtren o cerquen **bars**, ara surten resultats però en queden **pocs** (sobretot bars). La consulta externa limita a ~15, no usa el `type=bar` real de Google (només afegeix el text «Bar» + «pet friendly») i no paginem amb `next_page_token`. Valorar: filtre `type` de l’API, pujar límit (p. ex. 20) i/o segona pàgina de resultats; i millorar el mapeig de tipus Google → `PlaceType` de Zuppeto.

**Estat:** pendent de comentar / prioritzar (no implementar de moment; les proves manuals segueixen anotant el bàsic que funciona).

---

## 2026-08-13 21:02 CEST

**Àmbit:** mapa de llocs / marcadors (rodones)

**Millora:** més endavant substituir les **rodones** actuals dels marcadors per un objecte o imatge més xula, p. ex. un **Westie** petit (icona/il·lustració petita coherent amb la marca pet-friendly). Mantenir llegibilitat al zoom i estat seleccionat/hover.

**Estat:** proposta UX; pendent de comentar / prioritzar (no implementar de moment).

---

## 2026-08-13 21:03 CEST

**Àmbit:** fitxa / llistat de llocs · textos de descripció

**Què funciona ara:** la cerca i el llistat mostren llocs amb una descripció bàsica (ara mateix, per candidats Google, sol ser un text genèric tipus «Resultat Google Places · {nom}», amb adreça i dades mínimes). El flux és operable per proves manuals.

**Millora:** enriquir la **descripció** (i, si cal, la descripció curta / popup del mapa) amb **més detall** útil per a l’usuari: ambient, política pet quan es conegui, tipicitat del local, barri, etc. Fonts possibles: Places Details / camps addicionals de Google, contingut editorial propi, o generació assistida (p. ex. Gemini) amb revisió. Objectiu: que el bàsic segueixi funcionant, però el text deixi de ser stub i aporti valor de producte.

**Estat:** pendent de comentar / prioritzar (no implementar de moment; a proves anotar OK del bàsic).

---

## 2026-08-23 12:42 CEST

**Àmbit:** Excel de proves manuals (`MAIN_PROBES_ZUPETTO.xlsx`)

**Millora / criteri:** els textos de les proves (pantalla, descripció, passos, resultat esperat) han de ser **sempre llegibles per qualsevol persona**, sense argot de programador (`Hero`, `CTA`, `OAuth`, `header`, `Leaflet`, etc.). S’ha netejat l’Excel viu i el generador aplica el mateix criteri.

**Estat:** aplicat a l’Excel (backup `20260823-004`); mantenir el criteri en noves proves. **FET**
