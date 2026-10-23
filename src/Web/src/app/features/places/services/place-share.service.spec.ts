import { TestBed } from '@angular/core/testing';

import { PlaceShareService } from './place-share.service';

describe('PlaceShareService', () => {
  let service: PlaceShareService;
  let originalShare: PropertyDescriptor | undefined;
  let originalCanShare: PropertyDescriptor | undefined;
  let originalClipboard: PropertyDescriptor | undefined;
  let originalPrompt: typeof window.prompt;

  beforeEach(() => {
    originalShare = Object.getOwnPropertyDescriptor(navigator, 'share');
    originalCanShare = Object.getOwnPropertyDescriptor(navigator, 'canShare');
    originalClipboard = Object.getOwnPropertyDescriptor(navigator, 'clipboard');
    originalPrompt = window.prompt;
    document.querySelector('meta[name="petiloc-public-origin"]')?.remove();
    TestBed.resetTestingModule();
    service = TestBed.inject(PlaceShareService);
  });

  afterEach(() => {
    restoreProperty(navigator, 'share', originalShare);
    restoreProperty(navigator, 'canShare', originalCanShare);
    restoreProperty(navigator, 'clipboard', originalClipboard);
    window.prompt = originalPrompt;
    document.querySelector('meta[name="petiloc-public-origin"]')?.remove();
  });

  it('shares two different Places consecutively with their exact clean URLs', async () => {
    const share = vi.fn().mockResolvedValue(undefined);
    defineNavigatorProperty('share', share);
    defineNavigatorProperty('canShare', () => true);

    await expect(service.share({ id: 'place-1', name: 'Primer', city: 'Barcelona' }))
      .resolves.toEqual({ status: 'shared', url: localPlaceUrl('place-1') });
    await expect(service.share({ id: 'place/2', name: 'Segon', city: 'Girona' }))
      .resolves.toEqual({ status: 'shared', url: localPlaceUrl('place/2') });

    expect(share).toHaveBeenNthCalledWith(1, {
      title: 'Primer', text: 'Primer · Barcelona', url: localPlaceUrl('place-1')
    });
    expect(share).toHaveBeenNthCalledWith(2, {
      title: 'Segon', text: 'Segon · Girona', url: localPlaceUrl('place/2')
    });
  });

  it('uses the configured public origin without carrying private query parameters', () => {
    const meta = document.createElement('meta');
    meta.name = 'petiloc-public-origin';
    meta.content = 'https://petiloc.example/app?token=private';
    document.head.append(meta);

    expect(service.buildUrl('place-1')).toBe('https://petiloc.example/places/place-1');
  });

  it('treats voluntary native-share cancellation as a neutral result', async () => {
    defineNavigatorProperty('share', vi.fn().mockRejectedValue(new DOMException('Cancelled', 'AbortError')));

    await expect(service.share({ id: 'place-1', name: 'Local', city: 'Vic' }))
      .resolves.toEqual({ status: 'cancelled', url: localPlaceUrl('place-1') });
  });

  it('returns a comprehensible error when native sharing fails', async () => {
    defineNavigatorProperty('share', vi.fn().mockRejectedValue(new DOMException('Denied', 'NotAllowedError')));

    await expect(service.share({ id: 'place-1', name: 'Local', city: 'Vic' }))
      .resolves.toEqual(expect.objectContaining({
        status: 'failed', message: 'No s’ha pogut compartir l’establiment. Torna-ho a provar.'
      }));
  });

  it('copies the URL when Web Share is unavailable', async () => {
    defineNavigatorProperty('share', undefined);
    const writeText = vi.fn().mockResolvedValue(undefined);
    defineNavigatorProperty('clipboard', { writeText });

    await expect(service.share({ id: 'place-1', name: 'Local', city: 'Vic' }))
      .resolves.toEqual({ status: 'copied', url: localPlaceUrl('place-1') });
    expect(writeText).toHaveBeenCalledWith(localPlaceUrl('place-1'));
  });

  it('offers an accessible manual copy when clipboard permission is denied', async () => {
    defineNavigatorProperty('share', undefined);
    defineNavigatorProperty('clipboard', {
      writeText: vi.fn().mockRejectedValue(new DOMException('Denied', 'NotAllowedError'))
    });
    window.prompt = vi.fn();

    await expect(service.share({ id: 'place-1', name: 'Local', city: 'Vic' }))
      .resolves.toEqual(expect.objectContaining({ status: 'manual' }));
    expect(window.prompt).toHaveBeenCalledWith(
      'Copia aquest enllaç:', localPlaceUrl('place-1')
    );
  });

  it('coalesces repeated clicks into one browser operation', async () => {
    let finishShare!: () => void;
    const share = vi.fn().mockImplementation(() => new Promise<void>((resolve) => { finishShare = resolve; }));
    defineNavigatorProperty('share', share);
    const target = { id: 'place-1', name: 'Local', city: 'Vic' };

    const first = service.share(target);
    const second = service.share(target);
    expect(share).toHaveBeenCalledTimes(1);
    finishShare();
    await expect(Promise.all([first, second])).resolves.toHaveLength(2);
  });
});

function defineNavigatorProperty(name: 'share' | 'canShare' | 'clipboard', value: unknown): void {
  Object.defineProperty(navigator, name, { configurable: true, value });
}

function localPlaceUrl(placeId: string): string {
  return new URL(`/places/${encodeURIComponent(placeId)}`, window.location.origin).href;
}

function restoreProperty(target: Navigator, name: 'share' | 'canShare' | 'clipboard', descriptor?: PropertyDescriptor): void {
  if (descriptor) Object.defineProperty(target, name, descriptor);
  else delete (target as unknown as Record<string, unknown>)[name];
}
