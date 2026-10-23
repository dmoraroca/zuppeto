import { Injectable, signal } from '@angular/core';

const WESTIE_BARK_URL = '/audio/westie-bark.mp3';
const BARK_GAP_MS = 180;

@Injectable({ providedIn: 'root' })
export class WestieBarkService {
  private audio: HTMLAudioElement | null = null;
  private unlockedByUser = false;
  private unlockInProgress: Promise<boolean> | null = null;
  private finishCurrentBark: ((completed: boolean) => void) | null = null;
  private playbackSequence = 0;
  private readonly userActivationState = signal(false);

  public readonly userActivationReady = this.userActivationState.asReadonly();

  public unlockFromUserAction(): Promise<boolean> {
    if (this.unlockedByUser) return Promise.resolve(true);
    if (this.unlockInProgress) return this.unlockInProgress;

    this.unlockInProgress = this.unlockSilently().finally(() => {
      this.unlockInProgress = null;
    });
    return this.unlockInProgress;
  }

  public async previewFromUserAction(): Promise<boolean> {
    const played = await this.playBarks(2);
    if (played) this.markAsUnlocked();
    return played;
  }

  public async playAtStartOfRoute(): Promise<boolean> {
    const played = await this.playBarks(2);
    if (played) this.markAsUnlocked();
    return played;
  }

  public stop(): void {
    this.playbackSequence += 1;
    this.finishCurrentBark?.(false);
    this.finishCurrentBark = null;
    if (!this.audio) return;
    this.audio.pause();
    this.audio.currentTime = 0;
  }

  private async playBarks(count: number): Promise<boolean> {
    if (typeof Audio === 'undefined' || (typeof document !== 'undefined' && document.hidden)) {
      return false;
    }

    this.audio ??= this.createAudio();
    this.stop();
    const sequence = this.playbackSequence;

    for (let index = 0; index < count; index += 1) {
      if (sequence !== this.playbackSequence) return false;
      if (!await this.playOnceToEnd(this.audio)) return false;
      this.audio.currentTime = 0;
      if (index < count - 1) {
        await new Promise(resolve => setTimeout(resolve, BARK_GAP_MS));
      }
    }
    return sequence === this.playbackSequence;
  }

  private playOnceToEnd(audio: HTMLAudioElement): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      let settled = false;
      const finish = (completed: boolean): void => {
        if (settled) return;
        settled = true;
        audio.removeEventListener('ended', onEnded);
        audio.removeEventListener('error', onError);
        if (this.finishCurrentBark === finish) this.finishCurrentBark = null;
        resolve(completed);
      };
      const onEnded = (): void => finish(true);
      const onError = (): void => finish(false);
      this.finishCurrentBark = finish;
      audio.addEventListener('ended', onEnded, { once: true });
      audio.addEventListener('error', onError, { once: true });

      void audio.play().catch(() => finish(false));
    });
  }

  private async unlockSilently(): Promise<boolean> {
    if (typeof Audio === 'undefined' || (typeof document !== 'undefined' && document.hidden)) {
      return false;
    }

    this.audio ??= this.createAudio();
    const volume = this.audio.volume;
    this.stop();
    this.audio.volume = 0;

    try {
      await this.audio.play();
      this.audio.pause();
      this.audio.currentTime = 0;
      this.markAsUnlocked();
      return true;
    } catch {
      return false;
    } finally {
      this.audio.volume = volume;
    }
  }

  private createAudio(): HTMLAudioElement {
    const audio = new Audio(WESTIE_BARK_URL);
    audio.preload = 'auto';
    audio.volume = 0.5;
    return audio;
  }

  private markAsUnlocked(): void {
    this.unlockedByUser = true;
    this.userActivationState.set(true);
  }
}
