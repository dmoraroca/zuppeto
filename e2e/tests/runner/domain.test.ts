import assert from 'node:assert/strict';
import test from 'node:test';
import { ExecutionId, RunId, ScenarioId } from '../../domain/identifiers.js';
import { createRunState, markScenarioRunning, markScenarioTerminal } from '../../domain/run-state.js';

test('identifiers are stable and execution id includes run, scenario and attempt', () => {
  const run = RunId.create(new Date('2026-09-13T10:00:00.000Z'), () => '12345678-abcd');
  const scenario = ScenarioId.from('SIM-PASS');
  assert.equal(run.value, 'sim-20260913T100000000Z-12345678');
  assert.equal(ExecutionId.forAttempt(run, scenario, 2).value, run.value + '--SIM-PASS--a02');
});

test('run state allows planned to running to terminal and rejects invalid terminal transition', () => {
  const now = new Date('2026-09-13T10:00:00.000Z');
  const scenario = { id: ScenarioId.from('SIM-PASS'), label: 'pass' };
  const created = createRunState(RunId.from('run-1'), [scenario], now);
  const running = markScenarioRunning(created, scenario.id, now);
  const passed = markScenarioTerminal(running, scenario.id, 'passed', now);
  assert.equal(passed.scenarios[0].status, 'passed');
  assert.throws(() => markScenarioTerminal(passed, scenario.id, 'failed', now), /Invalid transition/);
});

test('run state freezes the selected scenario identifiers and labels at start', () => {
  const source = [{ id: ScenarioId.from('SIM-PASS'), label: 'Original' }];
  const state = createRunState(RunId.from('run-snapshot'), source, new Date('2026-09-13T10:00:00.000Z'));
  source[0].label = 'Changed after start';
  source.push({ id: ScenarioId.from('SIM-FAIL'), label: 'Added after start' });

  assert.deepEqual(state.scenarios.map((scenario) => [scenario.scenarioId, scenario.label]), [['SIM-PASS', 'Original']]);
});
