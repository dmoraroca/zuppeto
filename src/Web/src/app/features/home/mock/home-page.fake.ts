import { HomeCity, HomeHeroContent, HomeWhyContent } from '../models/home-content.model';

export const HOME_HERO_FAKE: HomeHeroContent = {
  eyebrow: 'PetiLoc · Descoberta pet-friendly',
  titleStart: 'Llocs que diuen',
  titleHighlight: 'SÍ',
  titleEnd: 'a les mascotes.',
  copy:
    'Descobreix llocs, estades i serveis que realment accepten mascotes amb una experiència més clara: filtres útils, mapa contextual i favorits pensats per reprendre la cerca sense fricció.',
  chips: ['Gossos benvinguts', 'Gats benvinguts', 'Terrassa exterior', 'Estades llargues'],
  quickMatchTitle: 'Terrassa pet-friendly per fer brunch',
  quickMatchCopy: 'Gossos benvinguts · Seients exteriors',
  futureReadyTitle: 'Mock-first amb criteri de producte',
  futureReadyCopy: 'La UI ja està pensada per passar a API real sense reescriure el flux',
  featuredPlaces: [
    {
      id: 'barcelona-pawtel-gotic',
      name: 'Pawtel Gotic',
      city: 'Barcelona',
      badge: 'Favorit'
    },
    {
      id: 'berlin-grunhof',
      name: 'Grunhof',
      city: 'Berlin',
      badge: 'Destacat'
    }
  ]
};

export const HOME_TRENDING_CITIES_FAKE: HomeCity[] = [
  {
    name: 'Barcelona',
    country: 'Espanya',
    vibe: 'Terrasses, passejos urbans i escapades de cap de setmana'
  },
  {
    name: 'Madrid',
    country: 'Espanya',
    vibe: 'Hotels, brunch i parcs pensats per sortir amb gos'
  },
  {
    name: 'Lisboa',
    country: 'Portugal',
    vibe: 'Estades lluminoses i barris tranquils per anar sense presses'
  },
  {
    name: 'Berlin',
    country: 'Alemanya',
    vibe: 'Cafès independents i apartaments que realment accepten mascotes'
  }
];

export const HOME_WHY_FAKE: HomeWhyContent = {
  eyebrow: 'Per què PetiLoc',
  title: 'Una experiència pensada per reduir fricció, no només per llistar llocs.',
  reasons: [
    'No més trucades per confirmar si accepten mascotes.',
    'Filtres pensats per la vida real, no per fer bonic.',
    'Una base preparada per créixer cap a comunitat i serveis.'
  ]
};
