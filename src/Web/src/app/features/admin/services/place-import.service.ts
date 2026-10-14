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

@Injectable({ providedIn: 'root' })
export class PlaceImportService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/admin/place-imports`;
  context(): Promise<PlaceImportContext> { return firstValueFrom(this.http.get<PlaceImportContext>(`${this.url}/context`)); }
  list(): Promise<PlaceImportRun[]> { return firstValueFrom(this.http.get<PlaceImportRun[]>(`${this.url}/`)); }
  get(id: string): Promise<PlaceImportDetails> { return firstValueFrom(this.http.get<PlaceImportDetails>(`${this.url}/${id}`)); }
  prepare(request: {countryId:string;territorialUnitId:string;categories:string[];accessMode:string;simulationOnly:true}): Promise<PlaceImportRun> { return firstValueFrom(this.http.post<PlaceImportRun>(`${this.url}/`, request)); }
  approve(id:string,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.url}/${id}/approve`,{reason}));}
  simulatePublication(id:string,reason:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.url}/${id}/simulate-publication`,{reason}));}
  cancel(id:string):Promise<void>{return firstValueFrom(this.http.post<void>(`${this.url}/${id}/cancel`,{}));}
}
