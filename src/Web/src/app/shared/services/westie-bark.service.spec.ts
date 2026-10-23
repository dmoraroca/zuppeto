import { TestBed } from '@angular/core/testing';

import { WestieBarkService } from './westie-bark.service';

describe('WestieBarkService', () => {
  const originalAudio = globalThis.Audio;
  let play: ReturnType<typeof vi.fn>;
  let pause: ReturnType<typeof vi.fn>;
  let activeAudio: AudioStub | null;

  class AudioStub {
    public currentTime = 0;
    public preload = '';
    public volume = 1;
    public readonly play = play;
    public readonly pause = pause;
    private readonly listeners = new Map<string, Set<EventListener>>();

    public constructor() {
      activeAudio = this;
    }

    public addEventListener(type: string, listener: EventListener): void {
      const listeners = this.listeners.get(type) ?? new Set<EventListener>();
      listeners.add(listener);
      this.listeners.set(type, listeners);
    }

    public removeEventListener(type: string, listener: EventListener): void {
      this.listeners.get(type)?.delete(listener);
    }

    public emit(type: 'ended' | 'error'): void {
      for (const listener of [...(this.listeners.get(type) ?? [])]) listener(new Event(type));
    }
  }

  beforeEach(() => {
    activeAudio = null;
    play = vi.fn().mockImplementation(() => {
      queueMicrotask(() => activeAudio?.emit('ended'));
      return Promise.resolve();
    });
    pause = vi.fn();
    globalThis.Audio = AudioStub as unknown as typeof Audio;
    TestBed.resetTestingModule();
  });

  afterEach(() => {
    globalThis.Audio = originalAudio;
  });

  it('attempts both route barks without suppressing them behind an internal unlock flag', async () => {
    vi.useFakeTimers();
    const service = TestBed.inject(WestieBarkService);

    expect(service.userActivationReady()).toBe(false);
    const result = service.playAtStartOfRoute();
    await vi.runAllTimersAsync();
    expect(await result).toBe(true);
    expect(service.userActivationReady()).toBe(true);
    expect(play).toHaveBeenCalledTimes(2);
    vi.useRealTimers();
  });

  it('previews once and unlocks later route barks without overlapping audio', async () => {
    vi.useFakeTimers();
    const service = TestBed.inject(WestieBarkService);

    const preview = service.previewFromUserAction();
    await vi.runAllTimersAsync();
    expect(await preview).toBe(true);
    expect(service.userActivationReady()).toBe(true);
    const route = service.playAtStartOfRoute();
    await vi.runAllTimersAsync();
    expect(await route).toBe(true);

    expect(play).toHaveBeenCalledTimes(4);
    expect(pause).toHaveBeenCalledTimes(2);
    vi.useRealTimers();
  });

  it('silently unlocks the persisted sound preference from an ordinary user interaction', async () => {
    vi.useFakeTimers();
    const service = TestBed.inject(WestieBarkService);

    expect(await service.unlockFromUserAction()).toBe(true);
    expect(service.userActivationReady()).toBe(true);
    const route = service.playAtStartOfRoute();
    await vi.runAllTimersAsync();
    expect(await route).toBe(true);

    expect(play).toHaveBeenCalledTimes(3);
    expect(pause).toHaveBeenCalledTimes(3);
    vi.useRealTimers();
  });

  it('plays exactly two complete barks on each of five consecutive activations', async () => {
    vi.useFakeTimers();
    const service = TestBed.inject(WestieBarkService);

    for (let activation = 0; activation < 5; activation += 1) {
      const result = service.previewFromUserAction();
      await vi.runAllTimersAsync();
      expect(await result).toBe(true);
    }

    expect(play).toHaveBeenCalledTimes(10);
    vi.useRealTimers();
  });

  it('cancels an unfinished sequence before starting another one', async () => {
    vi.useFakeTimers();
    play.mockResolvedValue(undefined);
    const service = TestBed.inject(WestieBarkService);

    const first = service.previewFromUserAction();
    const second = service.previewFromUserAction();
    expect(await first).toBe(false);
    activeAudio?.emit('ended');
    await vi.runAllTimersAsync();
    activeAudio?.emit('ended');
    expect(await second).toBe(true);

    expect(play).toHaveBeenCalledTimes(3);
    vi.useRealTimers();
  });

  it('does not unlock route barks when the browser rejects playback', async () => {
    play.mockRejectedValue(new DOMException('Blocked', 'NotAllowedError'));
    const service = TestBed.inject(WestieBarkService);

    expect(await service.previewFromUserAction()).toBe(false);
    expect(service.userActivationReady()).toBe(false);
    expect(await service.playAtStartOfRoute()).toBe(false);
    expect(play).toHaveBeenCalledTimes(2);
  });
});
