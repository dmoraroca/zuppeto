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
    neighborhood: 'Vila Olímpica',
    type: 'restaurant',
    shortDescription: 'Terrassa urbana amb menú casual i tracte fàcil per a gossos.',
    description:
      'Restaurant lluminós a prop del centre amb terrassa ampla, aigua per a mascotes i ambient relaxat per esmorzars llargs o dinars informals.',
    imageUrl:
      'https://images.unsplash.com/photo-1552566626-52f8b828add9?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.6,
    reviewCount: 187,
    priceLabel: '20-35 € per persona',
    petPolicyLabel: 'Sense suplement per gossos',
    tags: ['terrassa', 'brunch', 'centre'],
    address: 'Carrer de la Marina 118, Barcelona',
    petNotes: 'Gossos benvinguts a la terrassa i a l’interior en hores tranquil·les.',
    features: ['Aigua per mascotes', 'Terrassa gran', 'Reserva online'],
    coordinates: {
      lat: 41.390205,
      lng: 2.191987
    }
  },
  {
    id: 'barcelona-pawtel-gotic',
    name: 'Pawtel Gotic',
    city: 'Barcelona',
    country: 'Espanya',
    neighborhood: 'Barri Gòtic',
    type: 'hotel',
    shortDescription: 'Hotel pet-friendly per escapades urbanes i estades curtes.',
    description:
      'Hotel boutique amb habitacions àmplies, llits per a mascotes sota petició i bona connexió amb zones de passeig a peu.',
    imageUrl:
      'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.8,
    reviewCount: 312,
    priceLabel: '145-190 € per nit',
    petPolicyLabel: 'Suplement fix per mascota i nit',
    tags: ['hotel', 'boutique', 'ciutat'],
    address: 'Via Laietana 44, Barcelona',
    petNotes: 'Accepta gossos i gats petits. Suplement fix per nit.',
    features: ['Habitacions pet-friendly', 'Recepció 24h', 'Late check-out'],
    coordinates: {
      lat: 41.386602,
      lng: 2.177442
    }
  },
  {
    id: 'madrid-latido-park',
    name: 'Latido Park',
    city: 'Madrid',
    country: 'Espanya',
    neighborhood: 'Paisaje del Arte',
    type: 'park',
    shortDescription: 'Espai verd per passejar, jugar i descansar al mig de la ciutat.',
    description:
      'Parc obert amb ombra, zones de descans i accés fàcil des de barris residencials. Molt útil per sortides curtes o trobades amb altres mascotes.',
    imageUrl:
      'https://images.unsplash.com/photo-1511497584788-876760111969?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.4,
    reviewCount: 541,
    priceLabel: 'Accés gratuït',
    petPolicyLabel: 'Corretja recomanada en hores punta',
    tags: ['parc', 'ombra', 'passeig'],
    address: 'Paseo del Prado, Madrid',
    petNotes: 'Millor franja al matí o cap al vespre quan hi ha menys calor.',
    features: ['Zona verda', 'Bancs', 'Accés fàcil'],
    coordinates: {
      lat: 40.415363,
      lng: -3.692145
    }
  },
  {
    id: 'madrid-casa-larga',
    name: 'Casa Larga',
    city: 'Madrid',
    country: 'Espanya',
    neighborhood: 'Malasaña',
    type: 'apartment',
    shortDescription: 'Apartament pensat per estades llargues amb mascotes.',
    description:
      'Allotjament ampli amb cuina equipada, ascensor i flexibilitat per a estades de diverses setmanes amb animals de companyia.',
    imageUrl:
      'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.5,
    reviewCount: 126,
    priceLabel: '110-150 € per nit',
    petPolicyLabel: 'Ideal per estades llargues amb una mascota',
    tags: ['apartament', 'estades llargues', 'família'],
    address: 'Calle del Pez 17, Madrid',
    petNotes: 'Ideal per gossos tranquils i gats d’interior.',
    features: ['Cuina equipada', 'Rentadora', 'Check-in autònom'],
    coordinates: {
      lat: 40.423672,
      lng: -3.705118
    }
  },
  {
    id: 'lisboa-mar-calma',
    name: 'Mar Calma',
    city: 'Lisboa',
    country: 'Portugal',
    neighborhood: 'Santos',
    type: 'restaurant',
    shortDescription: 'Restaurant amb terrassa tranquil·la i ritme relaxat.',
    description:
      'Espai lluminós amb cuina mediterrània, seients còmodes i personal acostumat a rebre clients amb mascotes.',
    imageUrl:
      'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.3,
    reviewCount: 204,
    priceLabel: '25-40 € per persona',
    petPolicyLabel: 'Terrassa oberta a gossos petits i mitjans',
    tags: ['terrassa', 'sopar', 'barri'],
    address: 'Rua da Esperança 12, Lisboa',
    petNotes: 'Gossos de mida petita i mitjana sense problema a la terrassa.',
    features: ['Terrassa', 'Reserves ràpides', 'Ambient tranquil'],
    coordinates: {
      lat: 38.709847,
      lng: -9.160219
    }
  },
  {
    id: 'lisboa-siesta-stay',
    name: 'Siesta Stay',
    city: 'Lisboa',
    country: 'Portugal',
    neighborhood: 'Cais do Sodré',
    type: 'hotel',
    shortDescription: 'Hotel còmode amb recepció simple i espais amplis.',
    description:
      'Hotel modern per a viatges curts amb mascotes, amb habitacions àmplies i un procés d’arribada senzill.',
    imageUrl:
      'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.7,
    reviewCount: 228,
    priceLabel: '130-175 € per nit',
    petPolicyLabel: 'Una mascota per habitació amb suplement moderat',
    tags: ['hotel', 'escapada', 'còmode'],
    address: 'Avenida 24 de Julho 81, Lisboa',
    petNotes: 'Accepta una mascota per habitació amb suplement moderat.',
    features: ['Check-in ràpid', 'Habitacions grans', 'Servei amable'],
    coordinates: {
      lat: 38.706904,
      lng: -9.153894
    }
  },
  {
    id: 'berlin-kiez-cafe',
    name: 'Kiez Cafe',
    city: 'Berlin',
    country: 'Alemanya',
    neighborhood: 'Neukölln',
    type: 'service',
    shortDescription: 'Espai híbrid de cafè i servei local pet-friendly.',
    description:
      'Un lloc flexible per descansar, treballar una estona i trobar informació o serveis útils per moure’t amb mascota per la ciutat.',
    imageUrl:
      'https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: false,
    rating: 4.2,
    reviewCount: 89,
    priceLabel: 'Consumició mínima 8 €',
    petPolicyLabel: 'Gossos tranquils benvinguts a l’interior',
    tags: ['cafè', 'serveis', 'barri'],
    address: 'Weserstraße 33, Berlin',
    petNotes: 'Gossos tranquils benvinguts, especialment entre setmana.',
    features: ['Wi-Fi', 'Zona interior', 'Ambient local'],
    coordinates: {
      lat: 52.490876,
      lng: 13.424612
    }
  },
  {
    id: 'berlin-grunhof',
    name: 'Grunhof',
    city: 'Berlin',
    country: 'Alemanya',
    neighborhood: 'Prenzlauer Berg',
    type: 'apartment',
    shortDescription: 'Apartament pet-friendly per viure la ciutat sense presses.',
    description:
      'Allotjament lluminós amb accés fàcil a zones verdes, pensat per a estades de diversos dies amb mascotes.',
    imageUrl:
      'https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 4.9,
    reviewCount: 174,
    priceLabel: '120-165 € per nit',
    petPolicyLabel: 'Mascotes admeses sense tràmit extra',
    tags: ['apartament', 'barri tranquil', 'llarga estada'],
    address: 'Oderberger Straße 70, Berlin',
    petNotes: 'Ideal per mascotes acostumades a espais interiors tranquils.',
    features: ['Self check-in', 'Zona tranquil·la', 'Pet-friendly real'],
    coordinates: {
      lat: 52.541953,
      lng: 13.402631
    }
  }
];
