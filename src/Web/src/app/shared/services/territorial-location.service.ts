import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';

export interface TerritorialCountryOption { id: string; code: string; name: string; }
export interface TerritorialLocalityOption { id: string; countryId: string; name: string; context: string; locale: string | null; }
interface Page<T> { items: T[]; }

@Injectable({ providedIn: 'root' })
export class TerritorialLocationService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/territorial`;

  countries(): Promise<TerritorialCountryOption[]> {
    return firstValueFrom(this.http.get<TerritorialCountryOption[]>(`${this.url}/countries`));
  }

  async localities(countryId: string, search: string): Promise<TerritorialLocalityOption[]> {
    const params = new HttpParams().set('countryId', countryId).set('search', search).set('page', 1).set('pageSize', 20);
    return (await firstValueFrom(this.http.get<Page<TerritorialLocalityOption>>(`${this.url}/localities`, { params }))).items;
  }

  validate(countryId: string, territorialUnitId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.url}/location/validate`, { countryId, territorialUnitId }));
  }
}
