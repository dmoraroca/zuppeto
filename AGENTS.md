# Instruccions de treball de Zuppeto

## Presentació de proves manuals

Quan es recuperi o s'activi una prova de l'Excel, cal presentar-la sempre de manera clara i guiada amb aquesta estructura:

1. Títol: `Nova prova en curs — CODI`.
2. Nom o descripció funcional de la prova.
3. Dades bàsiques en una llista: pantalla o ruta, navegador, rol i prioritat.
4. Apartat `Passos`, amb instruccions numerades, concretes i fàcils de seguir.
5. Apartat `Resultat esperat`, amb una llista explícita de tot el que l'usuari ha de comprovar.

No s'han de copiar simplement els camps breus de l'Excel: cal transformar-los en un procediment pas a pas que s'entengui sense conèixer prèviament l'aplicació.

Quan hi hagi una prova activa i l'usuari respongui `ok`, cal interpretar-ho sempre com el tancament satisfactori de la prova: marcar-la `OK` a l'Excel, posar el primer registre `PENDENT` del flux actual com a `EN CURS` i presentar immediatament la nova prova completa amb el format anterior. No s'ha d'esperar que l'usuari demani la prova següent.

Quan s'acabin les proves d'un rol, cal indicar que el rol ha quedat complet, llistar les proves pendents del rol següent, posar-ne la primera `EN CURS` i presentar-la completa pas a pas.

Quan una prova es tanqui com a `OK`, cal marcar com a `EN CURS` el primer registre `PENDENT` del flux corresponent i presentar-lo immediatament amb el format anterior. Quan s'acabin les proves d'un rol, cal llistar les pendents del rol següent.

## Textos per a Git

Els textos proposats per a Git s'han de donar sempre com a text pla, sense blocs de codi Markdown ni accents greus envoltant el text.

## Arquitectura i qualitat

Qualsevol implementació del projecte, inclosa l'automatització E2E, ha de respectar fidelment **DDD** i **SOLID**.

- Mantenir el llenguatge ubic del domini (`Place`, `Favorite`, `Viewer`, `Permission` i els casos ZUP).
- Separar domini, aplicació i infraestructura, evitant dependències del domini cap a Angular, Playwright, HTTP, PostgreSQL o altres detalls tècnics.
- Aplicar inversió de dependències mitjançant ports, contractes i adaptadors quan correspongui.
- Donar una única responsabilitat clara a components, serveis, escenaris i helpers.
- Evitar fitxers gegants, duplicació entre navegadors i herències innecessàries.
- Als E2E, mantenir les especificacions primes i expressades com a comportament funcional; encapsular Playwright, API, dades i navegadors en fixtures o adaptadors reutilitzables.
- Aplicar aquests principis amb proporcionalitat i sense sobrearquitecturar solucions senzilles.

## Documentació de l'automatització E2E

El document `docs/ca/e2e-automatitzacio-ca.md` és el registre viu de l'automatització E2E. Cada canvi material en aquesta iniciativa ha d'actualitzar-lo amb l'estat, les decisions, l'estructura, els navegadors afectats i els resultats obtinguts.

Tant `docs/ca/e2e-automatitzacio-ca.md` com `docs/ca/millores-pendents-ca.md` han d'estar disponibles al catàleg de Documentació interna de Zuppeto.
