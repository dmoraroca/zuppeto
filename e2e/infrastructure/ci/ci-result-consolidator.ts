import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { basename, dirname, join } from 'node:path';
import type { RunSummary } from '../../domain/summary.js';
import type { ExcelExecution, ExcelSyncGateway } from '../../ports/excel-sync-gateway.js';
import { deferredExcelQueueFile } from './deferred-excel-sync-queue.js';

export interface ConsolidationResult {
  readonly runs: number;
  readonly executions: number;
  readonly alreadyPresent: number;
  readonly byOutcome: Readonly<Record<string, number>>;
}

export class CiResultConsolidator {
  public constructor(private readonly excel: ExcelSyncGateway) {}

  public async consolidate(artifactsRoot: string, summaryOutput: string): Promise<ConsolidationResult> {
    const files = await listFiles(artifactsRoot);
    const executions = validateCiExecutions(await readJsonlFiles(files.filter((path) => basename(path) === deferredExcelQueueFile)));
    const summaries = await readSummaries(files.filter((path) => basename(path) === 'summary.json'));
    let alreadyPresent = 0;
    for (const execution of executions) {
      const result = await this.excel.synchronize(execution);
      if (result.status === 'ALREADY_SYNCED') alreadyPresent += 1;
      if (result.status === 'NOT_SYNCED' || result.status === 'INTEGRITY_ERROR') {
        throw new Error(`Consolidació Excel rebutjada per ${execution.executionId}: ${result.status}.`);
      }
    }
    const byOutcome = tally(executions.map((execution) => execution.outcome));
    const result = { runs: summaries.length, executions: executions.length, alreadyPresent, byOutcome };
    await mkdir(dirname(summaryOutput), { recursive: true });
    await writeFile(summaryOutput, JSON.stringify({ ...result, summaries }, null, 2) + '\n', 'utf8');
    return result;
  }
}

export function validateCiExecutions(executions: readonly ExcelExecution[]): readonly ExcelExecution[] {
  const ids = new Set<string>();
  for (const execution of executions) {
    if (!execution.executionId || !execution.runId || !execution.executionId.startsWith(`${execution.runId}--`)) {
      throw new Error('La cua CI conté un executionId invàlid.');
    }
    if (execution.environment !== 'CI' || execution.origin !== 'CI') throw new Error(`L’execució ${execution.executionId} no està marcada com a CI.`);
    if (ids.has(execution.executionId)) throw new Error(`executionId CI duplicat: ${execution.executionId}`);
    ids.add(execution.executionId);
  }
  return [...executions].sort((left, right) => left.startedAt.localeCompare(right.startedAt) || left.executionId.localeCompare(right.executionId));
}

async function readJsonlFiles(paths: readonly string[]): Promise<ExcelExecution[]> {
  const result: ExcelExecution[] = [];
  for (const path of [...paths].sort()) {
    const lines = (await readFile(path, 'utf8')).split('\n').filter((line) => line.trim());
    for (const line of lines) result.push(JSON.parse(line) as ExcelExecution);
  }
  return result;
}

async function readSummaries(paths: readonly string[]): Promise<RunSummary[]> {
  const summaries: RunSummary[] = [];
  for (const path of [...paths].sort()) summaries.push(JSON.parse(await readFile(path, 'utf8')) as RunSummary);
  const runIds = summaries.map((summary) => summary.runId);
  if (new Set(runIds).size !== runIds.length) throw new Error('La consolidació conté runId duplicats.');
  return summaries;
}

async function listFiles(root: string): Promise<string[]> {
  const { readdir } = await import('node:fs/promises');
  const result: string[] = [];
  async function visit(directory: string): Promise<void> {
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      const path = join(directory, entry.name);
      if (entry.isDirectory()) await visit(path);
      else if (entry.isFile()) result.push(path);
    }
  }
  await visit(root);
  return result;
}

function tally(values: readonly string[]): Record<string, number> {
  const result: Record<string, number> = {};
  for (const value of values) result[value] = (result[value] ?? 0) + 1;
  return result;
}
