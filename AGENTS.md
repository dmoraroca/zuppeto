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
