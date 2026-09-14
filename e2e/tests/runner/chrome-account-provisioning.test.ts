import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { LocalE2EAccountWriter } from '../../infrastructure/config/local-e2e-account-writer.js';
import { loadPilotEnvironment } from '../../infrastructure/config/local-e2e-environment.js';
import { PostgresE2EAccountProvisioner } from '../../infrastructure/provisioning/postgres-e2e-account-provisioner.js';

test('local account writer updates exact role keys atomically without changing other accounts', async (context) => {
  const directory = await mkdtemp(join(tmpdir(), 'zuppeto-account-writer-'));
  context.after(async () => rm(directory, { recursive: true, force: true }));
  const path = join(directory, '.env.e2e.local');
  await writeFile(path, 'E2E_BASE_URL=http://web.test\nE2E_API_URL=http://api.test\nE2E_USER_EMAIL=user@e2e.local\nE2E_USER_PASSWORD=user-local', { mode: 0o600 });
  await new LocalE2EAccountWriter(path).save({ role: 'VIEWER', email: 'viewer@e2e.local', password: 'viewer-local' });
  const loaded = await loadPilotEnvironment(path);
  assert.equal(loaded.accounts.get('USER')?.email, 'user@e2e.local');
  assert.equal(loaded.accounts.get('VIEWER')?.email, 'viewer@e2e.local');
  assert.equal((await readFile(path, 'utf8')).includes('.tmp'), false);
});

test('database provisioner targets only exact dedicated E2E accounts', () => {
  const statements: string[] = [];
  const provisioner = new PostgresE2EAccountProvisioner('.', (sql) => {
    statements.push(sql);
    if (sql.startsWith('select')) return sql.includes('admin.e2e') ? '0\n' : '1\n';
    return '';
  });
  assert.equal(provisioner.provisionAdmin().role, 'ADMIN');
  assert.equal(provisioner.rotateViewer().role, 'VIEWER');
  assert.ok(statements.some((sql) => sql.startsWith('insert into users') && sql.includes("'admin.e2e@zuppeto.local'")));
  assert.ok(statements.some((sql) => sql.startsWith('update users') && sql.includes("where email='viewer.e2e@zuppeto.local'")));
  assert.ok(statements.every((sql) => !/delete\s+from/i.test(sql)));
});
