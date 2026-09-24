import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { KeyValuePipe } from '@angular/common';

import {
  CanonicalPreviewRow, ChangeHierarchy, ChangeItem, ImportDetail, ImportIssue, MappingProjectionDraft, PageResult,
  SourcePreviewRow, TerritorialContext, WorkbookInspection
} from '../../models/territorial-admin.model';
import { buildMappingDefinition, mappingDraftError, territorialFileError } from '../../policies/territorial-mapping.policy';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import {
  territorialActionLabel, territorialApprovalLabel, territorialFieldLabel, territorialNameKindLabel,
  territorialPublicationModeLabel, territorialSeverityLabel, territorialStageLabel, territorialStatusLabel,
  territorialTypeLabel
} from '../../policies/territorial-presentation.policy';

@Component({
  selector: 'app-territorial-import-wizard',
  imports: [FormsModule, KeyValuePipe],
  templateUrl: './import-wizard.component.html',
  styleUrl: './import-wizard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialImportWizardComponent implements OnChanges, OnDestroy {
  @ViewChild('changeDialogClose') private changeDialogClose?: ElementRef<HTMLButtonElement>;
  protected readonly statusLabel = territorialStatusLabel;
  protected readonly stageLabel = territorialStageLabel;
  protected readonly actionLabel = territorialActionLabel;
  protected readonly fieldLabel = territorialFieldLabel;
  protected readonly severityLabel = territorialSeverityLabel;
  protected readonly approvalLabel = territorialApprovalLabel;
  protected readonly publicationModeLabel = territorialPublicationModeLabel;
  protected readonly nameKindLabel = territorialNameKindLabel;
  protected readonly typeLabel = territorialTypeLabel;
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
  protected selectedChange: ChangeItem | null = null;
  protected changeHierarchy: ChangeHierarchy | null = null;
  protected hierarchySearch = '';
  protected hierarchyBusy = false;
  protected hierarchyError = '';
  protected changeDetailTab: 'general' | 'hierarchy' | 'names' | 'provenance' | 'changes' = 'general';
  protected drafts: MappingProjectionDraft[] = [];

  private readonly api = inject(TerritorialAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private pollHandle: ReturnType<typeof setTimeout> | null = null;
  private changeReturnFocus: HTMLElement | null = null;

  protected readonly changeDetailTabs = [
    { id: 'general' as const, label: 'General' },
    { id: 'hierarchy' as const, label: 'Jerarquia' },
    { id: 'names' as const, label: 'Noms i codis' },
    { id: 'provenance' as const, label: 'Procedència' },
    { id: 'changes' as const, label: 'Canvis' }
  ];

  ngOnChanges(changes: SimpleChanges): void {
    const id = changes['requestedImportId']?.currentValue as string | null;
    if (id) void this.open(id);
  }

  ngOnDestroy(): void { this.stopPolling(); }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.selectedChange) this.closeChangeDetail();
  }

  protected get isProcessing(): boolean {
    return !!this.detail && (['Queued', 'Uploaded', 'Mapped', 'Publishing'].includes(this.detail.import.status) ||
      (this.detail.import.status === 'Validated' && !this.detail.import.hasBlockingErrors));
  }

  protected get progressPercent(): number {
    const total = this.detail?.import.totalRows ?? 0;
    const processed = this.detail?.import.processedRows ?? 0;
    return total > 0 ? Math.min(100, Math.round(processed * 100 / total)) : 0;
  }

  protected coordinates(latitude: number | null, longitude: number | null): string {
    return latitude === null || longitude === null ? 'No disponibles' : `${latitude}, ${longitude}`;
  }

  protected get source() {
    return this.context.sources.find((item) => item.id === this.sourceId) ?? null;
  }

  protected get sourcePreviewColumns(): string[] {
    const columns = new Set<string>();
    for (const row of this.sourcePreview.items) Object.keys(row.values).forEach((key) => columns.add(key));
    return [...columns];
  }

  protected get requiresSourceSheet(): boolean { return (this.detail?.sourceSheets?.length ?? 0) > 1; }

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
      this.initializeSourceSheet();
      this.step = 4;
      this.importChanged.emit(this.detail!.import.id);
      this.startPolling();
    });
  }

  protected async open(id: string): Promise<void> {
    await this.run(async () => {
      this.detail = await this.api.detail(id);
      this.sourceId = this.detail.import.datasetSourceId;
      this.initializeSourceSheet();
      if (this.isProcessing) this.startPolling();
      else {
        await this.loadEvidence();
        this.step = this.stepFor(this.detail);
      }
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
    if (kind === 'source' && this.requiresSourceSheet && !this.previewSheet) return;
    await this.run(async () => {
      const filters = { page, sheet: this.previewSheet || undefined, search: this.previewSearch || undefined };
      if (kind === 'source') this.sourcePreview = await this.api.sourcePreview(this.detail!.import.id, filters);
      else this.canonicalPreview = await this.api.canonicalPreview(this.detail!.import.id, filters);
    });
  }

  protected async filterPreview(): Promise<void> {
    await this.run(async () => {
      if (!this.detail) return;
      this.sourcePreview = this.requiresSourceSheet && !this.previewSheet
        ? this.emptyPage()
        : await this.api.sourcePreview(this.detail.import.id, {
          sheet: this.previewSheet || undefined, search: this.previewSearch || undefined
        });
    });
  }

  protected async selectPreviewSheet(): Promise<void> { await this.filterPreview(); }

  protected openChangeDetail(change: ChangeItem, event: Event): void {
    this.changeReturnFocus = event.currentTarget instanceof HTMLElement ? event.currentTarget : null;
    this.selectedChange = change;
    this.changeHierarchy = null;
    this.hierarchySearch = '';
    this.hierarchyError = '';
    this.changeDetailTab = 'general';
    this.cdr.markForCheck();
    setTimeout(() => this.changeDialogClose?.nativeElement.focus());
    void this.loadChangeHierarchy(change.id, 1);
  }

  protected closeChangeDetail(): void {
    this.selectedChange = null;
    this.changeHierarchy = null;
    this.hierarchySearch = '';
    this.hierarchyError = '';
    const target = this.changeReturnFocus;
    this.changeReturnFocus = null;
    this.cdr.markForCheck();
    queueMicrotask(() => target?.focus());
  }

  protected selectChangeDetailTab(tab: 'general' | 'hierarchy' | 'names' | 'provenance' | 'changes'): void {
    this.changeDetailTab = tab;
  }

  protected async navigateChangeHierarchy(changeId: string): Promise<void> {
    this.hierarchySearch = '';
    await this.loadChangeHierarchy(changeId, 1);
  }

  protected async filterHierarchyChildren(): Promise<void> {
    if (!this.selectedChange) return;
    await this.loadChangeHierarchy(this.selectedChange.id, 1);
  }

  protected async hierarchyChildrenPage(page: number): Promise<void> {
    if (!this.selectedChange) return;
    await this.loadChangeHierarchy(this.selectedChange.id, page);
  }

  protected formatDate(value: string | null): string {
    if (!value) return 'No informada';
    return new Intl.DateTimeFormat('ca-ES', { dateStyle: 'medium' }).format(new Date(`${value}T00:00:00`));
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
      if (this.isProcessing) this.startPolling();
      else await this.loadEvidence();
      this.step = action === 'publish' ? 6 : this.stepFor(this.detail);
      this.importChanged.emit(this.detail!.import.id);
    });
  }

  protected reset(): void {
    this.stopPolling();
    this.step = 1; this.file = null; this.inspection = null; this.mappingId = '';
    this.detail = null; this.issues = this.emptyPage(); this.changes = this.emptyPage();
    this.sourcePreview = this.emptyPage(); this.canonicalPreview = this.emptyPage();
    this.error = ''; this.confirmAction = null; this.selectedChange = null; this.previewSheet = ''; this.previewSearch = ''; this.drafts = [];
  }

  private async loadEvidence(): Promise<void> {
    if (!this.detail) return;
    const sourcePromise = this.requiresSourceSheet && !this.previewSheet
      ? Promise.resolve(this.emptyPage<SourcePreviewRow>())
      : this.api.sourcePreview(this.detail.import.id, { sheet: this.previewSheet || undefined });
    [this.issues, this.changes, this.sourcePreview, this.canonicalPreview] = await Promise.all([
      this.api.issues(this.detail.import.id), this.api.changes(this.detail.import.id), sourcePromise,
      this.api.canonicalPreview(this.detail.import.id)
    ]);
  }

  private initializeSourceSheet(): void {
    const sheets = this.detail?.sourceSheets ?? [];
    if (sheets.length === 1) this.previewSheet = sheets[0];
    else if (!sheets.includes(this.previewSheet)) this.previewSheet = '';
  }

  private async loadChangeHierarchy(changeId: string, page: number): Promise<void> {
    if (!this.detail) return;
    this.hierarchyBusy = true;
    this.hierarchyError = '';
    this.cdr.markForCheck();
    try {
      const hierarchy = await this.api.changeHierarchy(this.detail.import.id, changeId, {
        search: this.hierarchySearch || undefined, page, pageSize: 25
      });
      this.changeHierarchy = hierarchy;
      this.selectedChange = hierarchy.current;
    } catch (reason) {
      this.hierarchyError = this.message(reason);
    } finally {
      this.hierarchyBusy = false;
      this.cdr.markForCheck();
    }
  }

  private startPolling(): void {
    this.stopPolling();
    if (!this.isProcessing) return;
    this.pollHandle = setTimeout(() => void this.poll(), 1000);
  }

  private async poll(): Promise<void> {
    if (!this.detail) return;
    try {
      this.detail = await this.api.detail(this.detail.import.id);
      this.initializeSourceSheet();
      if (this.isProcessing) this.startPolling();
      else {
        await this.loadEvidence();
        this.step = this.stepFor(this.detail);
        this.importChanged.emit(this.detail.import.id);
      }
    } catch (reason) {
      this.error = this.message(reason);
      this.stopPolling();
    } finally { this.cdr.markForCheck(); }
  }

  private stopPolling(): void {
    if (this.pollHandle !== null) clearTimeout(this.pollHandle);
    this.pollHandle = null;
  }

  private stepFor(detail: ImportDetail): number {
    if (detail.import.status === 'ReadyForReview') return 5;
    if (detail.import.status === 'Published' || detail.import.status === 'Reverted') return 6;
    return 4;
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
