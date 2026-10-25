import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { PlaceSearchTelemetryService } from './place-search-telemetry.service';

describe('PlaceSearchTelemetryService', () => {
  it('records operational metadata without search filters or coordinates', () => {
    const debug = vi.spyOn(console, 'debug').mockImplementation(() => undefined);
    const service = TestBed.inject(PlaceSearchTelemetryService);

    const startedAt = service.started(7);
    service.finished(7, startedAt, 'completed', 12);

    expect(debug).toHaveBeenCalledWith('[PetiLoc places] search started', { requestId: 7 });
    const completion = debug.mock.calls.at(-1)?.[1] as Record<string, unknown>;
    expect(completion).toMatchObject({ requestId: 7, outcome: 'completed', resultCount: 12 });
    expect(completion).not.toHaveProperty('filters');
    expect(completion).not.toHaveProperty('coordinates');
    debug.mockRestore();
  });
});
