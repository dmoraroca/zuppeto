const fallback = (value: string | null | undefined, labels: Readonly<Record<string, string>>, empty = '—'): string =>
  value ? labels[value] ?? value : empty;

export const territorialStatusLabel = (value: string | null | undefined): string => fallback(value, {
  Queued: 'En cua', Uploaded: 'Fitxer rebut', Mapped: 'Mapping completat', Validated: 'Validat',
  ReadyForReview: 'Preparat per revisar', Publishing: 'Publicant', Published: 'Publicat',
  Failed: 'Error', Cancelled: 'Cancel·lat', Reverted: 'Revertit'
}, 'Pendent');

export const territorialStageLabel = (value: string | null | undefined): string => fallback(value, {
  Artifact: 'Artefacte', Reading: 'Lectura', Staging: 'Preparació', Canonicalization: 'Canonicalització',
  Validation: 'Validació', ChangeSet: 'Preparació dels canvis', Publication: 'Publicació'
}, 'Pendent');

export const territorialActionLabel = (value: string): string => fallback(value, {
  Create: 'Crear', Update: 'Actualitzar', Deactivate: 'Desactivar', NoChange: 'Sense canvis'
});

export const territorialTypeLabel = (value: string): string => fallback(value, {
  AUTONOMOUS_COMMUNITY: 'Comunitat autònoma', AUTONOMOUS_CITY_MUNICIPALITY: 'Ciutat autònoma',
  PROVINCE: 'Província', MUNICIPALITY: 'Municipi', BUNDESLAND: 'Bundesland',
  REGIERUNGSBEZIRK: 'Regierungsbezirk', REGION: 'Regió', KREIS: 'Kreis',
  KREISFREIE_STADT: 'Ciutat independent', GEMEINDEVERBAND: 'Gemeindeverband',
  GEMEINDE: 'Gemeinde', SPECIAL_TERRITORY: 'Territori especial'
});

export const territorialFieldLabel = (value: string): string => fallback(value, {
  names: 'Noms', codes: 'Codis', territorialUnitType: 'Tipus', isActive: 'Estat',
  coordinates: 'Coordenades', parent: 'Unitat superior', unit: 'Unitat territorial'
});

export const territorialSeverityLabel = (value: string): string => fallback(value, { Error: 'Error', Warning: 'Avís' });
export const territorialApprovalLabel = (value: string): string => fallback(value, { Pending: 'Pendent', Approved: 'Aprovada', Rejected: 'Rebutjada' });
export const territorialPublicationModeLabel = (value: string): string => fallback(value, { FullSnapshot: 'Catàleg complet', Delta: 'Actualització parcial' });
export const territorialNameKindLabel = (value: string): string => fallback(value, { Official: 'Oficial', Alternative: 'Alternatiu', Historical: 'Històric' });
