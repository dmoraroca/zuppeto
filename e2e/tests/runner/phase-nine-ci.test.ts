import assert from 'node:assert/strict';
import { copyFile, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import test from 'node:test';
import type { ExcelExecution } from '../../ports/excel-sync-gateway.js';
import { parseSuiteProfile, selectSuiteProfile } from '../../domain/e2e-suite-profile.js';
import { loadCiEnvironment } from '../../infrastructure/config/local-e2e-environment.js';
import { auditCiArtifacts } from '../../infrastructure/ci/ci-artifact-auditor.js';
import { CiResultConsolidator, validateCiExecutions } from '../../infrastructure/ci/ci-result-consolidator.js';
import { DeferredExcelSyncQueue, deferredExcelQueueFile } from '../../infrastructure/ci/deferred-excel-sync-queue.js';
import { ExcelChromeScenarioInventory } from '../../infrastructure/excel/excel-chrome-scenario-inventory.js';
import { ExcelWorkbookSync } from '../../infrastructure/excel/excel-workbook-sync.js';
import { PostgresE2EAccountProvisioner } from '../../infrastructure/provisioning/postgres-e2e-account-provisioner.js';

const workbookPath = resolve(process.cwd(), '../docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx');

test('CI profiles select the shared smoke, critical and full inventories', async () => {
  const inventory = await new ExcelChromeScenarioInventory(workbookPath).load();
  assert.deepEqual(selectSuiteProfile(inventory, 'smoke').map((item) => item.testCode), ['ZUP-001', 'ZUP-073', 'ZUP-115']);
  assert.equal(selectSuiteProfile(inventory, 'critical').length, 59);
  assert.ok(selectSuiteProfile(inventory, 'critical').every((item) => item.risk === 'HIGH'));
  assert.equal(selectSuiteProfile(inventory, 'full').length, 177);
  assert.equal(parseSuiteProfile(undefined), 'full');
  assert.throws(() => parseSuiteProfile('copied-ci-suite'));
});

test('CI environment loads dedicated accounts without a local secret file', () => {
  const environment = loadCiEnvironment({
    E2E_BASE_URL: 'http://web.test', E2E_API_URL: 'http://api.test',
    E2E_USER_EMAIL: 'user.e2e@zuppeto.local', E2E_USER_PASSWORD: 'user-password-safe',
    E2E_ADMIN_EMAIL: 'admin.e2e@zuppeto.local', E2E_ADMIN_PASSWORD: 'admin-password-safe',
    E2E_DEVELOPER_EMAIL: 'developer.e2e@zuppeto.local', E2E_DEVELOPER_PASSWORD: 'developer-password-safe',
    E2E_VIEWER_EMAIL: 'viewer.e2e@zuppeto.local', E2E_VIEWER_PASSWORD: 'viewer-password-safe'
  });
  assert.equal(environment.accounts.size, 4);
  assert.equal(environment.accounts.get('ADMIN')?.email, 'admin.e2e@zuppeto.local');
});

test('deferred Excel queue is append-only and idempotent per executionId', async (context) => {
  const root = await mkdtemp(join(tmpdir(), 'zuppeto-ci-queue-'));
  context.after(async () => rm(root, { recursive: true, force: true }));
  const queue = new DeferredExcelSyncQueue(root);
  const item = execution('ci-run--ZUP-001-SENSE_SESSIO-principal--a01');
  assert.equal((await queue.synchronize(item)).executionRecorded, true);
  assert.equal((await queue.synchronize(item)).status, 'ALREADY_SYNCED');
  const lines = (await readFile(join(root, item.runId, deferredExcelQueueFile), 'utf8')).trim().split('\n');
  assert.equal(lines.length, 1);
});

test('CI consolidation rejects duplicate executionIds before touching Excel', () => {
  const item = execution('ci-run--ZUP-001-SENSE_SESSIO-principal--a01');
  assert.throws(() => validateCiExecutions([item, item]), /duplicat/);
  assert.equal(validateCiExecutions([item]).length, 1);
});

test('CI consolidation serially appends queued executions into an isolated workbook', async (context) => {
  const root = await mkdtemp(join(tmpdir(), 'zuppeto-ci-consolidate-'));
  context.after(async () => rm(root, { recursive: true, force: true }));
  const workbook = join(root, 'workbook.xlsx');
  const summary = join(root, 'consolidated-summary.json');
  await copyFile(workbookPath, workbook);
  const item = execution('ci-run--ZUP-001-SENSE_SESSIO-principal--a01');
  await new DeferredExcelSyncQueue(root).synchronize(item);
  await writeFile(join(root, item.runId, 'summary.json'), JSON.stringify({
    runId: item.runId, status: 'completed', totalScenarios: 1,
    counts: { passed: 1, failed: 0, blocked: 0, skipped: 0, interrupted: 0 },
    totalAttempts: 1, totalDurationMs: 1000, generatedAt: item.finishedAt
  }));
  const result = await new CiResultConsolidator(new ExcelWorkbookSync(workbook)).consolidate(root, summary);
  assert.equal(result.executions, 1);
  assert.equal(result.runs, 1);
  assert.match(await readFile(summary, 'utf8'), /"passed": 1/);
});

test('CI artifact audit rejects secret content and credential files', async (context) => {
  const root = await mkdtemp(join(tmpdir(), 'zuppeto-ci-artifacts-'));
  context.after(async () => rm(root, { recursive: true, force: true }));
  await writeFile(join(root, 'summary.json'), '{"status":"completed"}');
  assert.equal((await auditCiArtifacts(root, { E2E_USER_PASSWORD: 'never-publish-this' })).files, 1);
  await writeFile(join(root, 'diagnostics.json'), 'never-publish-this');
  await assert.rejects(() => auditCiArtifacts(root, { E2E_USER_PASSWORD: 'never-publish-this' }), /secret/);
  await rm(join(root, 'diagnostics.json'));
  await writeFile(join(root, '.env'), 'SAFE=false');
  await assert.rejects(() => auditCiArtifacts(root, {}), /prohibit/);
});

test('CI account provisioning only upserts exact dedicated accounts and never embeds passwords', () => {
  const statements: string[] = [];
  const provisioner = new PostgresE2EAccountProvisioner('.', (sql) => { statements.push(sql); return ''; });
  provisioner.configure({ role: 'ADMIN', email: 'admin.e2e@zuppeto.local', password: 'a-secure-ci-password' });
  assert.equal(statements.length, 1);
  assert.match(statements[0], /on conflict \(email\) do update/);
  assert.doesNotMatch(statements[0], /a-secure-ci-password/);
  assert.throws(() => provisioner.configure({ role: 'ADMIN', email: 'human@example.com', password: 'a-secure-ci-password' }));
  assert.ok(statements.every((sql) => !/delete\s+from/i.test(sql)));
});

test('CI opts into the demo place catalog without changing the CI environment', async () => {
  const compose = await readFile(resolve(process.cwd(), 'ci/docker-compose.ci.yml'), 'utf8');
  const workflow = await readFile(resolve(process.cwd(), '../.github/workflows/continuous-integration.yml'), 'utf8');
  const seeder = await readFile(resolve(process.cwd(), '../src/Backend/Infrastructure/Persistence/DevelopmentPlacesSeeder.cs'), 'utf8');
  assert.match(compose, /ZUPPETO_SEED_DEMO_PLACES:\s*["']true["']/);
  assert.match(workflow, /ASPNETCORE_ENVIRONMENT:\s*CI/);
  assert.match(seeder, /ZUPPETO_SEED_DEMO_PLACES/);
});

function execution(executionId: string): ExcelExecution {
  return {
    runId: 'ci-run', executionId,
    scenario: { testCode: 'ZUP-001', role: 'Sense sessió', browser: 'Chrome', scenarioId: 'principal' },
    variant: 'principal', engine: 'Blink', browserVersion: '1', environment: 'CI', commit: 'abc',
    startedAt: '2026-09-14T00:00:00.000Z', finishedAt: '2026-09-14T00:00:01.000Z', durationMs: 1000,
    attempt: 1, outcome: 'passed', javascriptErrors: '', networkErrors: '', message: 'PASS', evidencePaths: '', origin: 'CI'
  };
}
