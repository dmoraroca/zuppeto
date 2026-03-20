import { Place, PlaceType } from '../models/place.model';

export const PLACE_TYPE_LABELS: Record<PlaceType, string> = {
  restaurant: 'Restaurant',
  hotel: 'Hotel',
  apartment: 'Apartament',
  park: 'Parc',
  service: 'Servei'
};

export const PLACES_FAKE: Place[] = [
  {
    id: 'barcelona-brisa-bistro',
    name: 'Brisa Bistro',
    city: 'Barcelona',
    country: 'Espanya',
    type: 'restaurant',
    shortDescription: 'Terrassa urbana amb menú casual i tracte fàcil per a gossos.',
    description:
      'Restaurant lluminós a prop del centre amb terrassa ampla, aigua per a mascotes i ambient relaxat per esmorzars llargs o dinars informals.',
    imageUrl:
      'https://images.unsplash.com/photo-1552566626-52f8b828add9?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.6,
    tags: ['terrassa', 'brunch', 'centre'],
    address: 'Carrer de la Marina 118, Barcelona',
    petNotes: 'Gossos benvinguts a la terrassa i a l’interior en hores tranquil·les.',
    features: ['Aigua per mascotes', 'Terrassa gran', 'Reserva online']
  },
  {
    id: 'barcelona-pawtel-gotic',
    name: 'Pawtel Gotic',
    city: 'Barcelona',
    country: 'Espanya',
    type: 'hotel',
    shortDescription: 'Hotel pet-friendly per escapades urbanes i estades curtes.',
    description:
      'Hotel boutique amb habitacions àmplies, llits per a mascotes sota petició i bona connexió amb zones de passeig a peu.',
    imageUrl:
      'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.8,
    tags: ['hotel', 'boutique', 'ciutat'],
    address: 'Via Laietana 44, Barcelona',
    petNotes: 'Accepta gossos i gats petits. Suplement fix per nit.',
    features: ['Habitacions pet-friendly', 'Recepció 24h', 'Late check-out']
  },
  {
    id: 'madrid-latido-park',
    name: 'Latido Park',
    city: 'Madrid',
    country: 'Espanya',
    type: 'park',
    shortDescription: 'Espai verd per passejar, jugar i descansar al mig de la ciutat.',
    description:
      'Parc obert amb ombra, zones de descans i accés fàcil des de barris residencials. Molt útil per sortides curtes o trobades amb altres mascotes.',
    imageUrl:
      'https://images.unsplash.com/photo-1511497584788-876760111969?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.4,
    tags: ['parc', 'ombra', 'passeig'],
    address: 'Paseo del Prado, Madrid',
    petNotes: 'Millor franja al matí o cap al vespre quan hi ha menys calor.',
    features: ['Zona verda', 'Bancs', 'Accés fàcil']
  },
  {
    id: 'madrid-casa-larga',
    name: 'Casa Larga',
    city: 'Madrid',
    country: 'Espanya',
    type: 'apartment',
    shortDescription: 'Apartament pensat per estades llargues amb mascotes.',
    description:
      'Allotjament ampli amb cuina equipada, ascensor i flexibilitat per a estades de diverses setmanes amb animals de companyia.',
    imageUrl:
      'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.5,
    tags: ['apartament', 'estades llargues', 'família'],
    address: 'Calle del Pez 17, Madrid',
    petNotes: 'Ideal per gossos tranquils i gats d’interior.',
    features: ['Cuina equipada', 'Rentadora', 'Check-in autònom']
  },
  {
    id: 'lisboa-mar-calma',
    name: 'Mar Calma',
    city: 'Lisboa',
    country: 'Portugal',
    type: 'restaurant',
    shortDescription: 'Restaurant amb terrassa tranquil·la i ritme relaxat.',
    description:
      'Espai lluminós amb cuina mediterrània, seients còmodes i personal acostumat a rebre clients amb mascotes.',
    imageUrl:
      'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.3,
    tags: ['terrassa', 'sopar', 'barri'],
    address: 'Rua da Esperança 12, Lisboa',
    petNotes: 'Gossos de mida petita i mitjana sense problema a la terrassa.',
    features: ['Terrassa', 'Reserves ràpides', 'Ambient tranquil']
  },
  {
    id: 'lisboa-siesta-stay',
    name: 'Siesta Stay',
    city: 'Lisboa',
    country: 'Portugal',
    type: 'hotel',
    shortDescription: 'Hotel còmode amb recepció simple i espais amplis.',
    description:
      'Hotel modern per a viatges curts amb mascotes, amb habitacions àmplies i un procés d’arribada senzill.',
    imageUrl:
      'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.7,
    tags: ['hotel', 'escapada', 'còmode'],
    address: 'Avenida 24 de Julho 81, Lisboa',
    petNotes: 'Accepta una mascota per habitació amb suplement moderat.',
    features: ['Check-in ràpid', 'Habitacions grans', 'Servei amable']
  },
  {
    id: 'berlin-kiez-cafe',
    name: 'Kiez Cafe',
    city: 'Berlin',
    country: 'Alemanya',
    type: 'service',
    shortDescription: 'Espai híbrid de cafè i servei local pet-friendly.',
    description:
      'Un lloc flexible per descansar, treballar una estona i trobar informació o serveis útils per moure’t amb mascota per la ciutat.',
    imageUrl:
      'https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.2,
    tags: ['cafè', 'serveis', 'barri'],
    address: 'Weserstraße 33, Berlin',
    petNotes: 'Gossos tranquils benvinguts, especialment entre setmana.',
    features: ['Wi-Fi', 'Zona interior', 'Ambient local']
  },
  {
    id: 'berlin-grunhof',
    name: 'Grunhof',
    city: 'Berlin',
    country: 'Alemanya',
    type: 'apartment',
    shortDescription: 'Apartament pet-friendly per viure la ciutat sense presses.',
    description:
      'Allotjament lluminós amb accés fàcil a zones verdes, pensat per a estades de diversos dies amb mascotes.',
    imageUrl:
      'https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.9,
    tags: ['apartament', 'barri tranquil', 'llarga estada'],
    address: 'Oderberger Straße 70, Berlin',
    petNotes: 'Ideal per mascotes acostumades a espais interiors tranquils.',
    features: ['Self check-in', 'Zona tranquil·la', 'Pet-friendly real']
  }
];
