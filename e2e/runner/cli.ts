import { join } from 'node:path';
import { ExecuteScenario } from '../application/execute-scenario.js';
import { FinalizeRun } from '../application/finalize-run.js';
import { RunOrchestrator } from '../application/run-orchestrator.js';
import { ResumeRun } from '../application/resume-run.js';
import { StartRun } from '../application/start-run.js';
import { JsonlExecutionJournal } from '../infrastructure/persistence/jsonl-execution-journal.js';
import { JsonRunStateStore } from '../infrastructure/persistence/json-run-state-store.js';
import { createSimulatedScenarioCatalog } from '../infrastructure/simulation/simulated-scenario-catalog.js';
import { SimulatedScenarioExecutor } from '../infrastructure/simulation/simulated-scenario-executor.js';
import { systemClock } from '../ports/clock.js';

const runsDirectory = join(process.cwd(), '.runs');
const store = new JsonRunStateStore(runsDirectory);
const journal = new JsonlExecutionJournal(runsDirectory);
const executeScenario = new ExecuteScenario(store, journal, new SimulatedScenarioExecutor(), systemClock);
const finalizeRun = new FinalizeRun(store, journal, systemClock);
const orchestrator = new RunOrchestrator(store, journal, executeScenario, new ResumeRun(store, journal, systemClock), finalizeRun, systemClock);
const catalog = createSimulatedScenarioCatalog();
async function main(): Promise<void> {
  const [command, runId] = process.argv.slice(2);
  if (command === 'simulate') {
    const run = await new StartRun(store, systemClock).execute(catalog);
    const finalState = await orchestrator.run(run, showProgress);
    console.log('Simulated run ' + finalState.runId + ' finished: ' + finalState.status + '.');
  } else if (command === 'resume' && runId !== undefined) {
    const finalState = await orchestrator.resume(runId, showProgress);
    console.log('Simulated run ' + finalState.runId + ' finished: ' + finalState.status + '.');
  } else {
    console.error('Usage: cli simulate | cli resume <runId>');
    process.exitCode = 1;
  }
}

void main();

function showProgress(progress: {
  completedScenarios: number; totalScenarios: number; passed: number; failed: number; blocked: number; interrupted: number;
  currentScenarioId?: string; eta?: { estimatedRemainingMs: number };
}): void {
  const eta = progress.eta === undefined ? 'ETA pending sample' : 'ETA ' + progress.eta.estimatedRemainingMs + 'ms';
  console.log(
    progress.completedScenarios + '/' + progress.totalScenarios +
    ' PASS ' + progress.passed + ' FAIL ' + progress.failed + ' BLOCKED ' + progress.blocked +
    ' INTERRUPTED ' + progress.interrupted + ' current ' + (progress.currentScenarioId ?? '-') + ' ' + eta
  );
}
