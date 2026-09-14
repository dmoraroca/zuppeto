import { resolve } from 'node:path';
import type { E2EAccount } from '../domain/e2e-role.js';
import { LocalE2EAccountWriter } from '../infrastructure/config/local-e2e-account-writer.js';
import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import { PostgresE2EAccountProvisioner } from '../infrastructure/provisioning/postgres-e2e-account-provisioner.js';

const repositoryRoot = resolve(process.cwd(), '..');
const environmentPath = resolve(process.cwd(), '.env.e2e.local');

async function main(): Promise<void> {
  const environment = await loadPilotEnvironment(environmentPath);
  const provisioner = new PostgresE2EAccountProvisioner(repositoryRoot);
  const writer = new LocalE2EAccountWriter(environmentPath);
  const accounts: E2EAccount[] = [];
  if (!environment.accounts.has('ADMIN')) accounts.push(provisioner.provisionAdmin());
  if (!environment.accounts.has('VIEWER')) accounts.push(provisioner.rotateViewer());
  for (const account of accounts) {
    await verify(environment.apiBaseUrl, account);
    await writer.save(account);
  }
  const configured = await loadPilotEnvironment(environmentPath);
  console.log(`Comptes E2E configurats: ${[...configured.accounts.keys()].sort().join(', ')}. Cap secret mostrat.`);
}

async function verify(apiBaseUrl: string, account: E2EAccount): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: account.email, password: account.password })
  });
  if (!response.ok) throw new Error(`No s'ha pogut verificar el compte E2E ${account.role} (${response.status}).`);
  const session = await response.json() as { readonly user?: { readonly role?: string } };
  if (session.user?.role?.toUpperCase() !== account.role) throw new Error(`El compte E2E ${account.role} no retorna el rol esperat.`);
}

void main().catch((error) => { console.error(error instanceof Error ? error.message : 'Error desconegut de provisioning E2E.'); process.exitCode = 1; });
