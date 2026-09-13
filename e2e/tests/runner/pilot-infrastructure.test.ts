import assert from 'node:assert/strict';
import test from 'node:test';
import { redact, safeUrl } from '../../infrastructure/playwright/pilot-redactor.js';
import { pilotScenarios } from '../../scenarios/pilot/pilot-scenarios.js';

test('pilot catalog contains exactly the three approved scenarios with protected Chrome keys', () => {
  assert.deepEqual(pilotScenarios.map((scenario) => scenario.definition.id.value), [
    'ZUP-001-SENSE-SESSIO-principal', 'ZUP-073-USER-principal', 'ZUP-115-DEVELOPER-principal'
  ]);
  assert.ok(pilotScenarios.every((scenario) => scenario.key.browser === 'Chrome' && scenario.key.scenarioId === 'principal'));
  assert.deepEqual(pilotScenarios.map((scenario) => scenario.role), ['SENSE_SESSIO', 'USER', 'DEVELOPER']);
});

test('pilot diagnostics remove credentials and query strings before persistence', () => {
  const diagnostic = redact('Authorization: Bearer value-to-hide password=another-value');
  assert.equal(diagnostic.includes('value-to-hide'), false);
  assert.equal(diagnostic.includes('another-value'), false);
  assert.equal(safeUrl('https://example.test/path?access_token=value-to-hide'), 'https://example.test/path');
});
