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

    expect(service.lifeLineVisible()).toBe(true);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(true);
  });

  it('persists both preferences independently in the browser', () => {
    const service = TestBed.inject(WestiePreferencesService);

    service.setLifeLineVisible(false);
    service.setSoundEnabled(true);

    expect(localStorage.getItem('petiloc.westie.visible')).toBe('false');
    expect(localStorage.getItem('petiloc.westie.sound')).toBe('true');
    expect(service.lifeLineVisible()).toBe(false);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(false);
  });

  it('restores the previous bark preference when the life line is enabled again', () => {
    const service = TestBed.inject(WestiePreferencesService);

    service.setSoundEnabled(true);
    service.setLifeLineVisible(false);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(false);

    service.setLifeLineVisible(true);
    expect(service.soundEnabled()).toBe(true);
    expect(service.soundActive()).toBe(true);
  });
});
