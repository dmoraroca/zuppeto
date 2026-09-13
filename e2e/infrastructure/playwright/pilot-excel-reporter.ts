import type { ExecutionRecord } from '../../domain/execution.js';
import { ExcelWorkbookSync } from '../excel/excel-workbook-sync.js';
import { findPilotScenario } from '../../scenarios/pilot/pilot-scenarios.js';
import type { PilotPlaywrightExecutor } from './pilot-playwright-executor.js';

export class PilotExcelReporter {
  private readonly synced = new Set<string>();

  public constructor(private readonly excel: ExcelWorkbookSync, private readonly executor: PilotPlaywrightExecutor, private readonly environment: string, private readonly commit: string) {}

  public async synchronize(records: readonly ExecutionRecord[]): Promise<void> {
    for (const record of records) {
      if (this.synced.has(record.executionId)) continue;
      const scenario = findPilotScenario(record.scenarioId);
      if (scenario === undefined) continue;
      const evidence = this.executor.evidenceFor(record.scenarioId, record.attempt);
      await this.excel.synchronize({
        runId: record.runId, executionId: record.executionId, scenario: scenario.key, variant: scenario.variant,
        engine: 'Blink', browserVersion: this.executor.version(), environment: this.environment, commit: this.commit,
        startedAt: record.startedAt, finishedAt: record.finishedAt, durationMs: record.durationMs, attempt: record.attempt,
        outcome: record.outcome, javascriptErrors: evidence.javascriptErrors, networkErrors: evidence.networkErrors,
        message: [record.message, evidence.cleanupErrors].filter((value) => value !== '').join('\n'),
        evidencePaths: evidence.evidencePaths, origin: 'LOCAL'
      });
      this.synced.add(record.executionId);
    }
  }
}
