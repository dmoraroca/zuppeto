import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { TerritorialSourceDetail } from '../../models/territorial-admin.model';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import { TerritorialSourceReviewComponent } from './territorial-source-review.component';

describe('TerritorialSourceReviewComponent', () => {
  it('requires an audited reason and explicit confirmation before approving', async () => {
    const pending = source();
    const api = { source: vi.fn().mockResolvedValue(pending), decideSource: vi.fn().mockResolvedValue({ ...pending, approvalStatus: 'Approved' }) };
    const fixture = await create(api);
    let changed = 0;
    fixture.componentInstance.sourceChanged.subscribe(() => changed++);
    fixture.componentRef.setInput('sourceId', pending.id);
    fixture.detectChanges(); await fixture.whenStable(); fixture.detectChanges();

    button(fixture, 'Aprovar font').click(); fixture.detectChanges();
    expect(button(fixture, 'Confirmar decisió').disabled).toBe(true);
    const component = fixture.componentInstance as unknown as { reason: string; confirmed: boolean; confirmDecision(): Promise<void> };
    component.reason = 'Evidències oficials verificades'; component.confirmed = true;
    fixture.detectChanges(); await component.confirmDecision(); fixture.detectChanges();

    expect(api.decideSource).toHaveBeenCalledWith(pending.id, 'approve', 'Pending', 'Evidències oficials verificades');
    expect(changed).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Aprovada');
  });

  it('cancelling does not persist a decision and incomplete metadata disables approval', async () => {
    const pending = { ...source(), hasCompleteApprovalMetadata: false, missingApprovalMetadata: ['llicència'] };
    const api = { source: vi.fn().mockResolvedValue(pending), decideSource: vi.fn() };
    const fixture = await create(api);
    fixture.componentRef.setInput('sourceId', pending.id);
    fixture.detectChanges(); await fixture.whenStable(); fixture.detectChanges();

    expect(button(fixture, 'Aprovar font').disabled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Falten: llicència');
    button(fixture, 'Cancel·lar').click();
    expect(api.decideSource).not.toHaveBeenCalled();
  });

  it('keeps the displayed pending state when the network decision fails', async () => {
    const pending = source();
    const api = { source: vi.fn().mockResolvedValue(pending), decideSource: vi.fn().mockRejectedValue({}) };
    const fixture = await create(api);
    let changed = 0;
    fixture.componentInstance.sourceChanged.subscribe(() => changed++);
    fixture.componentRef.setInput('sourceId', pending.id);
    fixture.detectChanges(); await fixture.whenStable(); fixture.detectChanges();
    button(fixture, 'Rebutjar').click(); fixture.detectChanges();
    const component = fixture.componentInstance as unknown as { reason: string; confirmed: boolean; confirmDecision(): Promise<void> };
    component.reason = 'Evidència insuficient'; component.confirmed = true;
    await component.confirmDecision(); fixture.detectChanges();

    expect(changed).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Pendent');
    expect(fixture.nativeElement.textContent).toContain('no s’ha modificat');
  });
});

async function create(api: object) {
  await TestBed.configureTestingModule({ imports: [TerritorialSourceReviewComponent], providers: [{ provide: TerritorialAdminApiService, useValue: api }] }).compileComponents();
  return TestBed.createComponent(TerritorialSourceReviewComponent);
}

function button(fixture: ComponentFixture<TerritorialSourceReviewComponent>, label: string): HTMLButtonElement {
  return [...fixture.nativeElement.querySelectorAll('button')].find((item: HTMLButtonElement) => item.textContent?.trim() === label)!;
}

function source(): TerritorialSourceDetail {
  return {
    id: 'source-1', countryId: 'country-1', country: 'Espanya', sourceKey: 'CNIG_NGMEP_2026',
    organisation: 'IGN/CNIG', dataset: 'Nomenclátor Geográfico', datasetType: 'AdministrativeTerritory',
    locale: 'es-ES', approvalStatus: 'Pending', isActive: true, publicationMode: 'FullSnapshot',
    datasetVersion: '2026', datasetDate: '2026-01-01', url: 'https://example.test/source',
    downloadUrl: 'https://example.test/download', license: 'CC BY 4.0', licenseUrl: 'https://example.test/license',
    attribution: 'CNIG', commercialUseAllowed: true, transformationAllowed: true,
    restrictions: 'Atribució obligatòria', thirdPartyData: null, hasCompleteApprovalMetadata: true,
    missingApprovalMetadata: [], evidence: [{ label: 'Fitxa oficial', url: 'https://example.test/source' }], approvalHistory: []
  };
}
