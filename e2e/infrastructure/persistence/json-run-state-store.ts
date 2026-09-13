import { mkdir, readFile, rename, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import type { RunState } from '../../domain/run-state.js';
import type { RunSummary } from '../../domain/summary.js';
import type { RunStateStore } from '../../ports/run-state-store.js';

export class JsonRunStateStore implements RunStateStore {
  public constructor(private readonly runsDirectory: string) {}
  public async create(state: RunState): Promise<void> { await this.writeAtomic(this.statePath(state.runId), state); }
  public async load(runId: string): Promise<RunState> { return JSON.parse(await readFile(this.statePath(runId), 'utf8')) as RunState; }
  public async save(state: RunState): Promise<void> { await this.writeAtomic(this.statePath(state.runId), state); }
  public async saveSummary(summary: RunSummary): Promise<void> { await this.writeAtomic(join(this.runDirectory(summary.runId), 'summary.json'), summary); }
  private statePath(runId: string): string { return join(this.runDirectory(runId), 'state.json'); }
  private runDirectory(runId: string): string { return join(this.runsDirectory, runId); }
  private async writeAtomic(path: string, value: unknown): Promise<void> {
    await mkdir(dirname(path), { recursive: true });
    const temporary = path + '.tmp-' + process.pid + '-' + Date.now();
    await writeFile(temporary, JSON.stringify(value, null, 2) + '\n', 'utf8');
    await rename(temporary, path);
  }
}
