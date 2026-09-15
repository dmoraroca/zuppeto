import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { TotpChallengePageComponent } from './totp-challenge-page.component';

describe('TotpChallengePageComponent', () => {
  const auth = { completeTotpLogin: vi.fn(), getPostLoginRoute: () => '/perfil' };
  const router = { navigateByUrl: vi.fn() };
  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [TotpChallengePageComponent],
      providers: [
        { provide: AuthService, useValue: auth }, { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: (key: string) => key === 'challenge' ? 'challenge-id' : null } } } }
      ]
    }).compileComponents();
  });

  it('submits the challenge and navigates only after successful verification', async () => {
    auth.completeTotpLogin.mockResolvedValue(true);
    const fixture = TestBed.createComponent(TotpChallengePageComponent); fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '123456'; input.dispatchEvent(new Event('input')); fixture.detectChanges();
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    expect(auth.completeTotpLogin).toHaveBeenCalledWith('challenge-id', '123456');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/perfil');
  });
});
