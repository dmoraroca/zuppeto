import { Injectable } from '@angular/core';

const WESTIE_BARK_URL = '/audio/westie-bark.mp3';

@Injectable({ providedIn: 'root' })
export class WestieBarkService {
  private audio: HTMLAudioElement | null = null;
  private unlockedByUser = false;

  public async previewFromUserAction(): Promise<boolean> {
    const played = await this.play();
    if (played) this.unlockedByUser = true;
    return played;
  }

  public async playAtEndOfRoute(): Promise<boolean> {
    if (!this.unlockedByUser) return false;
    return this.play();
  }

  public stop(): void {
    if (!this.audio) return;
    this.audio.pause();
    this.audio.currentTime = 0;
  }

  private async play(): Promise<boolean> {
    if (typeof Audio === 'undefined' || (typeof document !== 'undefined' && document.hidden)) {
      return false;
    }

    this.audio ??= this.createAudio();
    this.audio.pause();
    this.audio.currentTime = 0;

    try {
      await this.audio.play();
      return true;
    } catch {
      return false;
    }
  }

  private createAudio(): HTMLAudioElement {
    const audio = new Audio(WESTIE_BARK_URL);
    audio.preload = 'auto';
    audio.volume = 0.5;
    return audio;
  }
}
