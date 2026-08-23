/**
 * Generates the Zuppeto manual test workbook (all screens).
 * Run: node scripts/generate-probes-excel.mjs
 * Requires exceljs (installed via npm in repo or EXCELJS_PATH env).
 *
 * Copy rule: every user-facing cell (screen, description, steps, expected)
 * must be readable by a non-programmer. No developer jargon (Hero, CTA, OAuth, etc.).
 */
import ExcelJS from 'exceljs';
import { fileURLToPath } from 'url';
import path from 'path';
import fs from 'fs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/** Keep probe copy free of programmer jargon for manual testers. */
function plainLanguageForTesters(input) {
  if (typeof input !== 'string') return input;
  let t = input.replace(/\/auth\/callback/g, '<<<AUTHCB>>>');
  const pairs = [
    [/Auth callback \(\/auth\/callback\)/gi, 'Retorn del login (/auth/callback)'],
    [/Callback OAuth Google\/LinkedIn \(flux complet\)/gi, 'Retorn del login amb Google o LinkedIn (flux complet)'],
    [/Callback OAuth sense paràmetres/gi, 'Retorn del login sense dades del proveïdor'],
    [/Guest guard:\s*/gi, "Control d'accés: "],
    [/Hero principal/gi, 'Franja principal de la portada'],
    [/Chips del hero/gi, 'Etiquetes de la franja principal'],
    [/Top favorits al hero/gi, 'Favorits destacats a la franja principal'],
    [/CTAs cap a places i favorits/gi, 'Botons cap a llocs i favorits'],
    [/CTA Veure com funciona/gi, 'Botó «Veure com funciona»'],
    [/CTA Anar a llocs/gi, 'Botó «Anar a llocs»'],
    [/CTA «Anar a llocs»/gi, 'Botó «Anar a llocs»'],
    [/CTA «Revisar favorits»/gi, 'Botó «Revisar favorits»'],
    [/Peu de pàgina \(footer\)/gi, 'Peu de pàgina'],
    [/Dropdown de compte/gi, 'Menú del compte'],
    [/Filtres: ciutat \(combobox\)/gi, 'Filtres: ciutat (llista desplegable)'],
    [/Ciutat al combobox de places/gi, 'Ciutat a la llista desplegable de llocs'],
    [/Spotlight últim guardat/gi, "Destacat de l'últim guardat"],
    [/Place cards amb/gi, 'Targetes de lloc amb'],
    [/journey cards/gi, 'targetes del recorregut'],
    [/Info cards/gi, 'Targetes informatives'],
    [/info cards/gi, 'targetes informatives'],
    [/Títol hero,/gi, 'Títol de la portada,'],
    [/chips pet-friendly i CTAs/gi, 'etiquetes pet-friendly i botons'],
    [/un chip del hero/gi, 'una etiqueta de la franja principal'],
    [/enllaços del hero/gi, 'enllaços de la franja principal'],
    [/del hero/gi, 'de la franja principal'],
    [/al hero/gi, 'a la franja principal'],
    [/revisar hero/gi, 'revisar la franja principal'],
    [/Footer visible/gi, 'Peu de pàgina visible'],
    [/al header amb/gi, 'a la capçalera amb'],
    [/revisar header/gi, 'revisar la capçalera'],
    [/Header es refresca/gi, 'La capçalera es refresca'],
    [/visible al header\/compte/gi, 'visible a la capçalera i al menú del compte'],
    [/visible al header/gi, 'visible a la capçalera'],
    [/Text actualitzat al header/gi, 'Text actualitzat a la capçalera'],
    [/desapareix del header/gi, 'desapareix de la capçalera'],
    [/actualitzat al header/gi, 'actualitzat a la capçalera'],
    [/Mapa Leaflet/gi, 'Mapa'],
    [/mapa Leaflet/gi, 'mapa'],
    [/Redirecció OAuth o/gi, 'Redirecció al proveïdor de login o'],
    [/Completar login federat des del proveïdor/gi, 'Completar el login amb Google o LinkedIn'],
    [/flux de places \(guest\/login segons estat\)/gi, "cerca de llocs (amb o sense sessió, segons l'estat)"],
    [/sense trencar la UI/gi, 'sense trencar la pantalla'],
    [/query params de filtre/gi, "filtres aplicats a l'adreça de la pàgina"],
    [/des de l'API amb caché/gi, 'des del servidor (amb memòria de cerca)'],
    [/coordenades del backend/gi, 'coordenades del lloc'],
    [/Panell\/modal detall/gi, 'Panell o finestra de detall'],
    [/al combobox/gi, 'a la llista desplegable'],
    [/coherent amb comboboxs/gi, 'coherent amb les llistes desplegables'],
    [/provar combobox a \/places/gi, 'provar la llista de ciutats a /places'],
    [/Badge\/indicador/gi, 'Indicador'],
    [/Empty state «/gi, 'Pantalla buida «'],
    [/chips i canal/gi, 'etiquetes i canal'],
    [/chips de filtres/gi, 'etiquetes de filtres'],
    [/lazy load/gi, 'càrrega sota demanda'],
    [/dropzone/gi, 'zona per deixar el fitxer'],
    [/relogin/gi, 'tornar a iniciar sessió'],
    [/\bCTAs?\b/g, 'botons'],
    [/\bHero\b/g, 'franja principal'],
    [/\bfooter\b/gi, 'peu de pàgina'],
    [/\bheader\b/gi, 'capçalera'],
    [/\bcombobox\b/gi, 'llista desplegable'],
    [/\bmodal\b/gi, 'finestra'],
    [/\bOAuth\b/g, 'login amb proveïdor extern'],
    [/\bLeaflet\b/g, 'mapa'],
    [/\bUI\b/g, 'pantalla'],
    [/\bAPI\b/g, 'servidor'],
    [/\bcallback\b/gi, 'retorn del login'],
    [/\btoast\b/gi, 'missatge emergent'],
  ];
  for (const [re, rep] of pairs) t = t.replace(re, rep);
  t = t.replace(/<<<AUTHCB>>>/g, '/auth/callback');
  t = t.replace(/\/auth\/retorn del login/gi, '/auth/callback');
  return t;
}

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function findRepoRoot(startDir) {
  let dir = startDir;
  while (dir !== path.dirname(dir)) {
    if (fs.existsSync(path.join(dir, 'docs', 'project-phases.md'))) {
      return dir;
    }
    dir = path.dirname(dir);
  }
  return path.resolve(startDir, '..');
}

const repoRoot = findRepoRoot(__dirname);

function todayYyyymmdd() {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}${m}${day}`;
}

const outputPath = path.join(
  repoRoot,
  'docs/probes-e2e-resultats',
  `${todayYyyymmdd()}_proves_zuppeto_pantalles.xlsx`
);

/** @type {Array<{ screen: string, priority: string, description: string, steps: string, expected: string, role?: string, roles?: string[], multiRole?: boolean }>} */
const rawTests = [
  // ── 1. Login (sense sessió → validació → accés → OAuth → preview → guards) ──
  { screen: 'Login (/login)', priority: 'Alta', description: 'Accés a pantalla de login', steps: 'Anar a /login sense sessió', expected: 'Títol «Torna a entrar a Zuppeto» i formulari email/contrasenya' },
  { screen: 'Login (/login)', priority: 'Alta', description: 'Formulari buit', steps: 'Clic Iniciar sessió sense omplir camps', expected: 'Validació; no es fa petició o es mostra error' },
  { screen: 'Login (/login)', priority: 'Alta', description: 'Credencials incorrectes', steps: 'Email vàlid amb contrasenya errònia', expected: 'Missatge d\'error; no s\'inicia sessió' },
  { screen: 'Login (/login)', priority: 'Alta', description: 'Login ADMIN correcte', steps: 'admin@admin.adm / Admin123 → Iniciar sessió', expected: 'Redirecció a home i sessió ADMIN activa' },
  { screen: 'Login (/login)', priority: 'Alta', description: 'Login USER correcte', steps: 'Usuari USER vàlid → Iniciar sessió', expected: 'Accés al producte sense opcions d\'admin' },
  { screen: 'Login (/login)', priority: 'Mitja', description: 'Proveïdor Google', steps: 'Revisar botó Google a la secció «o continua amb»', expected: 'Botó actiu si configurat; «Google pendent» si no' },
  { screen: 'Login (/login)', priority: 'Mitja', description: 'Proveïdor LinkedIn', steps: 'Clic LinkedIn si disponible', expected: 'Redirecció OAuth o botó desactivat si pendent' },
  { screen: 'Login (/login)', priority: 'Baixa', description: 'Proveïdor Facebook', steps: 'Revisar estat del botó Facebook', expected: 'Desactivat / «Facebook pendent» (no implementat encara)' },
  { screen: 'Login (/login)', priority: 'Mitja', description: 'Explorador públic al login — filtres', steps: 'Provar filtres cerca, ciutat, tipus, mascota', expected: 'Filtres responen; mosaic de llocs mostra dades' },
  { screen: 'Login (/login)', priority: 'Mitja', description: 'Explorador públic al login — mapa', steps: 'Revisar bloc «Mapa inicial»', expected: 'Mapa Leaflet centrat a Espanya sense dades carregades' },
  { screen: 'Login (/login)', priority: 'Mitja', description: 'Obrir cerca des del login', steps: 'Clic «Obrir cerca» amb filtres aplicats', expected: 'Navega cap al flux de places (guest/login segons estat)' },
  { screen: 'Login (/login)', priority: 'Baixa', description: 'Generar rutes (preview)', steps: 'Clic «Generar rutes»', expected: 'Acció de preview sense trencar la UI' },
  { screen: 'Login (/login)', priority: 'Mitja', description: 'Enllaç contacte des del login', steps: 'Clic «Demana usuari o recuperació de contrasenya»', expected: 'Navega a /contacte (públic, sense login)' },
  { screen: 'Login (/login)', priority: 'Alta', description: 'Guest guard: usuari logat no veu login', steps: 'Amb sessió activa anar a /login', expected: 'Redirecció fora del login (p.ex. home)' },

  // ── 2. Auth callback (error → flux complet) ──
  { screen: 'Auth callback (/auth/callback)', priority: 'Mitja', description: 'Callback OAuth sense paràmetres', steps: 'Anar a /auth/callback directament', expected: 'Gestió d\'error o redirecció segura; no pantalla trencada' },
  { screen: 'Auth callback (/auth/callback)', priority: 'Alta', description: 'Callback OAuth Google/LinkedIn (flux complet)', steps: 'Completar login federat des del proveïdor', expected: 'Sessió creada i redirecció al producte' },

  // ── 3. Capçalera (menú → compte → navegació → logout) ──
  { screen: 'Capçalera / Navegació', priority: 'Alta', description: 'Menú principal visible (USER)', steps: 'Login com USER → revisar barra superior', expected: 'Es veuen Inici, Llocs, Favorits i Ajuda segons permisos' },
  { screen: 'Capçalera / Navegació', priority: 'Alta', description: 'Menú ADMIN — Del administrador', steps: 'Login com admin@admin.adm / Admin123', expected: 'Apareix Del administrador amb subopcions internes' },
  { screen: 'Capçalera / Navegació', priority: 'Alta', description: 'Dropdown de compte (rodona)', steps: 'Clic a la rodona del compte', expected: 'Es veuen nom, email, rol, Perfil, Ajuda i Sortir' },
  { screen: 'Capçalera / Navegació', priority: 'Mitja', description: 'Submenú Ajuda des del compte', steps: 'Obrir Ajuda dins la rodona del compte', expected: 'Es mostren Com funciona i Contacta\'ns' },
  { screen: 'Capçalera / Navegació', priority: 'Mitja', description: 'Un sol desplegable obert', steps: 'Obrir Del administrador i després la rodona del compte', expected: 'Nomès queda un desplegable obert cada vegada' },
  { screen: 'Capçalera / Navegació', priority: 'Alta', description: 'Campana de notificacions', steps: 'Revisar icona campana al header amb notificacions pendents', expected: 'Indicador de no llegides i enllaç a /notificacions' },
  { screen: 'Capçalera / Navegació', priority: 'Alta', description: 'Navegació Inici → Llocs → Favorits', steps: 'Clic successiu als enllaços del menú principal', expected: 'Cada pantalla carrega sense error 404 ni pantalla en blanc', multiRole: true },
  { screen: 'Capçalera / Navegació', priority: 'Alta', description: 'Peu de pàgina (footer)', steps: 'Desplaçar-se al final de Home, Llocs i Ajuda', expected: 'Footer visible amb Inici, Llocs, Favorits, Ajuda i Contacta\'ns', multiRole: true },
  { screen: 'Capçalera / Navegació', priority: 'Mitja', description: 'Refresc després de canvis de permisos', steps: 'Canviar permisos/menús com ADMIN → Ctrl+Shift+R', expected: 'Capçalera es reconstrueix correctament' },
  { screen: 'Capçalera / Navegació', priority: 'Alta', description: 'Redirecció sense sessió', steps: 'Sense sessió: accedir a /places o /perfil', expected: 'Redirecció a /login', role: 'Sense sessió' },

  // ── 4. Rols — UX de menú (després del header, abans del producte) ──
  { screen: 'Rols i seguretat', priority: 'Alta', description: 'Login DEVELOPER — menú', steps: 'Login DEVELOPER → revisar header', expected: 'Del desenvolupador (documentació); no Del administrador complet' },
  { screen: 'Rols i seguretat', priority: 'Alta', description: 'Login USER — sense admin', steps: 'Login USER → revisar header', expected: 'Cap opció admin ni documentació interna' },
  { screen: 'Rols i seguretat', priority: 'Alta', description: 'Login VIEWER — només lectura', steps: 'Login VIEWER → provar accions d\'escriptura', expected: 'Navegació funcional sense capacitat de modificació' },

  // ── 5. Inici (càrrega → navegació sortida → contingut) ──
  { screen: 'Inici (/)', priority: 'Alta', description: 'Hero principal', steps: 'Anar a / autenticat', expected: 'Títol hero, chips pet-friendly i CTAs «Explora llocs» / «Entén el flux»', multiRole: true },
  { screen: 'Inici (/)', priority: 'Mitja', description: 'Chips del hero cap a places filtrats', steps: 'Clic a un chip del hero', expected: 'Obre /places amb query params de filtre' },
  { screen: 'Inici (/)', priority: 'Alta', description: 'CTA «Anar a llocs»', steps: 'Clic al botó primari del tancament', expected: 'Navega a /places' },
  { screen: 'Inici (/)', priority: 'Alta', description: 'CTA «Revisar favorits»', steps: 'Clic al botó secundari del tancament', expected: 'Navega a /favorites' },
  { screen: 'Inici (/)', priority: 'Alta', description: 'Top favorits al hero', steps: 'Amb favorits existents revisar hero', expected: 'Fins a 10 llocs favorits destacats al hero' },
  { screen: 'Inici (/)', priority: 'Mitja', description: 'Secció ciutats trending', steps: 'Revisar graella de ciutats', expected: 'Ciutats amb enllaç/navegació cap a cerca' },
  { screen: 'Inici (/)', priority: 'Mitja', description: 'Secció «Com es fa servir» (3 passos)', steps: 'Revisar journey cards 01-03', expected: 'Textos Filtra / Valida al mapa / Guarda i reprèn visibles' },
  { screen: 'Inici (/)', priority: 'Mitja', description: 'Secció Why Zuppeto', steps: 'Desplaçar a bloc de valor', expected: 'Contingut informatiu coherent' },

  // ── 6. Llocs (càrrega → filtres → cerca → llistat → mapa → detall) ──
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Càrrega pantalla llocs', steps: 'Anar a /places', expected: 'Heading «Descobreix… llocs pet-friendly», filtres i mapa visibles', multiRole: true },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Filtres: cerca text', steps: 'Escriure terme al camp cerca', expected: 'Filtre aplicat; chips de filtres actius actualitzats' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Filtres: ciutat (combobox)', steps: 'Seleccionar ciutat al combobox', expected: 'Llista i mapa es filtren per ciutat' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Filtres: tipus de lloc', steps: 'Canviar tipus (hotel, restaurant, etc.)', expected: 'Resultats coherents amb el tipus' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Filtres: política mascota', steps: 'Filtrar gossos / gats / totes', expected: 'Resultats respecten filtre mascota' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Obrir cerca (lazy load)', steps: 'Configurar filtres → «Obrir cerca»', expected: 'Es carreguen resultats des de l\'API amb caché' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Netejar filtres', steps: 'Aplicar filtres → «Netejar» o «Netejar tots»', expected: 'Tots els filtres es reinicien' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Empty state sense resultats', steps: 'Filtres que no retornin res', expected: 'Missatge «No hi ha resultats amb aquest filtre»' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Graella de targetes de lloc', steps: 'Revisar llistat sota el mapa', expected: 'Place cards amb imatge, tipus, favorit' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Toggle favorit des de la graella', steps: 'Clic cor/favorit a una targeta (USER)', expected: 'Lloc afegit o eliminat de favorits' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'VIEWER no pot afegir favorits', steps: 'Login VIEWER → intentar marcar favorit', expected: 'Acció bloquejada o desactivada' },
  { screen: 'Llocs (/places)', priority: 'Mitja', description: 'Enllaç «Veure favorits»', steps: 'Clic des del resum de resultats', expected: 'Navega a /favorites' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Mapa: marcadors visibles', steps: 'Després d\'una cerca amb resultats', expected: 'Marcadors al mapa Leaflet' },
  { screen: 'Llocs (/places)', priority: 'Mitja', description: 'Mapa: zoom amb roda del ratolí', steps: 'Zoom in/out sobre el mapa', expected: 'Zoom fluid sense saltar la vista' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Mapa: seleccionar marcador', steps: 'Clic a un marcador', expected: 'Targeta «Seleccionat al mapa» amb nom, ciutat, valoració' },
  { screen: 'Llocs (/places)', priority: 'Alta', description: 'Mapa: veure detall des de selecció', steps: 'Amb lloc seleccionat → «Veure detall»', expected: 'Navega a /places/:id' },
  { screen: 'Llocs (/places)', priority: 'Mitja', description: 'Mapa: treure selecció', steps: 'Clic «Treure selecció»', expected: 'Estat buit «Sense selecció activa»' },

  // ── 7. Detall lloc (càrrega → contingut → accions → relacionats → error) ──
  { screen: 'Detall lloc (/places/:id)', priority: 'Alta', description: 'Càrrega detall lloc vàlid', steps: 'Obrir /places/{id} d\'un lloc existent', expected: 'H1 amb nom, imatge, descripció i mètriques', multiRole: true },
  { screen: 'Detall lloc (/places/:id)', priority: 'Alta', description: 'Breadcrumbs', steps: 'Revisar Inici / Llocs / Nom', expected: 'Enllaços funcionals a / i /places' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Mitja', description: 'Secció Abans d\'anar-hi', steps: 'Revisar adreça, barri, política pet', expected: 'Dades completes del lloc' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Mitja', description: 'Secció Què hi trobaràs (features)', steps: 'Revisar tags de features', expected: 'Llista de característiques visible' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Mitja', description: 'Mapa ubicació al detall', steps: 'Revisar mapa centrat al lloc', expected: 'Marcador únic amb coordenades del backend' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Alta', description: 'Toggle favorit al detall', steps: 'Clic botó favorit', expected: 'Estat favorit canvia i persisteix' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Mitja', description: 'Tornar al llistat', steps: 'Clic «Tornar al llistat»', expected: 'Torna a /places conservant filtres si escau' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Mitja', description: 'Indicador «Has arribat des del mapa»', steps: 'Entrar al detall des del mapa de places', expected: 'Badge/indicador visible si escau' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Mitja', description: 'Llocs relacionats mateixa ciutat', steps: 'Desplaçar a «Més llocs a {ciutat}»', expected: 'Targetes enllaçades a altres /places/:id' },
  { screen: 'Detall lloc (/places/:id)', priority: 'Alta', description: 'Lloc inexistent', steps: 'Anar a /places/id-inexistent', expected: '«No hem trobat aquest lloc» + enllaç a Llocs' },

  // ── 8. Favorits (buit → afegir → resum → filtrar → eliminar) ──
  { screen: 'Favorits (/favorites)', priority: 'Alta', description: 'Pantalla favorits buida', steps: 'USER sense favorits → /favorites', expected: 'Empty state «Encara no tens favorits» + enllaç Explorar' },
  { screen: 'Favorits (/favorites)', priority: 'Alta', description: 'Afegir favorit des de places i veure aquí', steps: 'Marcar favorit a places → anar a favorits', expected: 'Lloc apareix a la graella' },
  { screen: 'Favorits (/favorites)', priority: 'Alta', description: 'Resum amb favorits', steps: 'Amb ≥1 favorit', expected: 'Comptador, ciutats presents, tipologies' },
  { screen: 'Favorits (/favorites)', priority: 'Mitja', description: 'Spotlight últim guardat', steps: 'Amb diversos favorits', expected: 'Bloc «Guardat més recent» amb enllaç al detall' },
  { screen: 'Favorits (/favorites)', priority: 'Mitja', description: 'Filtre cerca dins favorits', steps: 'Escriure al camp cerca de revisió', expected: 'Graella filtrada per nom/ciutat/etiqueta' },
  { screen: 'Favorits (/favorites)', priority: 'Mitja', description: 'Filtre ciutat dins favorits', steps: 'Seleccionar ciutat al combobox', expected: 'Només favorits d\'aquesta ciutat' },
  { screen: 'Favorits (/favorites)', priority: 'Mitja', description: 'Ordre: recents / valoració / nom', steps: 'Canviar selector d\'ordre', expected: 'Ordre de la graella canvia correctament' },
  { screen: 'Favorits (/favorites)', priority: 'Mitja', description: 'Restablir revisió', steps: 'Aplicar filtres → «Restablir revisió»', expected: 'Es mostren tots els favorits de nou' },
  { screen: 'Favorits (/favorites)', priority: 'Alta', description: 'Eliminar favorit des de la graella', steps: 'Desmarcar favorit a favorits', expected: 'Lloc desapareix de la llista' },
  { screen: 'Favorits (/favorites)', priority: 'Alta', description: 'Buidar favorits', steps: 'Clic «Buidar favorits» amb confirmació si escau', expected: 'Llista queda buida' },
  { screen: 'Favorits (/favorites)', priority: 'Alta', description: 'Continuar explorant', steps: 'Clic enllaç cap a /places', expected: 'Navegació correcta' },

  // ── 9. Perfil (càrrega → edició → avatar → consentiment → logout) ──
  { screen: 'Perfil (/perfil)', priority: 'Alta', description: 'Càrrega perfil USER', steps: 'Anar a /perfil com USER', expected: 'Heading perfil, avatar, email, rol, formulari' },
  { screen: 'Perfil (/perfil)', priority: 'Alta', description: 'Càrrega perfil ADMIN', steps: 'Anar a /perfil com ADMIN', expected: 'Text d\'administració; exempt consentiment' },
  { screen: 'Perfil (/perfil)', priority: 'Alta', description: 'Editar nom visible i desar', steps: 'Canviar nom → Guardar canvis', expected: 'Canvi persistit; visible al header/compte' },
  { screen: 'Perfil (/perfil)', priority: 'Mitja', description: 'Pujar avatar (fitxer)', steps: 'Afegir imatge des de fitxer', expected: 'Preview i persistència de l\'avatar' },
  { screen: 'Perfil (/perfil)', priority: 'Mitja', description: 'Avatar drag & drop', steps: 'Arrossegar imatge a la dropzone', expected: 'Avatar actualitzat' },
  { screen: 'Perfil (/perfil)', priority: 'Mitja', description: 'Treure avatar', steps: 'Clic «Treure»', expected: 'Placeholder NONE; avatar eliminat' },
  { screen: 'Perfil (/perfil)', priority: 'Alta', description: 'Consentiment obligatori USER', steps: 'Desmarcar checkbox privadesa i desar', expected: 'Validació; no es desa sense consentiment' },
  { screen: 'Perfil (/perfil)', priority: 'Alta', description: 'VIEWER no pot desar canvis', steps: 'Login VIEWER → editar i desar', expected: 'Edició bloquejada o error' },
  { screen: 'Perfil (/perfil)', priority: 'Alta', description: 'Tancar sessió des del perfil', steps: 'Clic «Tancar sessió»', expected: 'Sessió tancada; redirecció a login' },

  // ── 10. Notificacions (buit → llistat → accions) ──
  { screen: 'Notificacions (/notificacions)', priority: 'Alta', description: 'Pantalla sense notificacions', steps: 'Usuari sense notificacions', expected: 'Empty state «No hi ha notificacions»', multiRole: true },
  { screen: 'Notificacions (/notificacions)', priority: 'Alta', description: 'Llistat amb notificacions', steps: 'Generar error/acció que creï notificació', expected: 'Taula amb Estat, Títol, Missatge, Data, Accions' },
  { screen: 'Notificacions (/notificacions)', priority: 'Alta', description: 'Marcar una com a llegida', steps: 'Clic «Marcar llegida»', expected: 'Fila deixa d\'estar en negreta; comptador disminueix' },
  { screen: 'Notificacions (/notificacions)', priority: 'Mitja', description: 'Marcar com a no llegida', steps: 'Clic «Marcar no llegida»', expected: 'Torna a estat pendent' },
  { screen: 'Notificacions (/notificacions)', priority: 'Alta', description: 'Marcar totes com a llegides', steps: 'Clic botó del resum lateral', expected: 'Totes passen a llegides; botó desactivat si 0 pendents' },

  // ── 11. Ajuda (càrrega → contingut → enllaços) ──
  { screen: 'Ajuda (/ajuda)', priority: 'Alta', description: 'Càrrega pantalla ajuda', steps: 'Anar a /ajuda', expected: 'Heading «Com funciona Zuppeto avui»', multiRole: true },
  { screen: 'Ajuda (/ajuda)', priority: 'Mitja', description: 'Flux en tres passos', steps: 'Revisar secció Flux', expected: 'Tres info cards explicatives' },
  { screen: 'Ajuda (/ajuda)', priority: 'Mitja', description: 'FAQ / dubtes habituals', steps: 'Revisar secció inferior', expected: 'Cards amb respostes validades' },
  { screen: 'Ajuda (/ajuda)', priority: 'Mitja', description: 'CTAs cap a places i favorits', steps: 'Provar enllaços del hero', expected: 'Navegació correcta' },
  { screen: 'Ajuda (/ajuda)', priority: 'Mitja', description: 'Enllaç a contacte', steps: 'Clic «Necessites contacte o feedback?»', expected: 'Navega a /contacte' },

  // ── 12. Contacte (càrrega → canals → enllaços) ──
  { screen: 'Contacte (/contacte)', priority: 'Alta', description: 'Càrrega pantalla contacte', steps: 'Anar a /contacte', expected: 'Heading contacte, chips i canal suport@zuppeto.fake', multiRole: true },
  { screen: 'Contacte (/contacte)', priority: 'Mitja', description: 'Targetes de canals', steps: 'Revisar graella Partnerships / Feedback / Ciutats', expected: 'Info cards amb text coherent' },
  { screen: 'Contacte (/contacte)', priority: 'Mitja', description: 'CTA Veure com funciona', steps: 'Clic enllaç', expected: 'Navega a /ajuda' },
  { screen: 'Contacte (/contacte)', priority: 'Mitja', description: 'CTA Anar a llocs', steps: 'Clic enllaç', expected: 'Navega a /places' },

  // ── 13. Admin · Rols (catàleg base abans d'usuaris) ──
  { screen: 'Admin · Rols (/admin/rols)', priority: 'Alta', description: 'Catàleg de rols — llistat', steps: 'ADMIN → /admin/rols', expected: 'Títol «Catàleg de rols» i graella' },
  { screen: 'Admin · Rols (/admin/rols)', priority: 'Alta', description: 'Alta rol nou', steps: 'Crear rol amb clau única i nom visible', expected: 'Rol apareix al catàleg' },
  { screen: 'Admin · Rols (/admin/rols)', priority: 'Alta', description: 'Editar rol existent', steps: 'Canviar nom visible o estat actiu', expected: 'Canvis desats' },
  { screen: 'Admin · Rols (/admin/rols)', priority: 'Alta', description: 'Baixa rol sense dependències', steps: 'Esborrar rol sense usuaris assignats', expected: 'Rol eliminat' },
  { screen: 'Admin · Rols (/admin/rols)', priority: 'Alta', description: 'Baixa rol amb usuaris', steps: 'Intentar esborrar rol en ús', expected: 'Operació rebutjada amb missatge clar' },

  // ── 14. Admin · Usuaris (depèn de rols) ──
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Llistat usuaris', steps: 'ADMIN → /admin/usuaris', expected: 'Graella amb email, nom, rol, dates' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Usuari ADMIN existent', steps: 'Buscar admin@admin.adm', expected: 'Present amb rol ADMIN' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Modal crear usuari — validació', steps: 'Obrir Crear usuari amb camps buits', expected: 'Botó desactivat o validació fins omplir obligatoris' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Crear usuari VIEWER', steps: 'Omplir formulari rol VIEWER → crear', expected: 'Usuari nou a la graella' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Crear usuari USER', steps: 'Alta amb rol USER', expected: 'Usuari creat correctament' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Mitja', description: 'Crear usuari DEVELOPER', steps: 'Alta amb rol DEVELOPER', expected: 'Usuari creat correctament' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Email duplicat', steps: 'Intentar crear email ja existent', expected: 'Error de conflicte informatiu' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Veure detall usuari', steps: 'Clic fila o icona ull', expected: 'Panell/modal detall amb dades editables' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Canvi de rol', steps: 'Editar rol d\'un usuari i desar', expected: 'Canvi persistit després de refrescar' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Mitja', description: 'Avatar en alta usuari', steps: 'Pujar imatge al crear usuari', expected: 'Avatar desat i visible al detall' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'Esborrar usuari', steps: 'Icona esborrar → confirmar', expected: 'Usuari eliminat de la graella' },
  { screen: 'Admin · Usuaris (/admin/usuaris)', priority: 'Alta', description: 'DEVELOPER no accedeix', steps: 'DEVELOPER → /admin/usuaris', expected: 'Redirecció a home' },

  // ── 15. Admin · Permisos (depèn d'usuaris/rols) ──
  { screen: 'Admin · Permisos (/admin/permisos)', priority: 'Alta', description: 'Pantalla permisos per rol', steps: 'ADMIN → /admin/permisos', expected: 'Matriu o llistat permisos per rol' },
  { screen: 'Admin · Permisos (/admin/permisos)', priority: 'Alta', description: 'Editar permisos USER', steps: 'Modificar permís i desar', expected: 'Canvis desats' },
  { screen: 'Admin · Permisos (/admin/permisos)', priority: 'Alta', description: 'Editar permisos DEVELOPER', steps: 'Donar/treure page.admin.documentation', expected: 'Canvi reflectit en accés real' },
  { screen: 'Admin · Permisos (/admin/permisos)', priority: 'Alta', description: 'Refresc menú després de permisos', steps: 'Desar canvis que afecten menú', expected: 'Header es refresca sense relogin' },

  // ── 16. Admin · Menús (depèn de permisos) ──
  { screen: 'Admin · Menús (/admin/menus)', priority: 'Alta', description: 'Pantalla menús — llistat', steps: 'ADMIN → /admin/menus', expected: 'Definicions i assignacions de menú' },
  { screen: 'Admin · Menús (/admin/menus)', priority: 'Alta', description: 'Crear menú sota Ajuda', steps: 'Nou menú parentKey help', expected: 'Apareix dins submenú Ajuda' },
  { screen: 'Admin · Menús (/admin/menus)', priority: 'Alta', description: 'Crear menú sota admin', steps: 'Nou menú parentKey admin', expected: 'Apareix a Del administrador' },
  { screen: 'Admin · Menús (/admin/menus)', priority: 'Mitja', description: 'Canvi de label', steps: 'Editar label i desar', expected: 'Text actualitzat al header' },
  { screen: 'Admin · Menús (/admin/menus)', priority: 'Alta', description: 'Desactivar menú', steps: 'isActive false → desar', expected: 'Opció desapareix del header' },
  { screen: 'Admin · Menús (/admin/menus)', priority: 'Alta', description: 'Assignació per rol', steps: 'Menú només ADMIN', expected: 'Només ADMIN el veu' },
  { screen: 'Admin · Menús (/admin/menus)', priority: 'Alta', description: 'Refresc automàtic header', steps: 'Desar canvis a Menús', expected: 'Menú superior actualitzat' },

  // ── 17. Admin · Països (territori pare) ──
  { screen: 'Admin · Països (/admin/paisos)', priority: 'Alta', description: 'Catàleg de països — llistat', steps: 'ADMIN → /admin/paisos', expected: 'Graella països amb codi, ordre, actiu' },
  { screen: 'Admin · Països (/admin/paisos)', priority: 'Alta', description: 'Alta país', steps: 'Crear país amb codi únic (p.ex. ES)', expected: 'País nou a la llista' },
  { screen: 'Admin · Països (/admin/paisos)', priority: 'Alta', description: 'Editar país', steps: 'Modificar nom o ordre → desar', expected: 'Canvis persistits' },
  { screen: 'Admin · Països (/admin/paisos)', priority: 'Alta', description: 'Desactivar país', steps: 'Marcar inactiu', expected: 'Estat actualitzat; coherent amb comboboxs' },
  { screen: 'Admin · Països (/admin/paisos)', priority: 'Alta', description: 'Codi duplicat', steps: 'Alta amb codi ja existent', expected: 'Error de validació' },
  { screen: 'Admin · Països (/admin/paisos)', priority: 'Mitja', description: 'Baixa país sense ciutats', steps: 'Esborrar país sense dependències', expected: 'Eliminat correctament' },

  // ── 18. Admin · Ciutats (fill de països) ──
  { screen: 'Admin · Ciutats (/admin/ciutats)', priority: 'Alta', description: 'Catàleg de ciutats — llistat', steps: 'ADMIN → /admin/ciutats', expected: 'Graella ciutats per país' },
  { screen: 'Admin · Ciutats (/admin/ciutats)', priority: 'Alta', description: 'Alta ciutat', steps: 'Crear ciutat amb país i nom normalitzat', expected: 'Ciutat nova visible' },
  { screen: 'Admin · Ciutats (/admin/ciutats)', priority: 'Alta', description: 'Coordenades opcionals', steps: 'Afegir lat/long a una ciutat', expected: 'Coordenades desades' },
  { screen: 'Admin · Ciutats (/admin/ciutats)', priority: 'Mitja', description: 'Editar ciutat', steps: 'Canviar nom o coordenades', expected: 'Canvis persistits' },
  { screen: 'Admin · Ciutats (/admin/ciutats)', priority: 'Alta', description: 'Unicitat nom dins país', steps: 'Duplicar nom normalitzat mateix país', expected: 'Error de conflicte' },
  { screen: 'Admin · Ciutats (/admin/ciutats)', priority: 'Mitja', description: 'Ciutat al combobox de places', steps: 'Ciutat creada → provar combobox a /places', expected: 'Ciutat disponible a la cerca' },

  // ── 19. Admin · Llocs (contingut; usa territori) ──
  { screen: 'Admin · Llocs (/admin/llocs)', priority: 'Alta', description: 'Catàleg de llocs — llistat', steps: 'ADMIN → /admin/llocs', expected: 'Graella manteniment llocs' },
  { screen: 'Admin · Llocs (/admin/llocs)', priority: 'Alta', description: 'Alta lloc manual', steps: 'Crear lloc amb dades mínimes obligatòries', expected: 'Lloc al catàleg i visible a /places' },
  { screen: 'Admin · Llocs (/admin/llocs)', priority: 'Alta', description: 'Editar lloc', steps: 'Modificar nom, ciutat o política pet', expected: 'Canvis reflectits al detall públic' },
  { screen: 'Admin · Llocs (/admin/llocs)', priority: 'Alta', description: 'Baixa lloc', steps: 'Esborrar lloc de prova', expected: 'Desapareix del catàleg i cerca' },
  { screen: 'Admin · Llocs (/admin/llocs)', priority: 'Mitja', description: 'Traçabilitat procedència', steps: 'Revisar camp/origen del lloc (intern vs extern)', expected: 'Procedència identificable' },

  // ── 20. Admin · Documentació (accés per rol) ──
  { screen: 'Admin · Documentació (/admin/documentacio)', priority: 'Alta', description: 'Sense sessió — redirecció', steps: 'Anar directament a /admin/documentacio', expected: 'Redirecció a login' },
  { screen: 'Admin · Documentació (/admin/documentacio)', priority: 'Alta', description: 'USER no accedeix', steps: 'Login USER → URL directa', expected: 'Accés denegat / redirecció' },
  { screen: 'Admin · Documentació (/admin/documentacio)', priority: 'Alta', description: 'Accés DEVELOPER', steps: 'Login DEVELOPER → /admin/documentacio', expected: 'Llistat Documents i visor de contingut' },
  { screen: 'Admin · Documentació (/admin/documentacio)', priority: 'Alta', description: 'Accés ADMIN', steps: 'Login ADMIN → pantalla', expected: 'Mateix llistat amb tots els documents' },
  { screen: 'Admin · Documentació (/admin/documentacio)', priority: 'Mitja', description: 'Obrir document tècnic', steps: 'Seleccionar tecnic-ca al llistat', expected: 'Contingut markdown/text al panell dret' },
  { screen: 'Admin · Documentació (/admin/documentacio)', priority: 'Mitja', description: 'Obrir document funcional', steps: 'Seleccionar funcional-ca', expected: 'Contingut visible sense error' },

  // ── 21. Permisos stub ──
  { screen: 'Permisos stub (/permissions)', priority: 'Mitja', description: 'Accés ADMIN', steps: 'Anar a /permissions com ADMIN', expected: 'Pantalla informativa de rols i permisos' },
  { screen: 'Permisos stub (/permissions)', priority: 'Alta', description: 'USER no accedeix', steps: 'Anar a /permissions com USER', expected: 'Redirecció o accés denegat' },

  // ── 22. Seguretat API (transversal, al final) ──
  { screen: 'Rols i seguretat', priority: 'Alta', description: 'API admin com USER → 403', steps: 'Cridar /api/admin/* amb token USER', expected: '403 Forbidden' },
  { screen: 'Rols i seguretat', priority: 'Alta', description: 'API navigation menu ADMIN', steps: 'GET /api/navigation/menu com ADMIN', expected: '200 amb home, places, favorites, help, admin' },
];

const AUTH_ROLES = ['ADMIN', 'USER', 'DEVELOPER', 'VIEWER'];

const ROLE_STYLES = {
  'Sense sessió': { fill: 'FFF1F5F9', accent: 'FF64748B' },
  ADMIN: { fill: 'FFFEF2F2', accent: 'FFDC2626' },
  USER: { fill: 'FFEFF6FF', accent: 'FF2563EB' },
  DEVELOPER: { fill: 'FFF5F3FF', accent: 'FF7C3AED' },
  VIEWER: { fill: 'FFF0FDF4', accent: 'FF16A34A' },
};

const ROLE_CREDENTIALS = {
  'Sense sessió': '— (sense login)',
  ADMIN: 'admin@admin.adm / Admin123',
  USER: 'user.e2e@zuppeto.local / Admin123',
  DEVELOPER: 'developer.e2e@zuppeto.local / Admin123',
  VIEWER: 'viewer.e2e@zuppeto.local / Admin123',
};

function inferRole(test) {
  const blob = `${test.steps} ${test.description}`.toLowerCase();
  if (blob.includes('sense sessió') || blob.includes('sense sessio') || blob.includes('sense login')) {
    return 'Sense sessió';
  }
  if (blob.includes('developer')) return 'DEVELOPER';
  if (blob.includes('viewer')) return 'VIEWER';
  if (blob.includes('admin@admin') || blob.includes(' com admin') || blob.includes('→ admin')) {
    return 'ADMIN';
  }
  if (blob.includes(' com user') || blob.includes('(user)')) return 'USER';
  if (test.screen.startsWith('Admin ·')) return 'ADMIN';
  if (test.screen.includes('Permisos stub')) {
    return blob.includes('user no') ? 'USER' : 'ADMIN';
  }
  return 'USER';
}

function resolveRoles(test) {
  if (test.roles?.length) return test.roles;
  if (test.multiRole) return AUTH_ROLES;
  if (test.role) return [test.role];
  return [inferRole(test)];
}

for (const t of rawTests) {
  if (!t.role && !t.roles && !t.multiRole) {
    t.role = inferRole(t);
  }
}

function assignSequentialCodes(testCases) {
  return testCases.map((t, index) => ({
    ...t,
    code: `ZUP-${String(index + 1).padStart(3, '0')}`,
  }));
}

const tests = assignSequentialCodes(rawTests);

const screenColors = {
  'Capçalera / Navegació': 'FF1E40AF',
  'Login (/login)': 'FF7C3AED',
  'Auth callback (/auth/callback)': 'FF6D28D9',
  'Retorn del login (/auth/callback)': 'FF6D28D9',
  'Inici (/)': 'FF0369A1',
  'Llocs (/places)': 'FF0D9488',
  'Detall lloc (/places/:id)': 'FF0F766E',
  'Favorits (/favorites)': 'FFDB2777',
  'Perfil (/perfil)': 'FFEA580C',
  'Notificacions (/notificacions)': 'FFCA8A04',
  'Contacte (/contacte)': 'FF65A30D',
  'Ajuda (/ajuda)': 'FF16A34A',
  'Permisos stub (/permissions)': 'FF57534E',
  'Admin · Documentació (/admin/documentacio)': 'FF4338CA',
  'Admin · Usuaris (/admin/usuaris)': 'FF4F46E5',
  'Admin · Permisos (/admin/permisos)': 'FF6366F1',
  'Admin · Menús (/admin/menus)': 'FF818CF8',
  'Admin · Rols (/admin/rols)': 'FF9333EA',
  'Admin · Països (/admin/paisos)': 'FF0891B2',
  'Admin · Ciutats (/admin/ciutats)': 'FF0284C7',
  'Admin · Llocs (/admin/llocs)': 'FF059669',
  'Rols i seguretat': 'FFB91C1C',
};

const priorityFill = {
  Alta: 'FFFEE2E2',
  Mitja: 'FFFEF3C7',
  Baixa: 'FFE0F2FE',
};

/** One row per browser per test case. */
const BROWSERS = [
  { name: 'Chrome', fill: 'FFFFF7ED', accent: 'FFF59E0B' },
  { name: 'Firefox', fill: 'FFFFF7ED', accent: 'FFEA580C' },
  { name: 'Edge', fill: 'FFEFF6FF', accent: 'FF2563EB' },
  { name: 'IE 11', fill: 'FFE0F2FE', accent: 'FF0284C7' },
  { name: 'Opera', fill: 'FFFEF2F2', accent: 'FFDC2626' },
  { name: 'Brave', fill: 'FFFFF1F2', accent: 'FFE11D48' },
];

function countExecutionRows(testCases) {
  return testCases.reduce((sum, t) => sum + resolveRoles(t).length * BROWSERS.length, 0);
}

const TEST_DEF_COLUMNS = ['A', 'B', 'C', 'D', 'E', 'F'];

function applyCellBorder(cell) {
  cell.border = {
    top: { style: 'thin', color: { argb: 'FFE2E8F0' } },
    left: { style: 'thin', color: { argb: 'FFE2E8F0' } },
    bottom: { style: 'thin', color: { argb: 'FFE2E8F0' } },
    right: { style: 'thin', color: { argb: 'FFE2E8F0' } },
  };
}

function styleExecutionCells(row, browser, roleName) {
  const browserCell = row.getCell('browser');
  browserCell.value = browser.name;
  browserCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: browser.fill } };
  browserCell.font = { bold: true, color: { argb: browser.accent } };
  browserCell.alignment = { vertical: 'middle', horizontal: 'center' };

  const roleStyle = ROLE_STYLES[roleName] || ROLE_STYLES.USER;
  const roleCell = row.getCell('role');
  roleCell.value = roleName;
  roleCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: roleStyle.fill } };
  roleCell.font = { bold: true, color: { argb: roleStyle.accent } };
  roleCell.alignment = { vertical: 'middle', horizontal: 'center' };

  const dateCell = row.getCell('date');
  dateCell.numFmt = 'dd/mm/yyyy';
  dateCell.alignment = { vertical: 'middle', horizontal: 'center' };

  const resultCell = row.getCell('result');
  resultCell.value = 'PENDENT';
  resultCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF1F5F9' } };
  resultCell.alignment = { vertical: 'middle', horizontal: 'center' };
  resultCell.font = { bold: true, color: { argb: 'FF64748B' } };

  row.getCell('observations').alignment = { vertical: 'top', wrapText: true };
}

function styleHeaderRow(row) {
  row.height = 28;
  row.eachCell((cell) => {
    cell.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 11, name: 'Calibri' };
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF0F172A' } };
    cell.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
    cell.border = {
      top: { style: 'thin', color: { argb: 'FF334155' } },
      left: { style: 'thin', color: { argb: 'FF334155' } },
      bottom: { style: 'medium', color: { argb: 'FF3B82F6' } },
      right: { style: 'thin', color: { argb: 'FF334155' } },
    };
  });
}

async function main() {
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'Zuppeto';
  workbook.created = new Date();

  // ── Sheet: Instruccions ──
  const intro = workbook.addWorksheet('Instruccions', {
    views: [{ showGridLines: false }],
    properties: { tabColor: { argb: 'FF3B82F6' } },
  });
  intro.mergeCells('B2:H2');
  intro.getCell('B2').value = 'Zuppeto — Joc de probes manual (totes les pantalles)';
  intro.getCell('B2').font = { bold: true, size: 18, color: { argb: 'FF0F172A' } };
  intro.getCell('B4').value = 'Entorn';
  intro.getCell('C4').value = 'http://localhost:4200 (web) · http://localhost:5211/swagger (API)';
  intro.getCell('B5').value = 'Ordre recomanat';
  intro.getCell('C5').value = 'Login → Auth → Capçalera → Rols UX → Inici → Llocs → Detall → Favorits → Perfil → Notificacions → Ajuda → Contacte → Admin (Rols→Usuaris→Permisos→Menús→Països→Ciutats→Llocs→Docs) → API';
  intro.getCell('B6').value = 'Codi prova';
  intro.getCell('C6').value = 'Seqüencial global: ZUP-001 … ZUP-153 (ordre d\'execució recomanat)';
  intro.getCell('B7').value = 'Rol ADMIN';
  intro.getCell('C7').value = ROLE_CREDENTIALS.ADMIN;
  intro.getCell('B8').value = 'Rol USER';
  intro.getCell('C8').value = ROLE_CREDENTIALS.USER;
  intro.getCell('B9').value = 'Rol DEVELOPER';
  intro.getCell('C9').value = ROLE_CREDENTIALS.DEVELOPER;
  intro.getCell('B10').value = 'Rol VIEWER';
  intro.getCell('C10').value = ROLE_CREDENTIALS.VIEWER;
  intro.getCell('B11').value = 'Estructura';
  intro.getCell('C11').value = 'Cada prova es repeteix per rol(s) indicats i per navegador (6 files per rol). Data, Resultat i Observacions s\'omplen per fila.';
  intro.getCell('B12').value = 'Columna Rol probat';
  intro.getCell('C12').value = 'Sense sessió · ADMIN · USER · DEVELOPER · VIEWER (segons la prova; algunes es repeteixen per tots els rols autenticats)';
  intro.getCell('B13').value = 'Navegadors';
  intro.getCell('C13').value = BROWSERS.map((b) => b.name).join(' · ');
  intro.getCell('B14').value = 'Columna Resultat';
  intro.getCell('C14').value = 'Omplir amb: OK · KO · PENDENT · N/A (per cada rol i navegador)';
  intro.getCell('B15').value = 'Columna Data';
  intro.getCell('C15').value = 'Data d\'execució de la prova en aquell rol i navegador (dd/mm/aaaa)';
  intro.getCell('B17').value = 'Pantalles cobertes';
  intro.getCell('C17').value = Object.keys(screenColors).join(' · ');
  const totalExecutionRows = countExecutionRows(tests);
  intro.getCell('B19').value = `Proves lògiques: ${tests.length} · Files totals: ${totalExecutionRows} · Codis: ZUP-001–ZUP-${String(tests.length).padStart(3, '0')}`;
  intro.getCell('B19').font = { bold: true, color: { argb: 'FF1D4ED8' } };
  intro.columns = [{ width: 3 }, { width: 22 }, { width: 80 }];

  // ── Sheet: Proves ──
  const sheet = workbook.addWorksheet('Proves', {
    views: [{ state: 'frozen', ySplit: 1, xSplit: 0 }],
    properties: { tabColor: { argb: 'FF10B981' } },
  });

  sheet.columns = [
    { header: 'Pantalla', key: 'screen', width: 34 },
    { header: 'Prioritat', key: 'priority', width: 11 },
    { header: 'Codi prova', key: 'code', width: 14 },
    { header: 'Descripció prova', key: 'description', width: 38 },
    { header: 'Passos', key: 'steps', width: 42 },
    { header: 'Resultat esperat', key: 'expected', width: 38 },
    { header: 'Navegador', key: 'browser', width: 14 },
    { header: 'Rol probat', key: 'role', width: 14 },
    { header: 'Data', key: 'date', width: 13 },
    { header: 'Resultat', key: 'result', width: 12 },
    { header: 'Observacions', key: 'observations', width: 36 },
  ];

  styleHeaderRow(sheet.getRow(1));

  // Header tint: execution columns (per browser / role)
  for (const col of [7, 8, 9, 10, 11]) {
    const cell = sheet.getRow(1).getCell(col);
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF1E3A8A' } };
  }

  let testGroupIndex = 0;
  const totalRows = countExecutionRows(tests);

  tests.forEach((t) => {
    const startRow = sheet.lastRow.number + 1;
    const roles = resolveRoles(t);

    roles.forEach((roleName) => {
      BROWSERS.forEach((browser) => {
        const row = sheet.addRow({
          screen: plainLanguageForTesters(t.screen),
          priority: t.priority,
          code: t.code,
          description: plainLanguageForTesters(t.description),
          steps: plainLanguageForTesters(t.steps),
          expected: plainLanguageForTesters(t.expected),
          browser: browser.name,
          role: roleName,
          date: '',
          result: 'PENDENT',
          observations: '',
        });

        row.height = 36;
        row.eachCell((cell, colNumber) => {
          cell.alignment = { vertical: 'top', wrapText: true };
          applyCellBorder(cell);
          cell.font = { name: 'Calibri', size: 10 };

          if (colNumber <= 6 && testGroupIndex % 2 === 1) {
            cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF8FAFC' } };
          }
        });

        const priorityCell = row.getCell('priority');
        priorityCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: priorityFill[t.priority] || 'FFFFFFFF' } };
        priorityCell.alignment = { vertical: 'middle', horizontal: 'center' };
        priorityCell.font = { bold: true, size: 10 };

        const codeCell = row.getCell('code');
        codeCell.font = { bold: true, color: { argb: 'FF1E40AF' } };
        codeCell.alignment = { vertical: 'middle', horizontal: 'center' };

        styleExecutionCells(row, browser, roleName);
      });
    });

    const endRow = sheet.lastRow.number;
    if (endRow > startRow) {
      for (const col of TEST_DEF_COLUMNS) {
        sheet.mergeCells(`${col}${startRow}:${col}${endRow}`);
        const merged = sheet.getCell(`${col}${startRow}`);
        merged.alignment = { vertical: 'middle', horizontal: col === 'C' ? 'center' : 'top', wrapText: true };
      }
    }

    const screenColor = screenColors[t.screen] || 'FF64748B';
    const screenCell = sheet.getCell(`A${startRow}`);
    screenCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: screenColor } };
    screenCell.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 10 };
    screenCell.alignment = { vertical: 'middle', horizontal: 'left', wrapText: true };

    testGroupIndex += 1;
  });

  // Data validation: Resultat (per role × browser row)
  sheet.dataValidations.add(`J2:J${totalRows + 1}`, {
    type: 'list',
    allowBlank: true,
    formulae: ['"OK,KO,PENDENT,N/A"'],
    showErrorMessage: true,
    errorTitle: 'Valor no vàlid',
    error: 'Escull OK, KO, PENDENT o N/A',
  });

  sheet.autoFilter = { from: 'A1', to: 'K1' };

  // ── Sheet: Resum per pantalla ──
  const summary = workbook.addWorksheet('Resum', {
    properties: { tabColor: { argb: 'FFF59E0B' } },
  });
  summary.columns = [
    { header: 'Pantalla', key: 'screen', width: 40 },
    { header: 'Proves', key: 'total', width: 10 },
    { header: 'Execucions', key: 'executions', width: 12 },
    { header: 'Alta', key: 'alta', width: 10 },
    { header: 'Mitja', key: 'mitja', width: 10 },
    { header: 'Baixa', key: 'baixa', width: 10 },
    { header: 'OK', key: 'ok', width: 10 },
    { header: 'KO', key: 'ko', width: 10 },
    { header: 'Pendent', key: 'pending', width: 12 },
  ];
  styleHeaderRow(summary.getRow(1));

  const byScreen = {};
  for (const t of tests) {
    if (!byScreen[t.screen]) {
      byScreen[t.screen] = { total: 0, Alta: 0, Mitja: 0, Baixa: 0, executions: 0 };
    }
    byScreen[t.screen].total++;
    byScreen[t.screen][t.priority]++;
    byScreen[t.screen].executions += resolveRoles(t).length * BROWSERS.length;
  }

  Object.entries(byScreen).forEach(([screen, counts]) => {
    const row = summary.addRow({
      screen,
      total: counts.total,
      executions: counts.executions,
      alta: counts.Alta,
      mitja: counts.Mitja,
      baixa: counts.Baixa,
      ok: 0,
      ko: 0,
      pending: counts.executions,
    });
    const color = screenColors[screen] || 'FF64748B';
    row.getCell('screen').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: color } };
    row.getCell('screen').font = { bold: true, color: { argb: 'FFFFFFFF' } };
  });

  const totalRow = summary.addRow({
    screen: 'TOTAL',
    total: tests.length,
    executions: totalExecutionRows,
    alta: tests.filter((t) => t.priority === 'Alta').length,
    mitja: tests.filter((t) => t.priority === 'Mitja').length,
    baixa: tests.filter((t) => t.priority === 'Baixa').length,
    ok: 0,
    ko: 0,
    pending: totalExecutionRows,
  });
  totalRow.font = { bold: true };
  totalRow.eachCell((cell) => {
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF0F172A' } };
    cell.font = { bold: true, color: { argb: 'FFFFFFFF' } };
  });

  // ── Sheet: Resum per navegador ──
  const byBrowser = workbook.addWorksheet('Resum navegadors', {
    properties: { tabColor: { argb: 'FF8B5CF6' } },
  });
  byBrowser.columns = [
    { header: 'Navegador', key: 'browser', width: 16 },
    { header: 'Execucions', key: 'executions', width: 14 },
    { header: 'OK', key: 'ok', width: 10 },
    { header: 'KO', key: 'ko', width: 10 },
    { header: 'Pendent', key: 'pending', width: 12 },
    { header: 'N/A', key: 'na', width: 10 },
  ];
  styleHeaderRow(byBrowser.getRow(1));

  BROWSERS.forEach((browser) => {
    const executions = tests.reduce(
      (sum, t) => sum + resolveRoles(t).length,
      0,
    );
    const row = byBrowser.addRow({
      browser: browser.name,
      executions,
      ok: 0,
      ko: 0,
      pending: executions,
      na: 0,
    });
    row.getCell('browser').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: browser.fill } };
    row.getCell('browser').font = { bold: true, color: { argb: browser.accent } };
  });

  // ── Sheet: Resum per rol ──
  const byRole = workbook.addWorksheet('Resum rols', {
    properties: { tabColor: { argb: 'FFEF4444' } },
  });
  byRole.columns = [
    { header: 'Rol probat', key: 'role', width: 16 },
    { header: 'Credencials', key: 'credentials', width: 38 },
    { header: 'Execucions', key: 'executions', width: 14 },
    { header: 'OK', key: 'ok', width: 10 },
    { header: 'KO', key: 'ko', width: 10 },
    { header: 'Pendent', key: 'pending', width: 12 },
    { header: 'N/A', key: 'na', width: 10 },
  ];
  styleHeaderRow(byRole.getRow(1));

  const ALL_ROLES = ['Sense sessió', ...AUTH_ROLES];
  ALL_ROLES.forEach((roleName) => {
    const executions = tests.reduce((sum, t) => {
      const roles = resolveRoles(t);
      return roles.includes(roleName) ? sum + BROWSERS.length : sum;
    }, 0);
    const roleStyle = ROLE_STYLES[roleName] || ROLE_STYLES.USER;
    const row = byRole.addRow({
      role: roleName,
      credentials: ROLE_CREDENTIALS[roleName] || '—',
      executions,
      ok: 0,
      ko: 0,
      pending: executions,
      na: 0,
    });
    row.getCell('role').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: roleStyle.fill } };
    row.getCell('role').font = { bold: true, color: { argb: roleStyle.accent } };
  });

  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  await workbook.xlsx.writeFile(outputPath);
  console.log(`Generated: ${outputPath}`);
  console.log(`Logical tests: ${tests.length} · Rows (roles × browsers): ${totalExecutionRows}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
