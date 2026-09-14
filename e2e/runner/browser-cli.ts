import { execFileSync } from 'node:child_process';
import { mkdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { ExecuteScenario } from '../application/execute-scenario.js';
import { FinalizeRun } from '../application/finalize-run.js';
import { ResumeRun } from '../application/resume-run.js';
import { RunOrchestrator, type RunProgress } from '../application/run-orchestrator.js';
import { StartRun } from '../application/start-run.js';
import type { BrowserTarget } from '../domain/browser-target.js';
import type { ChromeBlock } from '../domain/chrome-inventory.js';
import { parseSuiteProfile, selectSuiteProfile } from '../domain/e2e-suite-profile.js';
import { loadCiEnvironment, loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import { DeferredExcelSyncQueue } from '../infrastructure/ci/deferred-excel-sync-queue.js';
import { ExcelChromeScenarioInventory } from '../infrastructure/excel/excel-chrome-scenario-inventory.js';
import { ExcelWorkbookSync } from '../infrastructure/excel/excel-workbook-sync.js';
import { JsonlExecutionJournal } from '../infrastructure/persistence/jsonl-execution-journal.js';
import { JsonRunStateStore } from '../infrastructure/persistence/json-run-state-store.js';
import type { BrowserLauncher } from '../infrastructure/playwright/browser-launcher.js';
import { ChromeExcelReporter } from '../infrastructure/playwright/chrome-excel-reporter.js';
import { ChromePlaywrightExecutor } from '../infrastructure/playwright/chrome-playwright-executor.js';
import { ChromeScenarioCatalog } from '../scenarios/chrome/chrome-scenario-catalog.js';
import { systemClock } from '../ports/clock.js';

const repositoryRoot = resolve(process.cwd(), '..');
const defaultWorkbookPath = join(repositoryRoot, 'docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

export async function runBrowser(target: BrowserTarget, launcher: BrowserLauncher, args: readonly string[]): Promise<void> {
  const options = parseOptions(args);
  const workbookPath = process.env.E2E_WORKBOOK_PATH || defaultWorkbookPath;
  const inventory = await new ExcelChromeScenarioInventory(workbookPath, target.name).load();
  const catalog = new ChromeScenarioCatalog(inventory);
  const profileScenarios = new Set(selectSuiteProfile(inventory, options.profile).map((scenario) => scenario.scenarioId));
  const selected = catalog.all().filter((scenario) =>
    profileScenarios.has(scenario.inventory.scenarioId)
    && (options.block === undefined || scenario.inventory.block === options.block)
    && (options.scenario === undefined || scenario.inventory.scenarioId === options.scenario)
  );
  if (!options.resume && selected.length === 0) throw new Error(`La selecció ${target.name} no conté escenaris.`);
  const ci = process.env.CI === 'true';
  const environment = ci ? loadCiEnvironment() : await loadPilotEnvironment(join(process.cwd(), '.env.e2e.local'));
  const runsRoot = process.env.E2E_RUNS_ROOT || join(process.cwd(), '.runs');
  const runsDirectory = target.runsDirectoryName === undefined ? runsRoot : join(runsRoot, target.runsDirectoryName);
  await mkdir(runsDirectory, { recursive: true });
  const store = new JsonRunStateStore(runsDirectory);
  const journal = new JsonlExecutionJournal(runsDirectory);
  const executor = new ChromePlaywrightExecutor(environment, runsDirectory, options.headed, catalog, launcher, target.name);
  const orchestrator = new RunOrchestrator(store, journal, new ExecuteScenario(store, journal, executor, systemClock), new ResumeRun(store, journal, systemClock), new FinalizeRun(store, journal, systemClock), systemClock);
  const deferred = process.env.E2E_DEFER_EXCEL_SYNC === 'true';
  const excel = deferred ? new DeferredExcelSyncQueue(runsDirectory) : new ExcelWorkbookSync(workbookPath);
  const reporter = new ChromeExcelReporter(excel, executor, catalog, commit(), target, ci ? 'CI' : 'LOCAL', ci ? 'CI' : 'LOCAL');
  const finalState = options.resume
    ? await orchestrator.resume(required(options.runId, '--run-id'), showProgress)
    : await orchestrator.run(await new StartRun(store, systemClock).execute(catalog.definitions(selected)), showProgress);
  await reporter.synchronize(await journal.list(finalState.runId));
  console.log(`${target.name} run ${finalState.runId} finished: ${finalState.status}.`);
  const records = await journal.list(finalState.runId);
  if (process.env.E2E_FAIL_ON_NON_PASS === 'true' && (finalState.status !== 'completed' || records.some((record) => record.outcome === 'failed' || record.outcome === 'blocked'))) {
    throw new Error(`Gate ${target.name} no superat: hi ha FAIL, BLOCKED o una interrupció.`);
  }
}

function parseOptions(args: readonly string[]): { readonly block?: ChromeBlock; readonly scenario?: string; readonly profile: ReturnType<typeof parseSuiteProfile>; readonly headed: boolean; readonly resume: boolean; readonly runId?: string } {
  const read = (name: string) => args.find((arg) => arg.startsWith(`${name}=`))?.slice(name.length + 1);
  return { block: read('--block') as ChromeBlock | undefined, scenario: read('--scenario'), profile: parseSuiteProfile(read('--profile')), headed: args.includes('--headed'), resume: args.includes('--resume'), runId: read('--run-id') };
}

function required(value: string | undefined, option: string): string { if (!value) throw new Error(`Falta ${option}.`); return value; }
function commit(): string { try { return execFileSync('git', ['rev-parse', 'HEAD'], { cwd: repositoryRoot, encoding: 'utf8' }).trim(); } catch { return 'unknown'; } }
function showProgress(value: RunProgress): void {
  const percent = value.totalScenarios === 0 ? 100 : Math.round(value.completedScenarios * 100 / value.totalScenarios);
  const eta = value.eta === undefined ? '-' : formatDuration(value.eta.estimatedRemainingMs);
  console.log(`${value.currentScenarioId ?? '-'} | ${value.completedScenarios}/${value.totalScenarios} ${percent}% | PASS ${value.passed} FAIL ${value.failed} BLOCKED ${value.blocked} SKIP ${value.skipped} RETRIES ${value.retries} | elapsed ${formatDuration(value.elapsedMs)} ETA ${eta}`);
}
function formatDuration(ms: number): string { const seconds = Math.round(ms / 1000); return `${Math.floor(seconds / 60)}m${String(seconds % 60).padStart(2, '0')}s`; }
