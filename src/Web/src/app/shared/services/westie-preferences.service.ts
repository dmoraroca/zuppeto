import { computed, Injectable, signal } from '@angular/core';

const WESTIE_VISIBLE_KEY = 'petiloc.westie.visible';
const WESTIE_SOUND_KEY = 'petiloc.westie.sound';

@Injectable({ providedIn: 'root' })
export class WestiePreferencesService {
  private readonly visibleState = signal(this.read(WESTIE_VISIBLE_KEY));
  private readonly soundState = signal(this.read(WESTIE_SOUND_KEY));

  public readonly lifeLineVisible = this.visibleState.asReadonly();
  public readonly soundEnabled = this.soundState.asReadonly();
  public readonly soundActive = computed(() => this.lifeLineVisible() && this.soundEnabled());

  public setLifeLineVisible(visible: boolean): void {
    this.visibleState.set(visible);
    this.write(WESTIE_VISIBLE_KEY, visible);
  }

  public setSoundEnabled(enabled: boolean): void {
    this.soundState.set(enabled);
    this.write(WESTIE_SOUND_KEY, enabled);
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
