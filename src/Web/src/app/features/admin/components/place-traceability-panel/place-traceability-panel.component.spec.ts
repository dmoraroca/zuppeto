import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { PlaceImportService } from '../../services/place-import.service';
import { PlaceTraceabilityPanelComponent } from './place-traceability-panel.component';

describe('PlaceTraceabilityPanelComponent', () => {
  it('mostra fonts, llicència, identificador extern i evidències pendents', async () => {
    const api = { traceability: vi.fn().mockResolvedValue({
      placeId: 'place-1', placeName: 'Local', complete: false,
      sources: [{ id:'record-1', datasetId:'dataset-1', sourceKey:'overture-maps-places', organisation:'Overture Maps Foundation', dataset:'Overture Maps Places', datasetUrl:'https://overturemaps.org/', externalId:'abc', recordUrl:'https://example.test/abc', sourceVersion:'2026-10', matchStatus:'ConfirmedMatch', verificationStatus:'Verified', observedAtUtc:'2026-10-18T10:00:00Z', retrievedAtUtc:'2026-10-18T10:00:00Z', verifiedAtUtc:'2026-10-18T10:05:00Z', licenseCode:'CDLA-Permissive-2.0', licenseUrl:'https://cdla.dev/permissive-2-0/', commercialUseStatus:'Allowed', attribution:'Overture Maps data', fields:[{ id:'field-1', fieldName:'name', originalValue:'Local', normalizedValue:'Local', authority:'AUTO', state:'Current', confidence:null, verificationStatus:'Pending', observedAtUtc:'2026-10-18T10:00:00Z', verifiedAtUtc:null }] }]
    }) };
    await TestBed.configureTestingModule({ imports:[PlaceTraceabilityPanelComponent], providers:[{provide:PlaceImportService,useValue:api}] }).compileComponents();
    const fixture: ComponentFixture<PlaceTraceabilityPanelComponent> = TestBed.createComponent(PlaceTraceabilityPanelComponent);
    fixture.componentRef.setInput('placeId','place-1'); fixture.detectChanges(); await fixture.whenStable(); fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(api.traceability).toHaveBeenCalledWith('place-1');
    expect(text).toContain('Overture Maps Foundation'); expect(text).toContain('CDLA-Permissive-2.0');
    expect(text).toContain('abc'); expect(text).toContain('Traçabilitat incompleta'); expect(text).toContain('Pending');
  });
});
