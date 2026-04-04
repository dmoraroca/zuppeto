# Joc de proves (CA)

## 1. Objectiu

Aquest document recull el joc de proves manual de YepPet en format separat de la documentacio funcional i tecnica.

El seu objectiu es:

- validar els fluxos principals sense barrejar-los amb la resta de documentacio
- deixar una base simple per proves de regressio
- poder ampliar casos i resultat esperat a mesura que avancin les fases

## 2. Criteri d'us

Aquest fitxer es fa servir com a checklist manual de validacio.

Format recomanat d'execucio:

- cas de prova
- prerequisit
- passos
- resultat esperat
- resultat obtingut

## 3. Prerequisits

- stack local aixecada amb `db`, `api` i `web`
- `http://localhost:4200` operatiu
- `http://localhost:5211` operatiu
- base de dades actualitzada amb migracions `EF`
- usuaris de desenvolupament disponibles:
  - `admin@admin.adm / Admin123`
  - `user@user.com / Admin123`

## 4. Joc de proves actual

### 4.1 Autenticacio

#### JP-001 · Login propi correcte

- prerequisit: usuari existent
- passos:
  - obrir `/login`
  - informar `email` i `password` correctes
  - clicar `Iniciar sessio`
- resultat esperat:
  - es crea sessio valida
  - l'usuari entra a l'aplicacio

#### JP-002 · Login propi incorrecte

- prerequisit: cap
- passos:
  - obrir `/login`
  - informar credencials incorrectes
  - clicar `Iniciar sessio`
- resultat esperat:
  - no es crea sessio
  - es manté a login amb feedback d'error

#### JP-003 · Login Google

- prerequisit: `ClientId` Google configurat
- passos:
  - obrir `/login`
  - clicar `Iniciar amb Google`
  - completar login federat
- resultat esperat:
  - es crea sessio valida
  - es recupera l'usuari correcte

#### JP-004 · Login LinkedIn

- prerequisit: `ClientId` i `ClientSecret` LinkedIn configurats
- passos:
  - obrir `/login`
  - clicar `Iniciar amb LinkedIn`
  - completar login federat
- resultat esperat:
  - es crea sessio valida
  - l'usuari torna a YepPet sense error federat

#### JP-005 · Logout

- prerequisit: sessio activa
- passos:
  - clicar `Sortir`
- resultat esperat:
  - la sessio desapareix
  - l'usuari torna a `/login`

### 4.2 Rols i permisos

#### JP-010 · Usuari nou per defecte

- prerequisit: login nou via propi o federat
- passos:
  - crear o fer entrar un usuari que no existeixi encara
- resultat esperat:
  - el rol inicial queda com `VIEWER`

#### JP-011 · `ADMIN` continua sent administrador

- prerequisit: usuari `admin@admin.adm`
- passos:
  - entrar amb `admin@admin.adm / Admin123`
- resultat esperat:
  - l'usuari queda identificat com `ADMIN`
  - veu el menu `ADMIN`

#### JP-012 · Menu `ADMIN` per a `ADMIN`

- prerequisit: sessio `ADMIN`
- passos:
  - obrir qualsevol pantalla autenticada
  - desplegar el menu `ADMIN`
- resultat esperat:
  - apareixen com a minim:
    - `Documentacio`
    - `Usuaris`
    - `Permisos`

#### JP-013 · Menu `ADMIN` per a `DEVELOPER`

- prerequisit: usuari amb rol `DEVELOPER`
- passos:
  - entrar amb sessio `DEVELOPER`
  - desplegar `ADMIN`
- resultat esperat:
  - el menu existeix
  - com a minim mostra `Documentacio`
  - no mostra manteniments reservats a `ADMIN`

#### JP-014 · `USER` sense accés a `.md`

- prerequisit: usuari amb rol `USER`
- passos:
  - entrar amb sessio `USER`
  - intentar accedir a documentacio interna
- resultat esperat:
  - no veu l'opcio `Documentacio`
  - no pot entrar a rutes internes de documentacio

#### JP-015 · `VIEWER` només lectura

- prerequisit: usuari amb rol `VIEWER`
- passos:
  - entrar amb sessio `VIEWER`
  - navegar per `Inici`, `Llocs`, `Favorits` i `Perfil`
- resultat esperat:
  - pot entrar a les pantalles funcionals
  - no pot modificar res

#### JP-016 · `VIEWER` no pot modificar favorits

- prerequisit: usuari amb rol `VIEWER`
- passos:
  - anar a `Llocs` o `Favorits`
  - intentar afegir o eliminar un favorit
- resultat esperat:
  - accio denegada o no disponible
  - no hi ha canvi persistent

#### JP-017 · `VIEWER` no pot modificar perfil

- prerequisit: usuari amb rol `VIEWER`
- passos:
  - anar a `Perfil`
  - intentar guardar canvis
- resultat esperat:
  - accio denegada o no disponible
  - no hi ha canvi persistent

#### JP-018 · `USER` pot operar normalment

- prerequisit: usuari amb rol `USER`
- passos:
  - entrar amb sessio `USER`
  - usar `places`, `favorites` i `perfil`
- resultat esperat:
  - pot fer servir el producte funcional
  - no veu el menu `ADMIN`

#### JP-019 · `ADMIN` pot assignar rol

- prerequisit: sessio `ADMIN`
- passos:
  - entrar a manteniment d'usuaris
  - assignar un rol diferent a un usuari
- resultat esperat:
  - el canvi queda guardat a BBDD

### 4.3 Navegacio i proteccio

#### JP-030 · Ruta protegida sense sessio

- prerequisit: cap sessio activa
- passos:
  - intentar obrir una ruta protegida directament
- resultat esperat:
  - redireccio a `/login`

#### JP-031 · Accés intern sense permís

- prerequisit: sessio autenticada sense permís intern
- passos:
  - obrir una ruta `admin/*` no permesa
- resultat esperat:
  - acces denegat
  - redireccio fora de la zona interna

### 4.4 Persistencia

#### JP-040 · API operativa

- prerequisit: stack aixecada
- passos:
  - consultar `http://localhost:5211/health/db`
- resultat esperat:
  - resposta `200 OK`

#### JP-041 · Sessio amb permisos

- prerequisit: qualsevol login correcte
- passos:
  - iniciar sessio
  - revisar el payload de sessio retornat per l'API
- resultat esperat:
  - la sessio inclou `permissionKeys`

## 5. Criteri d'ampliacio

Quan es tanqui el punt de `rols i permisos`, aquest fitxer s'haura d'ampliar amb:

- proves de manteniment d'usuaris
- proves de manteniment de permisos
- proves de documentacio interna `.md`
- proves de restriccio per opcio de menu, pagina i accio

## 6. Estat

- punt cobert actualment: `autenticacio` i base de `rols i permisos`
- fitxer viu: s'ha d'anar ampliant a cada tram funcional nou

## 7. Historic d'execucions

L'historic d'execucions automatitzades es guarda a `docs/probes-e2e-resultats/` amb format:

- `YYYYMMDD_HHMM_OK_<punt>_<commit>.md`
- `YYYYMMDD_HHMM_KO_<punt>_<commit>.md`
