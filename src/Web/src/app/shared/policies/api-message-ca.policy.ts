/** Maps leftover English API bodies to Catalan until the process is rebuilt. */
const EXACT_EN_TO_CA: Record<string, string> = {
  'City name already exists in this country.':
    'Ja existeix una ciutat amb aquest nom en aquest país.',
  'Country not found.': 'No s’ha trobat el país.',
  'City not found.': 'No s’ha trobat la ciutat.',
  'Cannot delete a country that still has cities.':
    'No es pot esborrar un país que encara té ciutats.',
  'Unable to create city.': 'No s’ha pogut crear la ciutat.',
  'Unable to update city.': 'No s’ha pogut actualitzar la ciutat.',
  'Unable to create country.': 'No s’ha pogut crear el país.',
  'Unable to update country.': 'No s’ha pogut actualitzar el país.',
  'Unable to delete country.': 'No s’ha pogut esborrar el país.',
  'Unable to create user.': 'No s’ha pogut crear l’usuari.',
  'User not found.': 'No s’ha trobat l’usuari.'
};

const PATTERN_EN_TO_CA: ReadonlyArray<{
  re: RegExp;
  to: (match: RegExpMatchArray) => string;
}> = [
  {
    re: /^Country code '(.+)' is already in use\.$/,
    to: (match) => `El codi «${match[1]}» ja existeix.`
  }
];

export function toCatalanApiMessage(message: string): string {
  const trimmed = message.trim();
  if (!trimmed) {
    return trimmed;
  }

  const exact = EXACT_EN_TO_CA[trimmed];
  if (exact) {
    return exact;
  }

  for (const rule of PATTERN_EN_TO_CA) {
    const match = trimmed.match(rule.re);
    if (match) {
      return rule.to(match);
    }
  }

  return trimmed;
}
