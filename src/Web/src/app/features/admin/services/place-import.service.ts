import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api.config';

export interface PlaceImportContext {
  sources: Array<{ key: string; label: string; license: string; attribution: string }>;
  countries: string[];
  categories: Array<{ key: string; label: string }>;
  simulationOnly: boolean;
}
export interface PlaceImportScope { countryId: string; countryIso2: string; territorialUnitId: string; territorialName: string; officialCodeScheme: string; officialCode: string; }
export interface PlaceImportRun {
  id: string; sourceKey: string; accessMode: string; scope: PlaceImportScope; categories: string[]; status: string; stage: string;
  simulationOnly: boolean; discoveredCount: number; validCount: number; warningCount: number; duplicateCount: number;
  boundaryExternalId: string | null; createdAtUtc: string; updatedAtUtc: string; errorCode: string | null; errorMessage: string | null;
}
export interface PlaceImportItem { id: string; externalId: string; name: string; category: string; latitude: number | null; longitude: number | null; matchBand: string; placeId: string | null; status: string; fields: Record<string,string>; }
export interface PlaceImportDetails { run: PlaceImportRun; items: PlaceImportItem[]; issues: Array<{id:string;code:string;severity:string;message:string;externalId:string|null}>; history: Array<{id:string;action:string;reason:string;actorId:string;decidedAtUtc:string}>; }
export interface PlaceCatalogGovernance {
  matches: Array<{id:string;sourceRecordId:string;sourceKey:string;externalId:string;candidatePlaceId:string|null;candidatePlaceName:string|null;outcome:string;score:number;signals:string[];contradictions:string[];isCurrent:boolean;reason:string;decidedAtUtc:string}>;
  conflicts: Array<{id:string;placeId:string;placeName:string;fieldName:string;leftObservationId:string;leftValue:string;leftAuthority:string;leftSource:string;rightObservationId:string;rightValue:string;rightAuthority:string;rightSource:string;status:string;resolutionReason:string;detectedAtUtc:string}>;
  lifecycles: Array<{placeId:string;placeName:string;status:string;history:Array<{id:string;fromStatus:string;toStatus:string;evidence:string[];automatic:boolean;reason:string;createdAtUtc:string}>}>;
  sources: Array<{id:string;sourceKey:string;organisation:string;dataset:string;datasetVersion:string;coverage:string;datasetUrl:string;licenseIdentifier:string;licenseUrl:string;attribution:string;commercialUseAllowed:boolean;permanentStorageAllowed:boolean;reuseStatus:'Allowed'|'NotAllowed'|'Unknown';approvalStatus:string;reviewedAtUtc:string;consultedAtUtc:string|null}>;
  checkpoints: Array<{id:string;sourceKey:string;regionKey:string;generation:string;extractSha256:string;replicationSequence:number|null;status:string;metricsJson:string;createdAtUtc:string;completedAtUtc:string|null}>;
  organisations: Array<{id:string;key:string;name:string;type:string;countryIso2:string|null;officialWebsiteUrl:string;status:string;verifiedAtUtc:string|null}>;
  licenses: Array<{id:string;code:string;name:string;version:string;officialUrl:string;commercialUseStatus:'Allowed'|'NotAllowed'|'Unknown';modificationStatus:'Allowed'|'NotAllowed'|'Unknown';redistributionStatus:'Allowed'|'NotAllowed'|'Unknown';attributionRequired:boolean|null;shareAlikeRequired:boolean|null;additionalRestrictions:string;status:string;verifiedAtUtc:string|null}>;
  licenseEvidence: Array<{id:string;datasetId:string;dataset:string;licenseId:string;licenseCode:string;termsUrl:string;checkedAtUtc:string;evidence:string;verificationStatus:string;notes:string;isCurrent:boolean}>;
  traceability:{totalPlaces:number;placesWithSources:number;placesWithFieldEvidence:number;placesWithIncompleteProvenance:number;pendingDatasets:number;prohibitedDatasets:number;unknownLicenseDatasets:number};
}
export interface PlaceTraceability {
  placeId:string; placeName:string; complete:boolean;
  sources:Array<{id:string;datasetId:string;sourceKey:string;organisation:string;dataset:string;datasetUrl:string;externalId:string;recordUrl:string;sourceVersion:string|null;matchStatus:string;verificationStatus:string;observedAtUtc:string;retrievedAtUtc:string;verifiedAtUtc:string|null;licenseCode:string;licenseUrl:string;commercialUseStatus:'Allowed'|'NotAllowed'|'Unknown';attribution:string;fields:Array<{id:string;fieldName:string;originalValue:string;normalizedValue:string;authority:string;state:string;confidence:number|null;verificationStatus:string;observedAtUtc:string;verifiedAtUtc:string|null}>}>;
}

@Injectable({ providedIn: 'root' })
export class PlaceImportService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/admin/place-imports`;
  private readonly governanceUrl = `${API_BASE_URL}/admin/place-catalog-governance`;
  context(): Promise<PlaceImportContext> { return firstValueFrom(this.http.get<PlaceImportContext>(`${this.url}/context`)); }
  list(): Promise<PlaceImportRun[]> { return firstValueFrom(this.http.get<PlaceImportRun[]>(`${this.url}/`)); }
  get(id: string): Promise<PlaceImportDetails> { return firstValueFrom(this.http.get<PlaceImportDetails>(`${this.url}/${id}`)); }
  prepare(request: {countryId:string;territorialUnitId:string;categories:string[];accessMode:string;simulationOnly:boolean}): Promise<PlaceImportRun> { return firstValueFrom(this.http.post<PlaceImportRun>(`${this.url}/`, request)); }
  approve(id:string,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.url}/${id}/approve`,{reason}));}
  simulatePublication(id:string,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.url}/${id}/simulate-publication`,{reason}));}
  publish(id:string,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.url}/${id}/publish`,{reason}));}
  cancel(id:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.url}/${id}/cancel`,{}));}
  governance():Promise<PlaceCatalogGovernance>{return firstValueFrom(this.http.get<PlaceCatalogGovernance>(`${this.governanceUrl}/`));}
  traceability(placeId:string):Promise<PlaceTraceability>{return firstValueFrom(this.http.get<PlaceTraceability>(`${this.governanceUrl}/places/${placeId}/traceability`));}
  resolveMatch(id:string,outcome:string,placeId:string|null,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/matches/${id}/resolve`,{outcome,placeId,reason}));}
  resolveConflict(id:string,winningObservationId:string,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/conflicts/${id}/resolve`,{winningObservationId,reason}));}
  changeLifecycle(placeId:string,status:string,evidence:string[],reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/places/${placeId}/lifecycle`,{status,evidence,reason}));}
  registerSource(request:{sourceKey:string;organisation:string;dataset:string;datasetVersion:string;coverage:string;url:string;attributionUrl:string;licenseIdentifier:string;licenseUrl:string;attribution:string;reuseConditions:string;commercialUseAllowed:boolean;permanentStorageAllowed:boolean}):Promise<{id:string}>{return firstValueFrom(this.http.post<{id:string}>(`${this.governanceUrl}/sources`,request));}
  reviewSource(id:string,approved:boolean,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/sources/${id}/review`,{approved,reason}));}
}
