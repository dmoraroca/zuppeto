import { mkdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { execFileSync } from 'node:child_process';
import { ExecuteScenario } from '../application/execute-scenario.js';
import { FinalizeRun } from '../application/finalize-run.js';
import { RunOrchestrator } from '../application/run-orchestrator.js';
import { ResumeRun } from '../application/resume-run.js';
import { StartRun } from '../application/start-run.js';
import { ExcelWorkbookSync } from '../infrastructure/excel/excel-workbook-sync.js';
import { JsonlExecutionJournal } from '../infrastructure/persistence/jsonl-execution-journal.js';
import { JsonRunStateStore } from '../infrastructure/persistence/json-run-state-store.js';
import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import { PilotExcelReporter } from '../infrastructure/playwright/pilot-excel-reporter.js';
import { PilotPlaywrightExecutor } from '../infrastructure/playwright/pilot-playwright-executor.js';
import { pilotScenarios } from '../scenarios/pilot/pilot-scenarios.js';
import { systemClock } from '../ports/clock.js';

const root = resolve(process.cwd(), '..');
const runsDirectory = join(process.cwd(), '.runs');
const workbookPath = join(root, 'docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');
const environmentPath = join(process.cwd(), '.env.e2e.local');

async function main(): Promise<void> {
  const options = parseOptions(process.argv.slice(2));
  const selected = options.scenario === undefined ? pilotScenarios : pilotScenarios.filter((scenario) => scenario.definition.id.value === options.scenario);
  if (!options.resume && selected.length === 0) throw new Error('Escenari pilot desconegut.');
  const environment = await loadPilotEnvironment(environmentPath);
  await mkdir(runsDirectory, { recursive: true });
  const store = new JsonRunStateStore(runsDirectory);
  const journal = new JsonlExecutionJournal(runsDirectory);
  const executor = new PilotPlaywrightExecutor(environment, runsDirectory, options.headed, options.interruptOnce);
  const orchestrator = new RunOrchestrator(
    store, journal, new ExecuteScenario(store, journal, executor, systemClock), new ResumeRun(store, journal, systemClock), new FinalizeRun(store, journal, systemClock), systemClock
  );
  const reporter = new PilotExcelReporter(new ExcelWorkbookSync(workbookPath), executor, 'LOCAL', commit());
  const finalState = options.resume
    ? await orchestrator.resume(required(options.runId, '--run-id'), showProgress)
    : await orchestrator.run(await new StartRun(store, systemClock).execute(selected.map((scenario) => scenario.definition)), showProgress);
  await reporter.synchronize(await journal.list(finalState.runId));
  console.log('Pilot run ' + finalState.runId + ' finished: ' + finalState.status + '.');
}

function parseOptions(argumentsList: readonly string[]): { scenario?: string; headed: boolean; resume: boolean; runId?: string; interruptOnce?: string } {
  const read = (name: string) => argumentsList.find((argument) => argument.startsWith(name + '='))?.slice(name.length + 1);
  return { scenario: read('--scenario'), headed: argumentsList.includes('--headed'), resume: argumentsList.includes('--resume'), runId: read('--run-id'), interruptOnce: read('--interrupt-once') };
}

function required(value: string | undefined, option: string): string {
  if (value === undefined || value === '') throw new Error('Falta ' + option + '.');
  return value;
}

function commit(): string {
  try { return execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(); }
  catch { return 'unknown'; }
}

function showProgress(value: { completedScenarios: number; totalScenarios: number; passed: number; failed: number; blocked: number; interrupted: number; currentScenarioId?: string }): void {
  console.log(value.completedScenarios + '/' + value.totalScenarios + ' PASS ' + value.passed + ' FAIL ' + value.failed + ' BLOCKED ' + value.blocked + ' INTERRUPTED ' + value.interrupted + ' current ' + (value.currentScenarioId ?? '-'));
}

void main().catch((error) => { console.error(error instanceof Error ? error.message : 'Error desconegut del pilot.'); process.exitCode = 1; });
