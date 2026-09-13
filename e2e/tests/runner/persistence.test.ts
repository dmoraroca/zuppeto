import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import test from 'node:test';
import { createExecutionRecord } from '../../domain/execution.js';
import { RunId, ScenarioId } from '../../domain/identifiers.js';
import { createRunState } from '../../domain/run-state.js';
import { JsonlExecutionJournal } from '../../infrastructure/persistence/jsonl-execution-journal.js';
import { JsonRunStateStore } from '../../infrastructure/persistence/json-run-state-store.js';
import { withTemporaryDirectory } from './test-support.js';

test('state store writes state atomically without temporary files remaining', async () => {
  await withTemporaryDirectory(async (directory) => {
    const store = new JsonRunStateStore(directory);
    const state = createRunState(RunId.from('run-state'), [], new Date('2026-09-13T10:00:00.000Z'));
    await store.create(state);
    assert.deepEqual(await store.load('run-state'), state);
    assert.deepEqual(await readdir(join(directory, 'run-state')), ['state.json']);
  });
});

test('execution journal is append-only and idempotent per execution id', async () => {
  await withTemporaryDirectory(async (directory) => {
    const journal = new JsonlExecutionJournal(directory);
    const record = createExecutionRecord({
      runId: RunId.from('run-journal'), scenarioId: ScenarioId.from('SIM-PASS'), attempt: 1,
      outcome: 'passed', startedAt: new Date('2026-09-13T10:00:00.000Z'), finishedAt: new Date('2026-09-13T10:00:00.010Z'), message: 'pass'
    });
    assert.equal(await journal.append(record), true);
    assert.equal(await journal.append(record), false);
    assert.equal((await journal.list('run-journal')).length, 1);
    assert.equal((await readFile(join(directory, 'run-journal', 'executions.jsonl'), 'utf8')).trim().split('\n').length, 1);
  });
});
