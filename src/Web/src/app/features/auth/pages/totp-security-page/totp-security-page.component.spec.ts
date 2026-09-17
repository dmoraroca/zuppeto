import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { GoogleIdentityService } from '../../services/google-identity.service';
import { TotpSecurityPageComponent } from './totp-security-page.component';

describe('TotpSecurityPageComponent', () => {
  const auth = {
    currentUser: () => ({ isTotpEnabled: false }),
    startTotpSetup: vi.fn(), confirmTotpSetup: vi.fn(),
    regenerateTotpRecoveryCodes: vi.fn(), disableTotp: vi.fn(),
    getAccessMethods: vi.fn(), getProviders: vi.fn(), linkGoogle: vi.fn()
  };
  let googleCallback: ((credential: string) => void) | null;
  const googleIdentity = {
    renderButton: vi.fn((_host: HTMLElement, _clientId: string, _mode: 'LOGIN' | 'LINK', callback: (credential: string) => void) => {
      googleCallback = callback;
      return Promise.resolve(() => undefined);
    })
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    googleCallback = null;
    auth.startTotpSetup.mockResolvedValue({ qrSvg: '<svg></svg>', manualEntryKey: 'BASE32KEY' });
    auth.confirmTotpSetup.mockResolvedValue(['RECOVERY01', 'RECOVERY02']);
    auth.getAccessMethods.mockResolvedValue([
      { provider: 'password', displayName: 'Contrasenya', linked: true, available: true, status: 'linked' },
      { provider: 'google', displayName: 'Google', linked: false, available: true, status: 'linkable' },
      { provider: 'linkedin', displayName: 'LinkedIn', linked: false, available: false, status: 'pending' },
      { provider: 'facebook', displayName: 'Facebook', linked: false, available: false, status: 'pending' }
    ]);
    auth.getProviders.mockResolvedValue([{ key: 'google', configured: true, clientId: 'client-id' }]);
    auth.linkGoogle.mockResolvedValue({ ok: true, alreadyLinked: false });
    await TestBed.configureTestingModule({
      imports: [TotpSecurityPageComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: GoogleIdentityService, useValue: googleIdentity }
      ]
    }).compileComponents();
  });

  it('guides setup and only displays recovery codes after valid confirmation', async () => {
    const fixture = TestBed.createComponent(TotpSecurityPageComponent);
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('BASE32KEY');

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '123456'; input.dispatchEvent(new Event('input')); fixture.detectChanges();
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    await fixture.whenStable(); fixture.detectChanges();
    expect(auth.confirmTotpSetup).toHaveBeenCalledWith('123456');
    expect(fixture.nativeElement.textContent).toContain('RECOVERY01');
    expect(fixture.nativeElement.textContent).toContain('està activada');
  });

  it('shows all access methods and explicitly links Google to the authenticated account', async () => {
    const fixture = TestBed.createComponent(TotpSecurityPageComponent);
    fixture.detectChanges();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Contrasenya');
    });

    expect(fixture.nativeElement.textContent).toContain('Google');
    expect(fixture.nativeElement.textContent).toContain('LinkedIn');
    expect(fixture.nativeElement.textContent).toContain('Facebook');
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(googleIdentity.renderButton).toHaveBeenCalled();
    });

    googleCallback?.('verified-google-token');
    await fixture.whenStable(); fixture.detectChanges();

    expect(auth.linkGoogle).toHaveBeenCalledWith('verified-google-token');
    expect(fixture.nativeElement.textContent).toContain('Google s’ha vinculat correctament.');
    expect(fixture.nativeElement.textContent).toContain('Vinculat');
  });
});
