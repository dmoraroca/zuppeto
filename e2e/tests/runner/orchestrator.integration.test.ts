import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { join } from 'node:path';
import test from 'node:test';
import { ExecuteScenario } from '../../application/execute-scenario.js';
import { FinalizeRun } from '../../application/finalize-run.js';
import { RunOrchestrator } from '../../application/run-orchestrator.js';
import { ResumeRun } from '../../application/resume-run.js';
import { StartRun } from '../../application/start-run.js';
import { ScenarioId } from '../../domain/identifiers.js';
import { markScenarioRunning } from '../../domain/run-state.js';
import { JsonlExecutionJournal } from '../../infrastructure/persistence/jsonl-execution-journal.js';
import { JsonRunStateStore } from '../../infrastructure/persistence/json-run-state-store.js';
import { createSimulatedScenarioCatalog } from '../../infrastructure/simulation/simulated-scenario-catalog.js';
import { SimulatedScenarioExecutor } from '../../infrastructure/simulation/simulated-scenario-executor.js';
import { withTemporaryDirectory, DeterministicClock } from './test-support.js';

function createServices(directory: string, clock: DeterministicClock) {
  const store = new JsonRunStateStore(directory);
  const journal = new JsonlExecutionJournal(directory);
  const executeScenario = new ExecuteScenario(store, journal, new SimulatedScenarioExecutor(), clock);
  const resumeRun = new ResumeRun(store, journal, clock);
  const finalizeRun = new FinalizeRun(store, journal, clock);
  return {
    store, journal, resumeRun,
    orchestrator: new RunOrchestrator(store, journal, executeScenario, resumeRun, finalizeRun, clock)
  };
}

test('simulated run continues after FAIL and BLOCKED, interrupts, resumes and produces a coherent summary', async () => {
  await withTemporaryDirectory(async (directory) => {
    const clock = new DeterministicClock([
      '2026-09-13T10:00:00.000Z', '2026-09-13T10:00:00.010Z', '2026-09-13T10:00:00.020Z',
      '2026-09-13T10:00:00.030Z', '2026-09-13T10:00:00.040Z', '2026-09-13T10:00:00.050Z',
      '2026-09-13T10:00:00.060Z', '2026-09-13T10:00:00.070Z', '2026-09-13T10:00:00.080Z',
      '2026-09-13T10:00:00.090Z', '2026-09-13T10:00:00.100Z', '2026-09-13T10:00:00.110Z',
      '2026-09-13T10:00:00.120Z', '2026-09-13T10:00:00.130Z'
    ]);
    const { store, journal, orchestrator } = createServices(directory, clock);
    const catalog = createSimulatedScenarioCatalog();
    const initial = await new StartRun(store, clock, () => ({ value: 'run-integration' } as never)).execute(catalog);
    const interrupted = await orchestrator.run(initial);
    assert.equal(interrupted.status, 'interrupted');
    assert.deepEqual((await journal.list(initial.runId)).map((record) => record.outcome), ['passed', 'failed', 'blocked', 'interrupted']);

    const completed = await orchestrator.resume(initial.runId);
    const records = await journal.list(initial.runId);
    assert.equal(completed.status, 'completed');
    assert.deepEqual(records.map((record) => record.outcome), ['passed', 'failed', 'blocked', 'interrupted', 'passed']);
    assert.equal(new Set(records.map((record) => record.executionId)).size, records.length);
    assert.equal(records.filter((record) => record.scenarioId === 'SIM-INTERRUPT-THEN-PASS').length, 2);

    const summary = JSON.parse(await readFile(join(directory, initial.runId, 'summary.json'), 'utf8')) as {
      counts: Record<string, number>; totalAttempts: number; status: string;
    };
    assert.equal(summary.status, 'completed');
    assert.equal(summary.totalAttempts, 5);
    assert.deepEqual(summary.counts, { passed: 2, failed: 1, blocked: 1, interrupted: 1 });
  });
});

test('resume records a scenario left running exactly once before retrying it', async () => {
  await withTemporaryDirectory(async (directory) => {
    const clock = new DeterministicClock([
      '2026-09-13T11:00:00.000Z', '2026-09-13T11:00:00.010Z', '2026-09-13T11:00:00.020Z',
      '2026-09-13T11:00:00.030Z', '2026-09-13T11:00:00.040Z'
    ]);
    const { store, journal, resumeRun } = createServices(directory, clock);
    const catalog = [createSimulatedScenarioCatalog()[0]];
    const initial = await new StartRun(store, clock, () => ({ value: 'run-crash' } as never)).execute(catalog);
    const crashed = markScenarioRunning(initial, ScenarioId.from('SIM-PASS'), clock.now());
    await store.save(crashed);

    const resumed = await resumeRun.execute(initial.runId);
    const records = await journal.list(initial.runId);
    assert.equal(resumed.scenarios[0].status, 'interrupted');
    assert.equal(records.length, 1);
    assert.equal(records[0].outcome, 'interrupted');

    await resumeRun.execute(initial.runId);
    assert.equal((await journal.list(initial.runId)).length, 1);
  });
});
