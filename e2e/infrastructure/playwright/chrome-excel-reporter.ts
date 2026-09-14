import type { ExecutionRecord } from '../../domain/execution.js';
import type { ChromeScenarioCatalog } from '../../scenarios/chrome/chrome-scenario-catalog.js';
import type { ExcelExecution, ExcelSyncGateway } from '../../ports/excel-sync-gateway.js';
import type { ChromePlaywrightExecutor } from './chrome-playwright-executor.js';
import type { BrowserTarget } from '../../domain/browser-target.js';

export class ChromeExcelReporter {
  private readonly synced = new Set<string>();
  public constructor(
    private readonly excel: ExcelSyncGateway,
    private readonly executor: ChromePlaywrightExecutor,
    private readonly catalog: ChromeScenarioCatalog,
    private readonly commit: string,
    private readonly browser: BrowserTarget = { key: 'chrome', name: 'Chrome', engine: 'Blink' },
    private readonly environment = 'LOCAL',
    private readonly origin: 'LOCAL' | 'CI' = 'LOCAL'
  ) {}
  public async synchronize(records: readonly ExecutionRecord[]): Promise<void> {
    const pending: ExcelExecution[] = [];
    for (const record of records) {
      if (this.synced.has(record.executionId)) continue;
      const scenario = this.catalog.find(record.scenarioId); if (scenario === undefined) continue;
      const evidence = this.executor.evidenceFor(record.scenarioId, record.attempt);
      pending.push({
        runId: record.runId, executionId: record.executionId,
        scenario: { testCode: scenario.inventory.testCode, role: scenario.inventory.excelRole, browser: this.browser.name, scenarioId: scenario.inventory.variant },
        variant: scenario.inventory.variant, engine: this.browser.engine, browserVersion: this.executor.version(), environment: this.environment, commit: this.commit,
        startedAt: record.startedAt, finishedAt: record.finishedAt, durationMs: record.durationMs, attempt: record.attempt, outcome: record.outcome,
        javascriptErrors: evidence.javascriptErrors, networkErrors: evidence.networkErrors,
        message: [record.message, evidence.cleanupErrors].filter(Boolean).join('\n'), evidencePaths: evidence.evidencePaths, origin: this.origin
      });
    }
    const results = this.excel.synchronizeMany === undefined
      ? await synchronizeSequentially(this.excel, pending)
      : await this.excel.synchronizeMany(pending);
    results.forEach((result, index) => {
      if (result.status !== 'NOT_SYNCED') this.synced.add(pending[index]!.executionId);
    });
  }
}

async function synchronizeSequentially(excel: ExcelSyncGateway, executions: readonly ExcelExecution[]) {
  const results = [];
  for (const execution of executions) results.push(await excel.synchronize(execution));
  return results;
}
