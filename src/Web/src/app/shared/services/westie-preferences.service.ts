import { computed, Injectable, signal } from '@angular/core';

const WESTIE_VISIBLE_KEY = 'petiloc.westie.visible';
const WESTIE_SOUND_KEY = 'petiloc.westie.sound';
const LIFE_LINE_VISIBLE_KEY = 'petiloc.life-line.visible';

@Injectable({ providedIn: 'root' })
export class WestiePreferencesService {
  private readonly westieVisibleState = signal(this.read(WESTIE_VISIBLE_KEY));
  private readonly soundState = signal(this.read(WESTIE_SOUND_KEY));
  private readonly lifeLineVisibleState = signal(this.read(LIFE_LINE_VISIBLE_KEY));

  public readonly westieVisible = this.westieVisibleState.asReadonly();
  public readonly soundEnabled = this.soundState.asReadonly();
  public readonly lifeLineVisible = this.lifeLineVisibleState.asReadonly();
  public readonly soundActive = computed(() => this.westieVisible() && this.soundEnabled());

  public setWestieVisible(visible: boolean): void {
    this.westieVisibleState.set(visible);
    this.write(WESTIE_VISIBLE_KEY, visible);
  }

  public setSoundEnabled(enabled: boolean): void {
    this.soundState.set(enabled);
    this.write(WESTIE_SOUND_KEY, enabled);
  }

  public setLifeLineVisible(visible: boolean): void {
    this.lifeLineVisibleState.set(visible);
    this.write(LIFE_LINE_VISIBLE_KEY, visible);
  }

  private read(key: string): boolean {
    if (typeof localStorage === 'undefined') return true;
    const stored = localStorage.getItem(key);
    return stored === null ? true : stored === 'true';
  }

  private write(key: string, value: boolean): void {
    if (typeof localStorage !== 'undefined') localStorage.setItem(key, String(value));
  }
}
