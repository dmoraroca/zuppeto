import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { AdminService, PetGovernanceOverviewDto } from '../../services/admin.service';
import { PetGovernancePanelComponent } from './pet-governance-panel.component';

describe('PetGovernancePanelComponent', () => {
  it('mostra procedència, conflictes, caducitats, evidència de cadena i historial', async () => {
    const { fixture } = await setup();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Conflictes pendents');
    expect(text).toContain('Hi ha dades caducades');
    expect(text).toContain('petiloc:manual');
    expect(text).toContain('https://evidence.test/manual');
    expect(text).toContain('https://evidence.test/chain');
    expect(text).toContain('Confirmació presencial');
    expect(fixture.nativeElement.querySelector('[aria-label="Estat de governança"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('valida els camps auditables i envia verificacions, revocacions i resolucions', async () => {
    const { fixture, api } = await setup();
    const component = fixture.componentInstance as unknown as {
      evidenceUri: string; reason: string;
      verify(assertion: PetGovernanceOverviewDto['assertions'][number]): Promise<void>;
      decide(assertion: PetGovernanceOverviewDto['assertions'][number], action: 'question' | 'revoke'): Promise<void>;
      resolve(assertion: PetGovernanceOverviewDto['assertions'][number]): Promise<void>;
    };
    const assertion = overview().assertions[0];

    await component.verify(assertion);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('Cal indicar evidència i motiu');
    expect(api.verifyPetAssertion).not.toHaveBeenCalled();

    component.evidenceUri = 'https://evidence.test/new';
    component.reason = 'Comprovació funcional';
    await component.verify(assertion);
    await component.resolve(assertion);
    await component.decide(assertion, 'revoke');

    expect(api.verifyPetAssertion).toHaveBeenCalledWith('place-1', 'assertion-1', expect.objectContaining({ evidenceUri: 'https://evidence.test/new' }));
    expect(api.resolvePetConflict).toHaveBeenCalledWith('place-1', 'assertion-1', 'OnSite', 'https://evidence.test/new', 'Comprovació funcional');
    expect(api.decidePetAssertion).toHaveBeenCalledWith('place-1', 'assertion-1', 'revoke', 'Comprovació funcional');
  });

  it('gestiona cadenes i presenta en català els errors retornats pel backend', async () => {
    const { fixture, api } = await setup();
    const component = fixture.componentInstance as unknown as {
      selectedChainId: string; evidenceUri: string; reason: string;
      addChainPolicy(): Promise<void>; addException(): Promise<void>;
    };
    component.selectedChainId = 'chain-1';
    component.evidenceUri = 'https://evidence.test/policy';
    component.reason = 'Evidència oficial';
    await component.addChainPolicy();
    expect(api.addChainPolicy).toHaveBeenCalledWith('chain-1', expect.objectContaining({ hasApplicabilityEvidence: true }));

    api.addLocalChainException.mockRejectedValueOnce({ error: { message: 'La decisió ha quedat invalidada.' } });
    await component.addException();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('La decisió ha quedat invalidada.');
  });
});

async function setup(): Promise<{ fixture: ComponentFixture<PetGovernancePanelComponent>; api: ReturnType<typeof adminApi> }> {
  const api = adminApi();
  await TestBed.configureTestingModule({
    imports: [PetGovernancePanelComponent],
    providers: [{ provide: AdminService, useValue: api }]
  }).compileComponents();
  const fixture = TestBed.createComponent(PetGovernancePanelComponent);
  fixture.componentRef.setInput('placeId', 'place-1');
  fixture.detectChanges();
  await (fixture.componentInstance as unknown as { reload(): Promise<void> }).reload();
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, api };
}

function adminApi() {
  return {
    getPetGovernance: vi.fn().mockResolvedValue(overview()),
    listCommercialChains: vi.fn().mockResolvedValue([{ id: 'chain-1', name: 'Cadena', createdAtUtc: '2026-10-13T10:00:00Z' }]),
    listPetGovernanceHistory: vi.fn().mockResolvedValue([{ id: 'global-1', placeId: null, subjectType: 'ChainPolicy', subjectId: 'policy-1', action: 'Created', reason: 'Política oficial', actorId: 'actor-1', decidedAtUtc: '2026-10-13T10:00:00Z' }]),
    addPetAssertion: vi.fn().mockResolvedValue({}), verifyPetAssertion: vi.fn().mockResolvedValue(undefined),
    decidePetAssertion: vi.fn().mockResolvedValue(undefined), resolvePetConflict: vi.fn().mockResolvedValue(undefined),
    createCommercialChain: vi.fn().mockResolvedValue({ id: 'chain-1', name: 'Cadena', createdAtUtc: '2026-10-13T10:00:00Z' }),
    linkPlaceToChain: vi.fn().mockResolvedValue(undefined), addChainPolicy: vi.fn().mockResolvedValue({}),
    addLocalChainException: vi.fn().mockResolvedValue({})
  };
}

function overview(): PetGovernanceOverviewDto {
  return {
    placeId: 'place-1', hasConflicts: true, hasPendingReview: true, hasExpiredData: true, hasUnknownData: false,
    effectivePolicies: [{ species: 'dog', status: 'Allowed', assertionId: 'assertion-1', sourceKey: 'petiloc:manual', isVerifiedManual: true, isInheritedFromChain: false, conditions: [] }],
    assertions: [{
      id: 'assertion-1', placeId: 'place-1', species: 'dog', status: 'Allowed', conditions: [], sourceKey: 'petiloc:manual',
      externalId: null, observedValue: 'Permès', observedAtUtc: '2026-10-13T09:00:00Z', verifiedAtUtc: '2026-10-13T10:00:00Z',
      validUntilUtc: null, verificationMethod: 'OnSite', confidence: 'Verified', responsibleActorId: 'actor-1',
      evidenceUri: 'https://evidence.test/manual', origin: 'Manual', state: 'Current', isExpired: false, isEffective: true, hasConflict: true
    }],
    chainMemberships: [{ chainId: 'chain-1', chainName: 'Cadena', isFranchise: true, linkedAtUtc: '2026-10-13T10:00:00Z' }],
    chainPolicies: [{
      id: 'policy-1', chainId: 'chain-1', chainName: 'Cadena', countryCode: 'ES', territorialUnitId: null,
      status: 'Allowed', species: ['dog'], conditions: [], validFromUtc: '2026-10-01T00:00:00Z', validUntilUtc: null,
      hasApplicabilityEvidence: true, sourceKey: 'chain-official', evidenceUri: 'https://evidence.test/chain', verifiedAtUtc: '2026-10-13T10:00:00Z', isCurrent: true
    }],
    history: [{ id: 'decision-1', placeId: 'place-1', subjectType: 'PetAssertion', subjectId: 'assertion-1', action: 'Verified', reason: 'Confirmació presencial', actorId: 'actor-1', decidedAtUtc: '2026-10-13T10:00:00Z' }]
  };
}
