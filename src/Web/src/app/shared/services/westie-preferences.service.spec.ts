import { TestBed } from '@angular/core/testing';

import { WestiePreferencesService } from './westie-preferences.service';

describe('WestiePreferencesService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  afterEach(() => localStorage.clear());

  it('enables the Westie, sound and life line by default', () => {
    const service = TestBed.inject(WestiePreferencesService);

    expect(service.westieVisible()).toBe(true);
    expect(service.lifeLineVisible()).toBe(true);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(true);
  });

  it('persists the three preferences independently in the browser', () => {
    const service = TestBed.inject(WestiePreferencesService);

    service.setWestieVisible(false);
    service.setLifeLineVisible(false);
    service.setSoundEnabled(true);

    expect(localStorage.getItem('petiloc.westie.visible')).toBe('false');
    expect(localStorage.getItem('petiloc.westie.sound')).toBe('true');
    expect(localStorage.getItem('petiloc.life-line.visible')).toBe('false');
    expect(service.westieVisible()).toBe(false);
    expect(service.lifeLineVisible()).toBe(false);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(false);
  });

  it('restores the previous bark preference when the Westie is enabled again', () => {
    const service = TestBed.inject(WestiePreferencesService);

    service.setSoundEnabled(true);
    service.setWestieVisible(false);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(false);

    service.setWestieVisible(true);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(true);
  });

  it('keeps the life line independent from the Westie and bark preferences', () => {
    const service = TestBed.inject(WestiePreferencesService);

    service.setLifeLineVisible(false);

    expect(service.lifeLineVisible()).toBe(false);
    expect(service.westieVisible()).toBe(true);
    expect(service.soundActive()).toBe(true);
  });
});
