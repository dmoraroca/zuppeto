import { appendFile, mkdir, readFile } from 'node:fs/promises';
import { join } from 'node:path';
import type { ExcelExecution, ExcelSyncGateway, ExcelSyncResult } from '../../ports/excel-sync-gateway.js';

export const deferredExcelQueueFile = 'excel-sync.jsonl';

export class DeferredExcelSyncQueue implements ExcelSyncGateway {
  public constructor(private readonly runsDirectory: string) {}

  public async migrate(): Promise<void> {}

  public async synchronize(execution: ExcelExecution): Promise<ExcelSyncResult> {
    const directory = join(this.runsDirectory, execution.runId);
    const path = join(directory, deferredExcelQueueFile);
    await mkdir(directory, { recursive: true });
    const existing = await this.read(path);
    if (existing.some((item) => item.executionId === execution.executionId)) {
      return { status: 'ALREADY_SYNCED', executionRecorded: false, provesUpdated: false, message: 'executionId ja present a la cua CI.' };
    }
    await appendFile(path, JSON.stringify(execution) + '\n', { encoding: 'utf8', mode: 0o600 });
    return { status: 'NOT_SYNCED', executionRecorded: true, provesUpdated: false, message: 'Execució en cua per a la consolidació CI serialitzada.' };
  }

  private async read(path: string): Promise<readonly ExcelExecution[]> {
    try {
      return (await readFile(path, 'utf8')).split('\n').filter((line) => line.trim()).map((line) => JSON.parse(line) as ExcelExecution);
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') return [];
      throw error;
    }
  }
}
