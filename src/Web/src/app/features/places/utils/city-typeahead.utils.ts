/**
 * Strips a trailing " (country)" label from the city typeahead so filters store the plain city
 * name that matches persisted place data.
 */
export function extractCityNameFromTypeaheadValue(value: string): string {
  const trimmed = stripPostalPrefixFromCity(value.trim());
  const parsed = splitCityAndCountryLabel(trimmed);
  return parsed.city;
}

/** Google ingest may include Spanish, Portuguese, Dutch or Australian postal fragments. */
export function stripPostalPrefixFromCity(city: string): string {
  return city
    .trim()
    .replace(/^\d{4}-\d{3}\s+/, '')
    .replace(/^\d{4}\s+[A-Z]{2}\s+/, '')
    .replace(/^\d{4,5}\s+/, '')
    .replace(/\s+[A-Z]{2,3}\s+\d{4}$/, '')
    .trim();
}

export function formatCityDisplayLabel(city: string, country = ''): string {
  const name = stripPostalPrefixFromCity(city);
  if (!name) {
    return '';
  }

  const countryLabel = country.trim();
  return countryLabel ? `${name} (${countryLabel})` : name;
}

/** First group wins. Same city+country (ignoring postal prefix and case) is kept once. */
export function mergeCityLabelsDistinct(...groups: readonly (readonly string[])[]): string[] {
  const seen = new Set<string>();
  const result: string[] = [];
  for (const group of groups) {
    for (const raw of group) {
      const label = raw.trim();
      if (!label) {
        continue;
      }

      const key = cityOptionDistinctKey(label);
      if (seen.has(key)) {
        continue;
      }

      seen.add(key);
      result.push(label);
    }
  }

  return result;
}

export function filterCityLabels(labels: readonly string[], query: string): string[] {
  const q = query.trim().toLowerCase();
  if (!q) {
    return [...labels];
  }

  return labels.filter((city) => city.toLowerCase().includes(q));
}

export function filterCityLabelsByCountry(labels: readonly string[], country: string): string[] {
  const selected = country.trim();
  if (!selected) {
    return [...labels];
  }

  return labels.filter((label) => {
    const itemCountry = splitCityAndCountryLabel(label).country;
    return (
      itemCountry.localeCompare(selected, 'und', { sensitivity: 'base' }) === 0 ||
      itemCountry.toLocaleLowerCase().endsWith(`, ${selected.toLocaleLowerCase()}`)
    );
  });
}

export function cityOptionDistinctKey(label: string): string {
  const parsed = splitCityAndCountryLabel(stripPostalPrefixFromCity(label.trim()));
  return `${parsed.city.toLowerCase()}|${parsed.country.toLowerCase()}`;
}

function splitCityAndCountryLabel(value: string): { city: string; country: string } {
  const match = value.match(/^(.+?)\s+\((.+)\)$/);
  if (match) {
    return { city: match[1].trim(), country: match[2].trim() };
  }

  return { city: value.trim(), country: '' };
}
