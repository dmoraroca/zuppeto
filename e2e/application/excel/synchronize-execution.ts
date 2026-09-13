import type { ExcelExecution, ExcelSyncGateway, ExcelSyncResult } from '../../ports/excel-sync-gateway.js';

export class SynchronizeExcelExecution {
  public constructor(private readonly gateway: ExcelSyncGateway) {}

  public async execute(execution: ExcelExecution): Promise<ExcelSyncResult> {
    return this.gateway.synchronize(execution);
  }
}
