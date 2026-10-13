# ADR-002 — Identitat, procedència per camp i frontera ODbL

## Estat

Acceptat tècnicament per al disseny. La qualificació jurídica final i l'aprovació de cada font són gates humans previs a qualsevol ingesta real.

## Context

PetiLoc combinarà dades manuals i possibles aportacions de fonts amb llicències diferents. OpenStreetMap està subjecte a l'ODbL; separar taules físicament no garanteix per si sol que dues bases siguin col·lectives i no derivades. També cal impedir que una observació automàtica substitueixi una decisió manual verificada.

## Decisió

- `Place` tindrà una identitat interna estable, independent dels identificadors de font.
- Les referències externes es conservaran en registres de font separats i únics per `(source_id, external_id)`.
- Cada valor candidat conservarà procedència, instant d'obtenció, llicència aplicable, confiança i estat de revisió.
- L'autoritat serà `MANUAL_VERIFIED > MANUAL_UNVERIFIED > AUTO`; cap importació modificarà silenciosament un valor manual.
- Les observacions ODbL i les transformacions que en depenguin tindran una frontera lògica i exportable, documentada al registre de font.
- Els camps independents aportats per altres fonts mantindran evidència pròpia. No es declararà que una simple separació física elimina obligacions ODbL.
- Qualsevol exportació o producte visible aplicarà l'atribució i, quan correspongui, les obligacions de share-alike determinades pel gate legal.

## Conseqüències

- Es pot explicar l'origen i l'autoritat de cada camp publicat.
- Les rectificacions, baixes de font i reprocessaments són auditables.
- El model és més ric que una columna genèrica `source`, però evita perdre drets, evidència i decisions manuals.
- Les regles d'exportació dependran de la composició real de les dades i no d'una etiqueta optimista.

## Alternatives descartades

- Una procedència única per lloc: és insuficient quan nom, coordenades, admissió i fotografia provenen de fonts diferents.
- Copiar directament tags OSM a `places`: dilueix la frontera ODbL i la revisió.
- Fer prevaldre sempre la dada més nova: podria sobreescriure verificacions humanes correctes.

## Criteris de verificació futura

- Tot camp publicable pot reconstruir font, data, llicència i decisió d'autoritat.
- Una observació automàtica no sobreescriu una dada manual protegida.
- Es pot identificar i exportar el subconjunt subjecte a ODbL.
- Cap font entra en producció sense fitxa documental aprovada.

