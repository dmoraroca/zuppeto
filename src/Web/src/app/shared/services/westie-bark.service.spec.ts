import { TestBed } from '@angular/core/testing';

import { WestieBarkService } from './westie-bark.service';

describe('WestieBarkService', () => {
  const originalAudio = globalThis.Audio;
  let play: ReturnType<typeof vi.fn>;
  let pause: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    play = vi.fn().mockResolvedValue(undefined);
    pause = vi.fn();
    class AudioStub {
      public currentTime = 0;
      public preload = '';
      public volume = 1;
      public readonly play = play;
      public readonly pause = pause;
    }
    globalThis.Audio = AudioStub as unknown as typeof Audio;
    TestBed.resetTestingModule();
  });

  afterEach(() => {
    globalThis.Audio = originalAudio;
  });

  it('does not bark at the end before a voluntary user preview', async () => {
    const service = TestBed.inject(WestieBarkService);

    expect(await service.playAtEndOfRoute()).toBe(false);
    expect(play).not.toHaveBeenCalled();
  });

  it('previews once and unlocks later route barks without overlapping audio', async () => {
    const service = TestBed.inject(WestieBarkService);

    expect(await service.previewFromUserAction()).toBe(true);
    expect(await service.playAtEndOfRoute()).toBe(true);

    expect(play).toHaveBeenCalledTimes(2);
    expect(pause).toHaveBeenCalledTimes(2);
  });

  it('does not unlock route barks when the browser rejects playback', async () => {
    play.mockRejectedValue(new DOMException('Blocked', 'NotAllowedError'));
    const service = TestBed.inject(WestieBarkService);

    expect(await service.previewFromUserAction()).toBe(false);
    expect(await service.playAtEndOfRoute()).toBe(false);
    expect(play).toHaveBeenCalledTimes(1);
  });
});
