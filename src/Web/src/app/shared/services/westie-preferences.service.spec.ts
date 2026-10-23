import { TestBed } from '@angular/core/testing';

import { WestiePreferencesService } from './westie-preferences.service';

describe('WestiePreferencesService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  afterEach(() => localStorage.clear());

  it('enables visibility and sound by default', () => {
    const service = TestBed.inject(WestiePreferencesService);

    expect(service.visible()).toBe(true);
    expect(service.soundEnabled()).toBe(true);
  });

  it('persists both preferences independently in the browser', () => {
    const service = TestBed.inject(WestiePreferencesService);

    service.setVisible(false);
    service.setSoundEnabled(true);

    expect(localStorage.getItem('petiloc.westie.visible')).toBe('false');
    expect(localStorage.getItem('petiloc.westie.sound')).toBe('true');
    expect(service.visible()).toBe(false);
    expect(service.soundEnabled()).toBe(true);
  });
});
