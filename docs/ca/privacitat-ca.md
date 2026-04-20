# Política de privacitat i dades (YepPet)

> **Estat:** esborrany per consolidar a la **Fase V** juntament amb la revisió profunda de privadesa i compliment normatiu previstos a `docs/project-phases.md`. Fins al tancament d’aquest punt, el text legal visible a l’usuari ha de seguir el criteri de direcció de projecte i, si cal, revisió jurídica.

## 1. Propòsit

Centralitzar aquí, quan estigui madur, la informació que l’usuari ha de conèixer sobre tractament de dades, proveïdors externs i drets. Aquest fitxer **no** substitueix el document funcional de criteri de producte (`funcional-ca.md`) ni el detall tècnic (`tecnic-ca.md`).

## 2. Integracions geogràfiques (GeoNames) — apunt per a la Fase V

Quan el producte consulti **GeoNames** (o serveis equivalents acordats) per suggerir **països** o **ciutats** abans de desar-los al catàleg YepPet:

- **Llicència de les dades GeoNames:** les dades del projecte GeoNames es publiquen sota **Creative Commons Attribution 4.0** (CC-BY 4.0). Cal **atribució** a GeoNames (enllaç o referència clara) en la documentació pública, la política de privacitat o la pàgina “Sobre el projecte”, segons el que fixi la versió final d’aquest document i la revisió legal.
- **Ús comercial:** el marc GeoNames permet ús comercial amb les condicions d’atribució i les seves condicions generals; la versió final d’aquest text ha de reflectir-ho sense contradir la normativa aplicable (UE, etc.).
- **Minimització:** les peticions al servei han de fer-se **des del backend**; només s’ha d’enviar el **mínim necessari** per obtenir suggeriments (p. ex. fragment de text després del llindar funcional, filtre de país quan escaigui). El detall d’implementació queda al `tecnic-ca.md`.
- **Persistència:** el que es guarda a la base de dades YepPet és el **catàleg propi** (`countries` / `cities`), no una còpia integral de bases externes; vegeu `funcional-ca.md` (§3.15.2 i §3.16).

A la **Fase V** s’ha d’**ampliar** aquest apartat amb: finalitats del tractament, base legal, destinataris, transferències fora de la UE si escau, terminis, drets dels interessats i qualsevol altre punt exigit per la normativa vigent i el criteri de direcció.

## 3. Referències

- `docs/project-phases.md` — Fase V (revisió de privadesa i compliment).
- `docs/ca/funcional-ca.md` — §3.15.1, §3.15.2, §3.16 (catàleg territorial i proveïdors).
- `docs/ca/tecnic-ca.md` — implementació, claus, caché (quan es tanqui el disseny).
