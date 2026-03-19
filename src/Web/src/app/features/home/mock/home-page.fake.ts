import { HomeCity, HomeHeroContent, HomeWhyContent } from '../models/home-content.model';

export const HOME_HERO_FAKE: HomeHeroContent = {
  eyebrow: 'YepPet · Fase 1',
  titleStart: 'Llocs que diuen',
  titleHighlight: 'SÍ',
  titleEnd: 'a les mascotes.',
  copy:
    'Plataforma pet-friendly per descobrir llocs, estades i serveis que realment accepten mascotes, amb una experiència més visual i més clara des de la primera pantalla.',
  chips: ['Dogs welcome', 'Cats welcome', 'Outdoor friendly', 'Long stays'],
  quickMatchTitle: 'Pet-friendly terrace for brunch',
  quickMatchCopy: 'Dogs welcome · Outdoor seating',
  futureReadyTitle: 'Mock-first architecture',
  futureReadyCopy: 'Ready to swap services for API later'
};

export const HOME_TRENDING_CITIES_FAKE: HomeCity[] = [
  {
    name: 'Barcelona',
    country: 'Spain',
    vibe: 'Terraces, urban walks and weekend escapes'
  },
  {
    name: 'Madrid',
    country: 'Spain',
    vibe: 'Hotels, brunch spots and dog-friendly parks'
  },
  {
    name: 'Lisboa',
    country: 'Portugal',
    vibe: 'Sunny stays and relaxed neighborhoods'
  },
  {
    name: 'Berlin',
    country: 'Germany',
    vibe: 'Independent cafes and pet-friendly apartments'
  }
];

export const HOME_WHY_FAKE: HomeWhyContent = {
  eyebrow: 'Why YepPet',
  title: 'Una experiència pensada per reduir fricció, no només per llistar llocs.',
  reasons: [
    'No més trucades per confirmar si accepten mascotes.',
    'Filtres pensats per la vida real, no per fer bonic.',
    'Una base preparada per créixer cap a comunitat i serveis.'
  ]
};
