import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  CatalogDescendantType, CatalogHierarchy, CatalogHierarchyNode, CatalogUnit, PageResult
} from '../../models/territorial-admin.model';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';

interface TreeEntry { node: CatalogHierarchyNode; depth: number; children: TreeEntry[]; }
interface ChildState {
  page: PageResult<CatalogHierarchyNode>;
  search: string;
  error: string;
}

@Component({
  selector: 'app-territorial-hierarchy-explorer',
  imports: [FormsModule, NgTemplateOutlet],
  templateUrl: './territorial-hierarchy-explorer.component.html',
  styleUrl: './territorial-hierarchy-explorer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialHierarchyExplorerComponent implements OnInit {
  readonly rootId = input.required<string>();
  readonly countryName = input.required<string>();
  readonly openUnit = output<string>();
  protected readonly reducedMotion = typeof window !== 'undefined'
    && typeof window.matchMedia === 'function'
    && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  protected hierarchy: CatalogHierarchy | null = null;
  protected rootLoading = true;
  protected rootError = '';
  protected selectedNodeId = '';
  protected childSearch = '';
  protected descendantType: CatalogDescendantType | null = null;
  protected descendantSearch = '';
  protected descendants: PageResult<CatalogUnit> = this.emptyPage<CatalogUnit>();
  protected descendantError = '';
  protected descendantLoading = false;
  protected descendantSkeleton = false;

  private readonly api = inject(TerritorialAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly nodes = new Map<string, CatalogHierarchyNode>();
  protected readonly children = new Map<string, ChildState>();
  private readonly expanded = new Set<string>();
  private readonly loading = new Set<string>();
  private readonly skeleton = new Set<string>();

  async ngOnInit(): Promise<void> { await this.loadRoot(); }

  protected get treeRoot(): TreeEntry | null {
    if (!this.hierarchy) return null;
    const path = [...this.hierarchy.ancestors, this.hierarchy.current];
    return this.buildPathEntry(path, 0, 1);
  }

  protected get selectedNode(): CatalogHierarchyNode | null {
    return this.nodes.get(this.selectedNodeId) ?? null;
  }

  protected get selectedChildren(): ChildState | null {
    return this.children.get(this.selectedNodeId) ?? null;
  }

  protected isExpanded(id: string): boolean { return this.expanded.has(id); }
  protected isLoading(id: string): boolean { return this.loading.has(id); }
  protected showsSkeleton(id: string): boolean { return this.skeleton.has(id); }

  protected async toggle(node: CatalogHierarchyNode): Promise<void> {
    this.selectedNodeId = node.id;
    this.childSearch = this.children.get(node.id)?.search ?? '';
    if (this.expanded.has(node.id)) {
      this.expanded.delete(node.id);
      this.cdr.markForCheck();
      return;
    }
    if (node.directChildCount === 0) return;
    if (!this.children.has(node.id)) await this.loadChildren(node.id, 1, '');
    if (this.children.has(node.id)) this.expanded.add(node.id);
    this.cdr.markForCheck();
  }

  protected async searchChildren(): Promise<void> {
    if (!this.selectedNodeId) return;
    await this.loadChildren(this.selectedNodeId, 1, this.childSearch);
    this.expanded.add(this.selectedNodeId);
  }

  protected async childrenPage(page: number): Promise<void> {
    if (!this.selectedNodeId) return;
    await this.loadChildren(this.selectedNodeId, page, this.childSearch);
    this.expanded.add(this.selectedNodeId);
  }

  protected async retryChildren(id: string): Promise<void> {
    const state = this.children.get(id);
    await this.loadChildren(id, state?.page.page ?? 1, state?.search ?? '');
    if (this.children.get(id)?.error === '') this.expanded.add(id);
  }

  protected async showDescendants(type: CatalogDescendantType): Promise<void> {
    this.descendantType = type;
    this.descendantSearch = '';
    await this.loadDescendants(1);
  }

  protected async searchDescendants(): Promise<void> { await this.loadDescendants(1); }
  protected async descendantsPage(page: number): Promise<void> { await this.loadDescendants(page); }
  protected closeDescendants(): void { this.descendantType = null; this.descendantError = ''; }

  protected descendantActionLabel(type: CatalogDescendantType): string {
    if (type.typeCode.includes('MUNICIPALITY')) return 'Tots els municipis';
    if (type.typeCode === 'GEMEINDE') return 'Totes les Gemeinde';
    return `Totes les unitats ${type.type}`;
  }

  protected async loadRoot(): Promise<void> {
    this.rootLoading = true;
    this.rootError = '';
    this.cdr.markForCheck();
    try {
      const hierarchy = await this.api.catalogHierarchy(this.rootId());
      this.hierarchy = hierarchy;
      [...hierarchy.ancestors, hierarchy.current, ...hierarchy.children.items]
        .forEach((node) => this.nodes.set(node.id, node));
      this.children.set(hierarchy.current.id, { page: hierarchy.children, search: '', error: '' });
      this.selectedNodeId = hierarchy.current.id;
      hierarchy.ancestors.forEach((node) => this.expanded.add(node.id));
      this.expanded.add(hierarchy.current.id);
    } catch (reason) {
      this.rootError = this.message(reason);
    } finally {
      this.rootLoading = false;
      this.cdr.markForCheck();
    }
  }

  private async loadChildren(id: string, page: number, search: string): Promise<void> {
    if (this.loading.has(id)) return;
    this.loading.add(id);
    let skeletonShownAt = 0;
    const timer = window.setTimeout(() => {
      skeletonShownAt = Date.now();
      this.skeleton.add(id);
      this.cdr.markForCheck();
    }, 120);
    this.children.set(id, { page: this.children.get(id)?.page ?? this.emptyPage<CatalogHierarchyNode>(), search, error: '' });
    this.cdr.markForCheck();
    try {
      const hierarchy = await this.api.catalogHierarchy(id, { search: search || undefined, page, pageSize: 50 });
      this.nodes.set(hierarchy.current.id, hierarchy.current);
      hierarchy.children.items.forEach((node) => this.nodes.set(node.id, node));
      this.children.set(id, { page: hierarchy.children, search, error: '' });
    } catch (reason) {
      const previous = this.children.get(id)?.page ?? this.emptyPage<CatalogHierarchyNode>();
      this.children.set(id, { page: previous, search, error: this.message(reason) });
    } finally {
      window.clearTimeout(timer);
      if (skeletonShownAt) await this.minimumSkeletonDuration(skeletonShownAt);
      this.loading.delete(id);
      this.skeleton.delete(id);
      this.cdr.markForCheck();
    }
  }

  protected async loadDescendants(page: number): Promise<void> {
    if (!this.descendantType || this.descendantLoading) return;
    this.descendantLoading = true;
    this.descendantError = '';
    let skeletonShownAt = 0;
    const timer = window.setTimeout(() => {
      skeletonShownAt = Date.now();
      this.descendantSkeleton = true;
      this.cdr.markForCheck();
    }, 120);
    this.cdr.markForCheck();
    try {
      this.descendants = await this.api.catalogDescendants(this.rootId(), this.descendantType.territorialUnitTypeId,
        { search: this.descendantSearch || undefined, page, pageSize: 50 });
    } catch (reason) {
      this.descendantError = this.message(reason);
    } finally {
      window.clearTimeout(timer);
      if (skeletonShownAt) await this.minimumSkeletonDuration(skeletonShownAt);
      this.descendantSkeleton = false;
      this.descendantLoading = false;
      this.cdr.markForCheck();
    }
  }

  protected childCountLabel(node: CatalogHierarchyNode): string {
    const child = this.children.get(node.id)?.page.items[0];
    if (!child) return `${node.directChildCount} ${node.directChildCount === 1 ? 'fill' : 'fills'}`;
    const labels: Record<string, [string, string]> = {
      AUTONOMOUS_COMMUNITY: ['comunitat autònoma', 'comunitats autònomes'],
      AUTONOMOUS_CITY: ['ciutat autònoma', 'ciutats autònomes'],
      PROVINCE: ['província', 'províncies'],
      MUNICIPALITY: ['municipi', 'municipis'],
      REGIERUNGSBEZIRK: ['Regierungsbezirk', 'Regierungsbezirke'],
      REGION: ['regió', 'regions'],
      KREIS: ['Kreis', 'Kreise'],
      GEMEINDEVERBAND: ['Gemeindeverband', 'Gemeindeverbände'],
      GEMEINDE: ['Gemeinde', 'Gemeinden']
    };
    const label = labels[child.typeCode] ?? [child.type, `unitats ${child.type}`];
    return `${node.directChildCount} ${node.directChildCount === 1 ? label[0] : label[1]}`;
  }

  protected descendantSummary(node: CatalogHierarchyNode): string {
    if (node.id !== this.hierarchy?.current.id) return '';
    const descendant = this.hierarchy.descendantTypes.find((type) => type.isSelectableLocality);
    return descendant && descendant.count !== node.directChildCount
      ? `${descendant.count} ${descendant.typeCode.includes('MUNICIPALITY') ? 'municipis' : descendant.type}`
      : '';
  }

  private buildPathEntry(path: CatalogHierarchyNode[], index: number, depth: number): TreeEntry {
    const node = path[index];
    const knownPathChild = path[index + 1];
    const children = !this.expanded.has(node.id)
      ? []
      : knownPathChild
        ? this.mergeKnownPathWithLoadedChildren(path, index, depth + 1)
        : this.loadedEntries(node, depth + 1);
    return { node, depth, children };
  }

  private mergeKnownPathWithLoadedChildren(path: CatalogHierarchyNode[], index: number, depth: number): TreeEntry[] {
    const parent = path[index];
    const knownPathChild = path[index + 1];
    const loaded = this.children.get(parent.id)?.page.items;
    if (!loaded?.length) return [this.buildPathEntry(path, index + 1, depth)];

    const entries = loaded.map((child) => child.id === knownPathChild.id
      ? this.buildPathEntry(path, index + 1, depth)
      : this.buildLoadedEntry(child, depth));
    return loaded.some((child) => child.id === knownPathChild.id)
      ? entries
      : [this.buildPathEntry(path, index + 1, depth), ...entries];
  }

  private loadedEntries(parent: CatalogHierarchyNode, depth: number): TreeEntry[] {
    const state = this.children.get(parent.id);
    if (!state || state.error) return [];
    return state.page.items.map((node) => this.buildLoadedEntry(node, depth));
  }

  private buildLoadedEntry(node: CatalogHierarchyNode, depth: number): TreeEntry {
    return { node, depth, children: this.expanded.has(node.id) ? this.loadedEntries(node, depth + 1) : [] };
  }

  private message(reason: unknown): string {
    const failure = reason as { error?: { message?: string }; message?: string };
    return failure.error?.message ?? failure.message ?? 'No s’han pogut carregar les unitats.';
  }

  private async minimumSkeletonDuration(shownAt: number): Promise<void> {
    const remaining = 180 - (Date.now() - shownAt);
    if (remaining > 0) await new Promise<void>((resolve) => window.setTimeout(resolve, remaining));
  }

  private emptyPage<T>(): PageResult<T> { return { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 }; }
}
