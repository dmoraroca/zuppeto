export interface TerritorialCountry {
  id: string;
  code: string;
  name: string;
  iso2: string | null;
  iso3: string | null;
  isActive: boolean;
}

export interface TerritorialSource {
  id: string;
  countryId: string;
  organisation: string;
  dataset: string;
  approvalStatus: string;
  isActive: boolean;
  publicationMode: string;
  datasetVersion: string | null;
  datasetDate: string | null;
  license: string | null;
  attribution: string | null;
}

export interface TerritorialUnitType {
  id: string;
  countryId: string;
  code: string;
  name: string;
  displayOrder: number;
  isSelectableLocality: boolean;
}

export interface TerritorialContext {
  countries: TerritorialCountry[];
  sources: TerritorialSource[];
  unitTypes: TerritorialUnitType[];
}

export interface WorkbookColumn { column: string; header: string; }
export interface WorkbookSheet {
  name: string;
  detectedHeaderRow: number;
  rowCount: number;
  columns: WorkbookColumn[];
}

export interface MappingSummary {
  id: string;
  datasetSourceId: string;
  version: number;
  schemaFingerprint: string;
  definitionChecksum: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface WorkbookInspection {
  artifactName: string;
  fileSize: number;
  fileChecksum: string;
  format: string;
  schemaFingerprint: string;
  sheets: WorkbookSheet[];
  compatibleMappings: MappingSummary[];
}

export interface ImportCounters {
  rows: number;
  errors: number;
  warnings: number;
  create: number;
  update: number;
  deactivate: number;
  noChange: number;
}

export interface TerritorialImport {
  id: string;
  countryId: string;
  countryName: string;
  datasetSourceId: string;
  organisation: string;
  dataset: string;
  datasetVersion: string;
  mappingTemplateId: string;
  mappingVersion: number;
  status: string;
  hasBlockingErrors: boolean;
  catalogVersion: number | null;
  artifactName: string;
  fileSize: number;
  fileChecksum: string;
  actor: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  publishedAtUtc: string | null;
  counters: ImportCounters;
}

export interface ImportDetail {
  import: TerritorialImport;
  publicationMode: string;
  failureReason: string | null;
  schemaFingerprint: string;
  canCancel: boolean;
  canPublish: boolean;
  canRevert: boolean;
  currentCatalogVersion: number;
  changeSetId: string | null;
  changeSetStatus: string | null;
}

export interface ImportIssue {
  id: string;
  severity: string;
  ruleCode: string;
  message: string;
  sheet: string | null;
  rowNumber: number | null;
  field: string | null;
  problemValue: string | null;
  canonicalUnitKey: string | null;
}

export interface ChangeItem {
  id: string;
  kind: string;
  territorialUnitId: string | null;
  canonicalUnitKey: string;
  before: unknown;
  after: unknown;
  changedFields: string[];
}

export interface SourcePreviewRow {
  id: string; sheet: string; rowNumber: number; values: Record<string, string | null>; readingStatus: string;
}
export interface CanonicalPreviewRow {
  id: string; sheet: string; rowNumber: number; canonicalUnitKey: string; parentCanonicalUnitKey: string | null;
  name: string; locale: string | null; territorialUnitTypeCode: string;
  codes: Array<{ scheme: string; value: string; isPrimary: boolean }>;
  latitude: number | null; longitude: number | null; status: string; issueCount: number;
}
export interface CatalogUnit {
  id: string; countryId: string; country: string; territorialUnitTypeId: string; typeCode: string; type: string;
  parentId: string | null; parent: string | null; primaryCode: string | null; primaryName: string; locale: string | null;
  latitude: number | null; longitude: number | null; isActive: boolean; isSelectableLocality: boolean; hasManualOverride: boolean;
  hasManualActiveOverride: boolean; hasManualSelectableOverride: boolean; hasManualCoordinateOverride: boolean;
}
export interface CatalogAudit {
  id: string; action: string; field: string; beforeValue: string | null; afterValue: string | null;
  reason: string; actor: string; origin: string; createdAtUtc: string;
}
export interface CatalogDetail {
  unit: CatalogUnit;
  ancestors: Array<{ id: string; name: string; type: string }>;
  names: Array<{ id: string; name: string; locale: string | null; kind: string; isPrimary: boolean; source: string | null }>;
  codes: Array<{ id: string; scheme: string; value: string; validFrom: string | null; validTo: string | null; isPrimary: boolean; source: string | null }>;
  coordinateSource: string | null; datasetSources: string[]; importIds: string[]; audit: CatalogAudit[];
}

export interface PageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface MappingProjectionDraft {
  sheet: string;
  headerRow: number;
  unitTypeCode: string;
  nameColumn: string;
  canonicalColumns: string[];
  parentColumns: string[];
  codeScheme: string;
  codeColumns: string[];
  locale: string;
  longitudeColumn: string;
  latitudeColumn: string;
  zeroZeroIsSentinel: boolean;
}
