import { ChangeDetectionStrategy, Component, HostListener, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CatalogAudit, CatalogDetail } from '../../models/territorial-admin.model';
import { territorialNameKindLabel } from '../../policies/territorial-presentation.policy';

export interface TerritorialMaintenanceCommand {
  action: string;
  reason: string;
  isSelectableLocality?: boolean;
  latitude?: number;
  longitude?: number;
}

type DetailTab = 'general' | 'hierarchy' | 'names' | 'coordinates' | 'provenance' | 'audit';
type MaintenanceAction = 'activate' | 'deactivate' | 'set-selectable' | 'set-coordinates';

@Component({
  selector: 'app-territorial-catalog-detail',
  imports: [FormsModule],
  templateUrl: './territorial-catalog-detail.component.html',
  styleUrl: './territorial-catalog-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialCatalogDetailComponent {
  protected readonly nameKindLabel = territorialNameKindLabel;
  readonly detail = input.required<CatalogDetail>();
  readonly busy = input(false);
  readonly error = input('');
  readonly closeDetail = output<void>();
  readonly openUnit = output<string>();
  readonly filterChildren = output<{ id: string; name: string }>();
  readonly maintenance = output<TerritorialMaintenanceCommand>();

  protected readonly activeTab = signal<DetailTab>('general');
  protected readonly pendingAction = signal<MaintenanceAction | null>(null);
  protected reason = '';
  protected latitude: number | null = null;
  protected longitude: number | null = null;

  protected readonly tabs: ReadonlyArray<{ id: DetailTab; label: string }> = [
    { id: 'general', label: 'General' },
    { id: 'hierarchy', label: 'Jerarquia' },
    { id: 'names', label: 'Noms i codis' },
    { id: 'coordinates', label: 'Coordenades' },
    { id: 'provenance', label: 'Procedència' },
    { id: 'audit', label: 'Auditoria' }
  ];

  protected get hasValidReason(): boolean {
    const length = this.reason.trim().length;
    return length >= 3 && length <= 500;
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.pendingAction()) this.cancelMaintenance();
    else this.closeDetail.emit();
  }

  protected selectTab(tab: DetailTab): void { this.activeTab.set(tab); }

  protected beginMaintenance(action: MaintenanceAction): void {
    this.pendingAction.set(action);
    this.reason = '';
    this.latitude = this.detail().unit.latitude;
    this.longitude = this.detail().unit.longitude;
  }

  protected cancelMaintenance(): void {
    this.pendingAction.set(null);
    this.reason = '';
  }

  protected confirmMaintenance(): void {
    const action = this.pendingAction();
    if (!action || !this.hasValidReason || (action === 'set-coordinates' && !this.hasValidCoordinates)) return;
    this.maintenance.emit({
      action,
      reason: this.reason.trim(),
      isSelectableLocality: action === 'set-selectable' ? !this.detail().unit.isSelectableLocality : undefined,
      latitude: action === 'set-coordinates' ? this.latitude ?? undefined : undefined,
      longitude: action === 'set-coordinates' ? this.longitude ?? undefined : undefined
    });
    this.cancelMaintenance();
    if (action !== 'set-coordinates') this.activeTab.set('audit');
  }

  protected get hasValidCoordinates(): boolean {
    if (this.latitude === null || this.longitude === null) return false;
    if (this.latitude < -90 || this.latitude > 90 || this.longitude < -180 || this.longitude > 180) return false;
    return this.latitude !== 0 || this.longitude !== 0;
  }

  protected get actionTitle(): string {
    const name = this.detail().unit.primaryName;
    switch (this.pendingAction()) {
      case 'activate': return `Reactivar ${name}`;
      case 'deactivate': return `Desactivar ${name}`;
      case 'set-selectable': return `${this.detail().unit.isSelectableLocality ? 'Fer no seleccionable' : 'Fer seleccionable'} ${name}`;
      case 'set-coordinates': return `Corregir coordenades de ${name}`;
      default: return '';
    }
  }

  protected get actionLabel(): string {
    switch (this.pendingAction()) {
      case 'activate': return 'Reactivar';
      case 'deactivate': return 'Desactivar';
      case 'set-selectable': return this.detail().unit.isSelectableLocality ? 'Fer no seleccionable' : 'Fer seleccionable';
      case 'set-coordinates': return 'Corregir coordenades';
      default: return 'Confirmar';
    }
  }

  protected get canConfirm(): boolean {
    return !this.busy() && this.hasValidReason && (this.pendingAction() !== 'set-coordinates' || this.hasValidCoordinates);
  }

  protected auditAction(action: string): string {
    const labels: Record<string, string> = {
      activate: 'Reactivació', deactivate: 'Desactivació', 'set-selectable': 'Seleccionabilitat',
      'set-coordinates': 'Correcció de coordenades'
    };
    return labels[action] ?? action;
  }

  protected auditValue(audit: CatalogAudit, value: string | null): string {
    if (value === null || value === '') return 'Sense valor';
    if (audit.field === 'isActive') return value === 'true' ? 'Actiu' : value === 'false' ? 'Inactiu' : value;
    if (audit.field === 'isSelectableLocality') return value === 'true' ? 'Seleccionable' : value === 'false' ? 'No seleccionable' : 'Configuració del tipus';
    try {
      const parsed = JSON.parse(value) as unknown;
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        return Object.entries(parsed as Record<string, unknown>).map(([key, item]) => `${key}: ${item ?? '—'}`).join(' · ');
      }
      return String(parsed ?? 'Sense valor');
    } catch { return value; }
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat('ca-ES', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
  }
}
