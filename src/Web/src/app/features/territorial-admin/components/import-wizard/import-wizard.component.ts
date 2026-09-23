import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JsonPipe, KeyValuePipe } from '@angular/common';

import {
  CanonicalPreviewRow, ChangeItem, ImportDetail, ImportIssue, MappingProjectionDraft, PageResult,
  SourcePreviewRow, TerritorialContext, WorkbookInspection
} from '../../models/territorial-admin.model';
import { buildMappingDefinition, mappingDraftError, territorialFileError } from '../../policies/territorial-mapping.policy';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';

@Component({
  selector: 'app-territorial-import-wizard',
  imports: [FormsModule, JsonPipe, KeyValuePipe],
  templateUrl: './import-wizard.component.html',
  styleUrl: './import-wizard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialImportWizardComponent implements OnChanges {
  @Input({ required: true }) context!: TerritorialContext;
  @Input() requestedImportId: string | null = null;
  @Output() readonly importChanged = new EventEmitter<string>();

  protected step = 1;
  protected sourceId = '';
  protected datasetVersion = '';
  protected file: File | null = null;
  protected inspection: WorkbookInspection | null = null;
  protected mappingId = '';
  protected detail: ImportDetail | null = null;
  protected issues: PageResult<ImportIssue> = this.emptyPage();
  protected changes: PageResult<ChangeItem> = this.emptyPage();
  protected sourcePreview: PageResult<SourcePreviewRow> = this.emptyPage();
  protected canonicalPreview: PageResult<CanonicalPreviewRow> = this.emptyPage();
  protected busy = false;
  protected error = '';
  protected issueSeverity = '';
  protected issueRuleCode = '';
  protected issueSheet = '';
  protected issueField = '';
  protected changeKind = '';
  protected changeSearch = '';
  protected previewSheet = '';
  protected previewSearch = '';
  protected confirmAction: 'publish' | 'cancel' | 'revert' | null = null;
  protected drafts: MappingProjectionDraft[] = [];

  private readonly api = inject(TerritorialAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  ngOnChanges(changes: SimpleChanges): void {
    const id = changes['requestedImportId']?.currentValue as string | null;
    if (id) void this.open(id);
  }

  protected get source() {
    return this.context.sources.find((item) => item.id === this.sourceId) ?? null;
  }

  protected get countryTypes() {
    return this.context.unitTypes.filter((item) => item.countryId === this.source?.countryId);
  }

  protected selectSource(): void {
    this.inspection = null;
    this.mappingId = '';
    this.drafts = [];
    if (this.source?.datasetVersion) this.datasetVersion = this.source.datasetVersion;
  }

  protected chooseFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.item(0) ?? null;
    this.error = territorialFileError(this.file) ?? '';
  }

  protected async inspect(): Promise<void> {
    const invalid = territorialFileError(this.file);
    if (!this.sourceId || invalid) {
      this.error = !this.sourceId ? 'Selecciona una font territorial.' : invalid!;
      return;
    }
    await this.run(async () => {
      this.inspection = await this.api.inspect(this.sourceId, this.file!);
      this.mappingId = this.inspection.compatibleMappings[0]?.id ?? '';
      this.drafts = [this.newDraft()];
      this.step = 3;
    });
  }

  protected addProjection(): void {
    this.drafts = [...this.drafts, this.newDraft()];
  }

  protected removeProjection(index: number): void {
    this.drafts = this.drafts.filter((_, current) => current !== index);
  }

  protected columns(sheetName: string) {
    return this.inspection?.sheets.find((sheet) => sheet.name === sheetName)?.columns ?? [];
  }

  protected split(value: string): string[] {
    return value.split(',').map((item) => item.trim()).filter(Boolean);
  }

  protected join(value: string[]): string {
    return value.join(', ');
  }

  protected updateColumns(draft: MappingProjectionDraft, field: 'canonicalColumns' | 'parentColumns' | 'codeColumns', value: string): void {
    draft[field] = this.split(value);
  }

  protected async saveMapping(): Promise<void> {
    if (!this.inspection) return;
    const invalid = this.drafts.map(mappingDraftError).find(Boolean);
    if (invalid) { this.error = invalid; return; }
    await this.run(async () => {
      const mapping = await this.api.createMapping(
        this.sourceId, this.inspection!.schemaFingerprint, buildMappingDefinition(this.drafts));
      this.mappingId = mapping.id;
    });
  }

  protected async prepare(): Promise<void> {
    if (!this.file || !this.mappingId || !this.datasetVersion.trim()) {
      this.error = 'Fitxer, mapping compatible i versió del dataset són obligatoris.';
      return;
    }
    await this.run(async () => {
      this.detail = await this.api.prepare(this.sourceId, this.mappingId, this.datasetVersion.trim(), this.file!);
      await this.loadEvidence();
      this.step = this.detail!.import.hasBlockingErrors ? 4 : 5;
      this.importChanged.emit(this.detail!.import.id);
    });
  }

  protected async open(id: string): Promise<void> {
    await this.run(async () => {
      this.detail = await this.api.detail(id);
      this.sourceId = this.detail.import.datasetSourceId;
      await this.loadEvidence();
      this.step = this.detail.import.status === 'ReadyForReview' ? 5 : 6;
    });
  }

  protected async issuePage(page: number): Promise<void> {
    if (!this.detail) return;
    await this.run(async () => { this.issues = await this.api.issues(this.detail!.import.id, { page, severity: this.issueSeverity || undefined, ruleCode: this.issueRuleCode || undefined, sheet: this.issueSheet || undefined, field: this.issueField || undefined }); });
  }

  protected async changePage(page: number): Promise<void> {
    if (!this.detail) return;
    await this.run(async () => { this.changes = await this.api.changes(this.detail!.import.id, { page, kind: this.changeKind || undefined, search: this.changeSearch || undefined }); });
  }

  protected async filterIssues(): Promise<void> { await this.issuePage(1); }

  protected async filterChanges(): Promise<void> { await this.changePage(1); }

  protected async previewPage(kind: 'source' | 'canonical', page: number): Promise<void> {
    if (!this.detail) return;
    await this.run(async () => {
      const filters = { page, sheet: this.previewSheet || undefined, search: this.previewSearch || undefined };
      if (kind === 'source') this.sourcePreview = await this.api.sourcePreview(this.detail!.import.id, filters);
      else this.canonicalPreview = await this.api.canonicalPreview(this.detail!.import.id, filters);
    });
  }

  protected async filterPreview(): Promise<void> {
    await this.run(async () => {
      if (!this.detail) return;
      [this.sourcePreview, this.canonicalPreview] = await Promise.all([
        this.api.sourcePreview(this.detail.import.id, { sheet: this.previewSheet || undefined, search: this.previewSearch || undefined }),
        this.api.canonicalPreview(this.detail.import.id, { sheet: this.previewSheet || undefined, search: this.previewSearch || undefined })
      ]);
    });
  }

  protected request(action: 'publish' | 'cancel' | 'revert'): void {
    this.confirmAction = action;
  }

  protected async confirm(): Promise<void> {
    if (!this.detail || !this.confirmAction) return;
    const action = this.confirmAction;
    this.confirmAction = null;
    await this.run(async () => {
      this.detail = await this.api[action](this.detail!.import.id);
      await this.loadEvidence();
      this.step = 6;
      this.importChanged.emit(this.detail!.import.id);
    });
  }

  protected reset(): void {
    this.step = 1; this.file = null; this.inspection = null; this.mappingId = '';
    this.detail = null; this.issues = this.emptyPage(); this.changes = this.emptyPage();
    this.sourcePreview = this.emptyPage(); this.canonicalPreview = this.emptyPage();
    this.error = ''; this.confirmAction = null; this.drafts = [];
  }

  private async loadEvidence(): Promise<void> {
    if (!this.detail) return;
    [this.issues, this.changes, this.sourcePreview, this.canonicalPreview] = await Promise.all([
      this.api.issues(this.detail.import.id), this.api.changes(this.detail.import.id),
      this.api.sourcePreview(this.detail.import.id), this.api.canonicalPreview(this.detail.import.id)
    ]);
  }

  private newDraft(): MappingProjectionDraft {
    const sheet = this.inspection?.sheets[0];
    return {
      sheet: sheet?.name ?? '', headerRow: sheet?.detectedHeaderRow ?? 1,
      unitTypeCode: this.countryTypes[0]?.code ?? '', nameColumn: '',
      canonicalColumns: [], parentColumns: [], codeScheme: '', codeColumns: [],
      locale: '', longitudeColumn: '', latitudeColumn: '', zeroZeroIsSentinel: false
    };
  }

  private async run(work: () => Promise<void>): Promise<void> {
    this.busy = true; this.error = ''; this.cdr.markForCheck();
    try { await work(); }
    catch (reason) { this.error = this.message(reason); }
    finally { this.busy = false; this.cdr.markForCheck(); }
  }

  private message(reason: unknown): string {
    const candidate = reason as { error?: { message?: string }; message?: string };
    return candidate?.error?.message ?? candidate?.message ?? 'No s’ha pogut completar l’operació.';
  }

  private emptyPage<T>(): PageResult<T> {
    return { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 };
  }
}
