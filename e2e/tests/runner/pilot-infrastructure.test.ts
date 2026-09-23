import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { redact, safeUrl } from '../../infrastructure/playwright/pilot-redactor.js';
import { isKnownExternalReportOnlyError, PilotDiagnosticsCollector } from '../../infrastructure/playwright/pilot-diagnostics.js';
import { pilotScenarios } from '../../scenarios/pilot/pilot-scenarios.js';
import { PilotArtifactWriter } from '../../infrastructure/playwright/pilot-artifact-writer.js';

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

test('diagnostics can allow an exact expected HTTP status and path', () => {
  const responseListeners: ((response: { status(): number; url(): string }) => void)[] = [];
  const page = { on(event: string, listener: (value: never) => void) { if (event === 'response') responseListeners.push(listener as never); }, url() { return 'https://web.test/login'; } };
  const collector = new PilotDiagnosticsCollector(page as never);
  collector.allowHttpStatus(401, '/api/auth/login');
  collector.start();
  responseListeners.forEach((listener) => listener({ status: () => 401, url: () => 'https://api.test/api/auth/login?token=hidden' }));
  assert.equal(collector.snapshot().networkErrors, '');
});

test('diagnostics ignore only the known Google report-only framing warning', () => {
  assert.equal(isKnownExternalReportOnlyError(
    "Framing 'https://accounts.google.com/' violates the following report-only Content Security Policy directive: \"frame-ancestors 'self'\"."
  ), true);
  assert.equal(isKnownExternalReportOnlyError("Refused to frame 'https://accounts.google.com/' because of Content Security Policy."), false);
  assert.equal(isKnownExternalReportOnlyError('Application TypeError'), false);
});

test('failure evidence is isolated by the complete executionId', async (context) => {
  const root = await mkdtemp(join(tmpdir(), 'zuppeto-evidence-'));
  context.after(async () => rm(root, { recursive: true, force: true }));
  const writer = new PilotArtifactWriter(root);
  const diagnostics = { javascriptErrors: '', networkErrors: '', finalUrl: 'https://web.test/' };

  const first = await writer.writeFailure('run-one--ZUP-001-SENSE_SESSIO-principal--a01', undefined, diagnostics, 'first');
  const second = await writer.writeFailure('run-two--ZUP-001-SENSE_SESSIO-principal--a01', undefined, diagnostics, 'second');

  assert.notEqual(first, second);
  assert.match(await readFile(join(first, 'diagnostics.json'), 'utf8'), /first/);
  assert.match(await readFile(join(second, 'diagnostics.json'), 'utf8'), /second/);
});
