# Millores pendents (CA)

Backlog de millores detectades mentre es treballa o es fan proves manuals.
No substitueix `docs/project-phases.md` ni el joc de proves Excel: aquí només anotem idees per comentar i prioritzar després.

**Format d’entrada:** data i hora (Europe/Madrid), després la descripció.

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

**Estat:** acordat; **Development aplicat** (`GooglePlaces:Enabled=false`, `PreferExternalSearchFirst=false` a `appsettings.Development.json` + guard al servei/provider). Híbrid BD+Google i Places Details: pendent després de proves USER.

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

**Estat:** aplicat a l’Excel (backup `20260823-004`); mantenir el criteri en noves proves.
