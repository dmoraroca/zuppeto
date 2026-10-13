import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AdminService,
  CommercialChainDto,
  PetAssertionDto,
  PetGovernanceDecisionDto,
  PetGovernanceOverviewDto
} from '../../services/admin.service';

@Component({
  selector: 'app-pet-governance-panel',
  imports: [FormsModule],
  templateUrl: './pet-governance-panel.component.html',
  styleUrl: './pet-governance-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PetGovernancePanelComponent {
  readonly placeId = input.required<string>();
  private readonly admin = inject(AdminService);
  protected readonly overview = signal<PetGovernanceOverviewDto | null>(null);
  protected readonly chains = signal<CommercialChainDto[]>([]);
  protected readonly globalHistory = signal<PetGovernanceDecisionDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal('');
  protected species = 'dog';
  protected status = 'Unknown';
  protected sourceKey = 'petiloc:manual';
  protected observedValue = '';
  protected validUntil = '';
  protected conditions = '';
  protected evidenceUri = '';
  protected reason = '';
  protected verificationMethod = 'OnSite';
  protected selectedChainId = '';
  protected isFranchise = false;
  protected newChainName = '';
  protected chainCountryCode = 'ES';
  protected chainStatus = 'Allowed';
  protected chainSpecies = 'dog';
  protected chainValidUntil = '';
  protected exceptionStatus = 'NotAllowed';

  constructor() {
    effect(() => { const id = this.placeId(); if (id) void this.reload(); });
  }

  protected async reload(): Promise<void> {
    await this.run(async () => {
      const [overview, chains, history] = await Promise.all([
        this.admin.getPetGovernance(this.placeId()), this.admin.listCommercialChains(),
        this.admin.listPetGovernanceHistory()
      ]);
      this.overview.set(overview); this.chains.set(chains); this.globalHistory.set(history.filter(x => x.placeId == null));
      if (!this.selectedChainId && chains.length) this.selectedChainId = chains[0].id;
    }, 'No s’ha pogut carregar la governança pet-friendly.');
  }

  protected async addAssertion(): Promise<void> {
    if (!this.observedValue.trim()) { this.error.set('El valor observat és obligatori.'); return; }
    await this.run(async () => {
      await this.admin.addPetAssertion(this.placeId(), {
        species: this.species, status: this.status, conditions: this.parseConditions(), sourceKey: this.sourceKey,
        externalId: null, observedValue: this.observedValue, observedAtUtc: new Date().toISOString(),
        validUntilUtc: this.dateOrNull(this.validUntil), origin: this.sourceKey === 'petiloc:manual' ? 'Manual' : 'Automatic',
        confidence: 'Unverified'
      });
      this.observedValue = ''; this.success.set('Afirmació registrada.'); await this.reload();
    }, 'No s’ha pogut registrar l’afirmació.');
  }

  protected async verify(assertion: PetAssertionDto): Promise<void> {
    if (!this.evidenceUri.trim() || !this.reason.trim()) { this.error.set('Cal indicar evidència i motiu.'); return; }
    await this.run(async () => {
      await this.admin.verifyPetAssertion(this.placeId(), assertion.id, {
        verificationMethod: this.verificationMethod, evidenceUri: this.evidenceUri, reason: this.reason
      });
      this.success.set('Afirmació verificada.'); await this.reload();
    }, 'No s’ha pogut verificar l’afirmació.');
  }

  protected async decide(assertion: PetAssertionDto, action: 'question' | 'revoke'): Promise<void> {
    if (!this.reason.trim()) { this.error.set('El motiu és obligatori.'); return; }
    await this.run(async () => {
      await this.admin.decidePetAssertion(this.placeId(), assertion.id, action, this.reason);
      this.success.set(action === 'revoke' ? 'Verificació revocada.' : 'Afirmació marcada com a qüestionada.');
      await this.reload();
    }, 'No s’ha pogut registrar la decisió.');
  }

  protected async resolve(assertion: PetAssertionDto): Promise<void> {
    if (!this.evidenceUri.trim() || !this.reason.trim()) { this.error.set('Cal indicar evidència i motiu.'); return; }
    await this.run(async () => {
      await this.admin.resolvePetConflict(this.placeId(), assertion.id, this.verificationMethod, this.evidenceUri, this.reason);
      this.success.set('Conflicte resolt i fonts contradictòries conservades com a qüestionades.'); await this.reload();
    }, 'No s’ha pogut resoldre el conflicte.');
  }

  protected async createChain(): Promise<void> {
    if (!this.newChainName.trim()) { this.error.set('El nom de la cadena és obligatori.'); return; }
    await this.run(async () => {
      const chain = await this.admin.createCommercialChain(this.newChainName.trim());
      this.newChainName = ''; this.selectedChainId = chain.id; this.success.set('Cadena creada.'); await this.reload();
    }, 'No s’ha pogut crear la cadena.');
  }

  protected async linkChain(): Promise<void> {
    if (!this.selectedChainId || !this.reason.trim()) { this.error.set('Selecciona una cadena i indica el motiu.'); return; }
    await this.run(async () => {
      await this.admin.linkPlaceToChain(this.placeId(), this.selectedChainId, this.isFranchise, this.reason);
      this.success.set('Establiment vinculat a la cadena.'); await this.reload();
    }, 'No s’ha pogut vincular la cadena.');
  }

  protected async addChainPolicy(): Promise<void> {
    if (!this.selectedChainId || !this.evidenceUri.trim() || !this.reason.trim()) { this.error.set('Cal cadena, evidència i motiu.'); return; }
    await this.run(async () => {
      await this.admin.addChainPolicy(this.selectedChainId, {
        countryCode: this.chainCountryCode, territorialUnitId: null, status: this.chainStatus,
        species: this.split(this.chainSpecies), conditions: this.parseConditions(), validFromUtc: new Date().toISOString(),
        validUntilUtc: this.dateOrNull(this.chainValidUntil), hasApplicabilityEvidence: true,
        sourceKey: 'petiloc:chain-official', evidenceUri: this.evidenceUri, reason: this.reason
      });
      this.success.set('Política general de cadena registrada.'); await this.reload();
    }, 'No s’ha pogut registrar la política de cadena.');
  }

  protected async addException(): Promise<void> {
    if (!this.selectedChainId || !this.evidenceUri.trim() || !this.reason.trim()) { this.error.set('Cal cadena, evidència i motiu.'); return; }
    await this.run(async () => {
      await this.admin.addLocalChainException(this.placeId(), {
        chainId: this.selectedChainId, species: this.species, status: this.exceptionStatus,
        conditions: this.parseConditions(), evidenceUri: this.evidenceUri, reason: this.reason,
        validUntilUtc: this.dateOrNull(this.validUntil)
      });
      this.success.set('Excepció local verificada i registrada.'); await this.reload();
    }, 'No s’ha pogut registrar l’excepció local.');
  }

  protected statusLabel(value: string): string {
    return ({ Allowed: 'Permès', NotAllowed: 'No permès', TerraceOnly: 'Només terrassa', Unknown: 'Desconegut', PendingReview: 'Pendent de revisió' } as Record<string, string>)[value] ?? value;
  }

  private async run(action: () => Promise<void>, fallback: string): Promise<void> {
    this.loading.set(true); this.error.set(''); this.success.set('');
    try { await action(); } catch (error: unknown) {
      const response = error as { error?: { message?: string } };
      this.error.set(response.error?.message || fallback);
    } finally { this.loading.set(false); }
  }
  private split(value: string): string[] { return value.split(',').map(x => x.trim()).filter(Boolean); }
  private parseConditions(): Array<{ code: string; value: string | null }> {
    return this.split(this.conditions).map(item => { const [code, ...value] = item.split(':'); return { code, value: value.join(':').trim() || null }; });
  }
  private dateOrNull(value: string): string | null { return value ? new Date(`${value}T23:59:59Z`).toISOString() : null; }
}
