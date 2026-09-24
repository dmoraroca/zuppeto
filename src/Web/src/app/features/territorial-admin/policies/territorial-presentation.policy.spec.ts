import { describe, expect, it } from 'vitest';

import {
  territorialActionLabel, territorialFieldLabel, territorialStageLabel,
  territorialStatusLabel, territorialTypeLabel
} from './territorial-presentation.policy';

describe('territorial presentation policy', () => {
  it('translates every persisted import status and stage without changing its value', () => {
    expect(['Queued', 'Uploaded', 'Mapped', 'Validated', 'ReadyForReview', 'Publishing', 'Published', 'Failed', 'Cancelled', 'Reverted']
      .map(territorialStatusLabel)).toEqual([
        'En cua', 'Fitxer rebut', 'Mapping completat', 'Validat', 'Preparat per revisar', 'Publicant',
        'Publicat', 'Error', 'Cancel·lat', 'Revertit'
      ]);
    expect(territorialStageLabel('ChangeSet')).toBe('Preparació dels canvis');
  });

  it('uses functional labels for actions, types and before/after fields', () => {
    expect(['Create', 'Update', 'Deactivate', 'NoChange'].map(territorialActionLabel))
      .toEqual(['Crear', 'Actualitzar', 'Desactivar', 'Sense canvis']);
    expect(territorialTypeLabel('MUNICIPALITY')).toBe('Municipi');
    expect(territorialFieldLabel('parent')).toBe('Unitat superior');
  });
});
