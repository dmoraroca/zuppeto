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
  sources: Array<{id:string;sourceKey:string;organisation:string;dataset:string;datasetVersion:string;coverage:string;licenseIdentifier:string;attribution:string;commercialUseAllowed:boolean;permanentStorageAllowed:boolean;approvalStatus:string;reviewedAtUtc:string}>;
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
  resolveMatch(id:string,outcome:string,placeId:string|null,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/matches/${id}/resolve`,{outcome,placeId,reason}));}
  resolveConflict(id:string,winningObservationId:string,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/conflicts/${id}/resolve`,{winningObservationId,reason}));}
  changeLifecycle(placeId:string,status:string,evidence:string[],reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/places/${placeId}/lifecycle`,{status,evidence,reason}));}
  registerSource(request:{sourceKey:string;organisation:string;dataset:string;datasetVersion:string;coverage:string;url:string;attributionUrl:string;licenseIdentifier:string;licenseUrl:string;attribution:string;reuseConditions:string;commercialUseAllowed:boolean;permanentStorageAllowed:boolean}):Promise<{id:string}>{return firstValueFrom(this.http.post<{id:string}>(`${this.governanceUrl}/sources`,request));}
  reviewSource(id:string,approved:boolean,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.governanceUrl}/sources/${id}/review`,{approved,reason}));}
}
