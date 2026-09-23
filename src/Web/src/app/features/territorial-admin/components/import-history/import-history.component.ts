import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { DatePipe } from '@angular/common';

import { PageResult, TerritorialImport } from '../../models/territorial-admin.model';

@Component({
  selector: 'app-territorial-import-history',
  imports: [DatePipe],
  templateUrl: './import-history.component.html',
  styleUrl: './import-history.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialImportHistoryComponent {
  @Input({ required: true }) history!: PageResult<TerritorialImport>;
  @Input() busy = false;
  @Output() readonly openImport = new EventEmitter<string>();
  @Output() readonly pageChange = new EventEmitter<number>();
}
