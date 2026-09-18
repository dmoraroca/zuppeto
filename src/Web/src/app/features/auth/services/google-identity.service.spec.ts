import { GoogleIdentityService } from './google-identity.service';

describe('GoogleIdentityService', () => {
  type GoogleIdApi = NonNullable<Window['google']>['accounts']['id'];
  type Initialize = GoogleIdApi['initialize'];
  type RenderButton = GoogleIdApi['renderButton'];
  let initialize: ReturnType<typeof vi.fn<Initialize>>;
  let renderButton: ReturnType<typeof vi.fn<RenderButton>>;
  let disableAutoSelect: ReturnType<typeof vi.fn<GoogleIdApi['disableAutoSelect']>>;
  let globalCallback: ((response: { credential: string; state?: string }) => void) | undefined;

  beforeEach(() => {
    initialize = vi.fn((options: { callback: (response: { credential: string; state?: string }) => void }) => {
      globalCallback = options.callback;
    });
    renderButton = vi.fn();
    disableAutoSelect = vi.fn();
    window.google = {
      accounts: {
        id: {
          initialize: (options) => initialize(options),
          renderButton: (parent, options) => renderButton(parent, options),
          disableAutoSelect: () => disableAutoSelect()
        }
      }
    };
  });

  afterEach(() => {
    delete window.google;
  });

  it('initializes GIS once and dispatches to the currently mounted LINK control', async () => {
    const service = new GoogleIdentityService();
    const login = vi.fn();
    const link = vi.fn();
    const loginHost = document.createElement('div');
    const linkHost = document.createElement('div');

    const disposeLogin = await service.renderButton(loginHost, 'client-id', 'LOGIN', login);
    disposeLogin();
    await service.renderButton(linkHost, 'client-id', 'LINK', link);

    expect(initialize).toHaveBeenCalledTimes(1);
    globalCallback?.({ credential: 'link-token' });

    expect(link).toHaveBeenCalledWith('link-token');
    expect(login).not.toHaveBeenCalled();
  });

  it('replaces a stale LOGIN registration when the LINK control is mounted', async () => {
    const service = new GoogleIdentityService();
    const login = vi.fn();
    const link = vi.fn();

    await service.renderButton(document.createElement('div'), 'client-id', 'LOGIN', login);
    await service.renderButton(document.createElement('div'), 'client-id', 'LINK', link);
    globalCallback?.({ credential: 'link-token' });

    expect(link).toHaveBeenCalledWith('link-token');
    expect(login).not.toHaveBeenCalled();
  });

  it('does not consume credentials after the active control is destroyed', async () => {
    const service = new GoogleIdentityService();
    const login = vi.fn();

    const dispose = await service.renderButton(document.createElement('div'), 'client-id', 'LOGIN', login);
    dispose();
    globalCallback?.({ credential: 'login-token' });

    expect(login).not.toHaveBeenCalled();
  });

  it('does not intercept the official GIS button click', async () => {
    const service = new GoogleIdentityService();

    await service.renderButton(document.createElement('div'), 'client-id', 'LOGIN', vi.fn());

    expect(renderButton.mock.calls[0][1]).not.toHaveProperty('click_listener');
    expect(renderButton.mock.calls[0][1]).not.toHaveProperty('state');
  });

  it('uses the official GIS sign-out control when available', async () => {
    const service = new GoogleIdentityService();

    await service.disableAutoSelect();

    expect(disableAutoSelect).toHaveBeenCalledOnce();
  });
});
