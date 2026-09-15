import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { TotpSecurityPageComponent } from './totp-security-page.component';

describe('TotpSecurityPageComponent', () => {
  const auth = {
    currentUser: () => ({ isTotpEnabled: false }),
    startTotpSetup: vi.fn(), confirmTotpSetup: vi.fn(),
    regenerateTotpRecoveryCodes: vi.fn(), disableTotp: vi.fn()
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    auth.startTotpSetup.mockResolvedValue({ qrSvg: '<svg></svg>', manualEntryKey: 'BASE32KEY' });
    auth.confirmTotpSetup.mockResolvedValue(['RECOVERY01', 'RECOVERY02']);
    await TestBed.configureTestingModule({
      imports: [TotpSecurityPageComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }]
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
});
