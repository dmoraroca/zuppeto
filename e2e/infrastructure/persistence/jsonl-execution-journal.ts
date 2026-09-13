import { appendFile, mkdir, readFile } from 'node:fs/promises';
import { join } from 'node:path';
import type { ExecutionRecord } from '../../domain/execution.js';
import type { ExecutionJournal } from '../../ports/execution-journal.js';

export class JsonlExecutionJournal implements ExecutionJournal {
  public constructor(private readonly runsDirectory: string) {}
  public async append(record: ExecutionRecord): Promise<boolean> {
    if (await this.has(record.executionId)) return false;
    await mkdir(this.runDirectory(record.runId), { recursive: true });
    await appendFile(this.path(record.runId), JSON.stringify(record) + '\n', 'utf8');
    return true;
  }
  public async list(runId: string): Promise<readonly ExecutionRecord[]> {
    try {
      return (await readFile(this.path(runId), 'utf8')).split('\n').filter((line) => line.trim()).map((line) => JSON.parse(line) as ExecutionRecord);
    } catch (error: unknown) {
      if (typeof error === 'object' && error !== null && (error as NodeJS.ErrnoException).code === 'ENOENT') return [];
      throw error;
    }
  }
  public async has(executionId: string): Promise<boolean> {
    const runId = executionId.split('--')[0];
    return (await this.list(runId)).some((record) => record.executionId === executionId);
  }
  private path(runId: string): string { return join(this.runDirectory(runId), 'executions.jsonl'); }
  private runDirectory(runId: string): string { return join(this.runsDirectory, runId); }
}
