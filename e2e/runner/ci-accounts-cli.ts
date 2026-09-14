import { resolve } from 'node:path';
import { loadCiEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import { PostgresE2EAccountProvisioner } from '../infrastructure/provisioning/postgres-e2e-account-provisioner.js';

const authenticatedRoles = ['USER', 'ADMIN', 'DEVELOPER', 'VIEWER'] as const;

async function main(): Promise<void> {
  const environment = loadCiEnvironment();
  const provisioner = new PostgresE2EAccountProvisioner(resolve(process.cwd(), '..'));
  for (const role of authenticatedRoles) {
    const account = environment.accounts.get(role);
    if (account === undefined) throw new Error(`Falta el compte CI dedicat ${role}.`);
    provisioner.configure(account);
  }
  console.log('Quatre comptes E2E dedicats configurats per a CI. Cap secret mostrat.');
}

void main().catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut configurant comptes CI.');
  process.exitCode = 1;
});
