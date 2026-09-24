import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';
import {
  ChangeHierarchy, CatalogHierarchy,
  CanonicalPreviewRow, CatalogDetail, CatalogUnit, ChangeItem, ImportDetail, ImportIssue,
  MappingSummary, PageResult, SourcePreviewRow, TerritorialContext, TerritorialImport, WorkbookInspection
} from '../models/territorial-admin.model';

@Injectable({ providedIn: 'root' })
export class TerritorialAdminApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/admin/territorial`;

  context(): Promise<TerritorialContext> {
    return firstValueFrom(this.http.get<TerritorialContext>(`${this.url}/context`));
  }

  inspect(sourceId: string, file: File): Promise<WorkbookInspection> {
    return firstValueFrom(this.http.post<WorkbookInspection>(
      `${this.url}/workbooks/inspect`, this.fileBody(file, { datasetSourceId: sourceId })));
  }

  mappings(sourceId: string, fingerprint?: string): Promise<MappingSummary[]> {
    let params = new HttpParams().set('datasetSourceId', sourceId);
    if (fingerprint) params = params.set('schemaFingerprint', fingerprint);
    return firstValueFrom(this.http.get<MappingSummary[]>(`${this.url}/mappings`, { params }));
  }

  createMapping(sourceId: string, fingerprint: string, definition: unknown): Promise<MappingSummary> {
    return firstValueFrom(this.http.post<MappingSummary>(`${this.url}/mappings`, {
      datasetSourceId: sourceId, schemaFingerprint: fingerprint, definition
    }));
  }

  prepare(sourceId: string, mappingId: string, datasetVersion: string, file: File): Promise<ImportDetail> {
    const body = this.fileBody(file, {
      datasetSourceId: sourceId, mappingTemplateId: mappingId, datasetVersion
    });
    return firstValueFrom(this.http.post<ImportDetail>(`${this.url}/imports`, body));
  }

  imports(filters: {
    countryId?: string; datasetSourceId?: string; status?: string; page?: number; pageSize?: number;
  } = {}): Promise<PageResult<TerritorialImport>> {
    let params = this.pageParams(filters.page, filters.pageSize ?? 20);
    if (filters.countryId) params = params.set('countryId', filters.countryId);
    if (filters.datasetSourceId) params = params.set('datasetSourceId', filters.datasetSourceId);
    if (filters.status) params = params.set('status', filters.status);
    return firstValueFrom(this.http.get<PageResult<TerritorialImport>>(`${this.url}/imports`, { params }));
  }

  detail(id: string): Promise<ImportDetail> {
    return firstValueFrom(this.http.get<ImportDetail>(`${this.url}/imports/${id}`));
  }

  issues(id: string, filters: {
    severity?: string; ruleCode?: string; sheet?: string; field?: string; page?: number; pageSize?: number;
  } = {}): Promise<PageResult<ImportIssue>> {
    let params = this.pageParams(filters.page, filters.pageSize);
    for (const [key, value] of Object.entries(filters)) {
      if (value && key !== 'page' && key !== 'pageSize') params = params.set(key, value);
    }
    return firstValueFrom(this.http.get<PageResult<ImportIssue>>(
      `${this.url}/imports/${id}/issues`, { params }));
  }

  changes(id: string, filters: {
    kind?: string; search?: string; page?: number; pageSize?: number;
  } = {}): Promise<PageResult<ChangeItem>> {
    let params = this.pageParams(filters.page, filters.pageSize);
    if (filters.kind) params = params.set('kind', filters.kind);
    if (filters.search) params = params.set('search', filters.search);
    return firstValueFrom(this.http.get<PageResult<ChangeItem>>(
      `${this.url}/imports/${id}/changes`, { params }));
  }

  changeHierarchy(id: string, changeId: string, filters: { search?: string; page?: number; pageSize?: number } = {}): Promise<ChangeHierarchy> {
    let params = this.pageParams(filters.page, filters.pageSize ?? 25);
    if (filters.search) params = params.set('search', filters.search);
    return firstValueFrom(this.http.get<ChangeHierarchy>(
      `${this.url}/imports/${id}/changes/${changeId}/hierarchy`, { params }));
  }

  sourcePreview(id: string, filters: { sheet?: string; search?: string; page?: number } = {}): Promise<PageResult<SourcePreviewRow>> {
    return firstValueFrom(this.http.get<PageResult<SourcePreviewRow>>(
      `${this.url}/imports/${id}/preview/source`, { params: this.previewParams(filters) }));
  }

  canonicalPreview(id: string, filters: { sheet?: string; search?: string; page?: number } = {}): Promise<PageResult<CanonicalPreviewRow>> {
    return firstValueFrom(this.http.get<PageResult<CanonicalPreviewRow>>(
      `${this.url}/imports/${id}/preview/canonical`, { params: this.previewParams(filters) }));
  }

  catalog(filters: {
    countryId?: string; territorialUnitTypeId?: string; status?: string; locale?: string;
    parentId?: string; search?: string; selectableLocality?: boolean; page?: number;
  } = {}): Promise<PageResult<CatalogUnit>> {
    let params = this.pageParams(filters.page, 50);
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== '' && key !== 'page') params = params.set(key, String(value));
    }
    return firstValueFrom(this.http.get<PageResult<CatalogUnit>>(`${this.url}/catalog`, { params }));
  }

  catalogDetail(id: string): Promise<CatalogDetail> {
    return firstValueFrom(this.http.get<CatalogDetail>(`${this.url}/catalog/${id}`));
  }

  catalogHierarchy(id: string, filters: { search?: string; page?: number; pageSize?: number } = {}): Promise<CatalogHierarchy> {
    let params = this.pageParams(filters.page, filters.pageSize ?? 50);
    if (filters.search) params = params.set('search', filters.search);
    return firstValueFrom(this.http.get<CatalogHierarchy>(`${this.url}/catalog/${id}/hierarchy`, { params }));
  }

  catalogDescendants(id: string, territorialUnitTypeId: string,
    filters: { search?: string; page?: number; pageSize?: number } = {}): Promise<PageResult<CatalogUnit>> {
    let params = this.pageParams(filters.page, filters.pageSize ?? 50)
      .set('territorialUnitTypeId', territorialUnitTypeId);
    if (filters.search) params = params.set('search', filters.search);
    return firstValueFrom(this.http.get<PageResult<CatalogUnit>>(`${this.url}/catalog/${id}/descendants`, { params }));
  }

  maintain(id: string, request: { action: string; reason: string; isSelectableLocality?: boolean; latitude?: number; longitude?: number }): Promise<CatalogDetail> {
    return firstValueFrom(this.http.post<CatalogDetail>(`${this.url}/catalog/${id}/maintenance`, request));
  }

  publish(id: string): Promise<ImportDetail> { return this.command(id, 'publish'); }
  cancel(id: string): Promise<ImportDetail> { return this.command(id, 'cancel'); }
  revert(id: string): Promise<ImportDetail> { return this.command(id, 'revert'); }

  private command(id: string, action: string): Promise<ImportDetail> {
    return firstValueFrom(this.http.post<ImportDetail>(`${this.url}/imports/${id}/${action}`, {}));
  }

  private pageParams(page = 1, pageSize = 50): HttpParams {
    return new HttpParams().set('page', page).set('pageSize', pageSize);
  }

  private previewParams(filters: { sheet?: string; search?: string; page?: number }): HttpParams {
    let params = this.pageParams(filters.page, 50);
    if (filters.sheet) params = params.set('sheet', filters.sheet);
    if (filters.search) params = params.set('search', filters.search);
    return params;
  }

  private fileBody(file: File, values: Record<string, string>): FormData {
    const body = new FormData();
    Object.entries(values).forEach(([key, value]) => body.append(key, value));
    body.append('file', file, file.name);
    return body;
  }
}
