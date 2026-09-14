import { readFile, stat } from 'node:fs/promises';
import type { E2EAccount, E2ERole, AuthenticatedRole } from '../../domain/e2e-role.js';

export interface PilotEnvironment {
  readonly webBaseUrl: string;
  readonly apiBaseUrl: string;
  readonly accounts: ReadonlyMap<E2ERole, E2EAccount>;
}

export async function loadPilotEnvironment(path: string): Promise<PilotEnvironment> {
  const metadata = await stat(path);
  if ((metadata.mode & 0o077) !== 0) throw new Error('El fitxer local E2E ha de tenir permisos 600.');
  const source = await readFile(path, 'utf8');
  const values = new Map<string, string>();
  for (const line of source.split(/\r?\n/)) {
    const separator = line.indexOf('=');
    if (separator <= 0 || line.trimStart().startsWith('#')) continue;
    values.set(line.slice(0, separator).trim(), line.slice(separator + 1));
  }
  return loadEnvironment(values, 'local');
}

export function loadCiEnvironment(source: NodeJS.ProcessEnv = process.env): PilotEnvironment {
  const values = new Map<string, string>();
  for (const [key, value] of Object.entries(source)) if (value !== undefined) values.set(key, value);
  return loadEnvironment(values, 'CI');
}

function loadEnvironment(values: ReadonlyMap<string, string>, source: string): PilotEnvironment {
  const accounts = new Map<E2ERole, E2EAccount>();
  addAccount(accounts, values, 'USER');
  addAccount(accounts, values, 'ADMIN');
  addAccount(accounts, values, 'DEVELOPER');
  addAccount(accounts, values, 'VIEWER');
  return { webBaseUrl: required(values, 'E2E_BASE_URL', source), apiBaseUrl: required(values, 'E2E_API_URL', source), accounts };
}

function addAccount(accounts: Map<E2ERole, E2EAccount>, values: ReadonlyMap<string, string>, role: AuthenticatedRole): void {
  const email = values.get(`E2E_${role}_EMAIL`);
  const password = values.get(`E2E_${role}_PASSWORD`);
  if (email === undefined && password === undefined) return;
  if (!email || !password) throw new Error(`La configuració local del rol ${role} és incompleta.`);
  accounts.set(role, { role, email, password });
}

function required(values: ReadonlyMap<string, string>, key: string, source: string): string {
  const value = values.get(key);
  if (value === undefined || value.length === 0) throw new Error(`Falta la variable ${source} requerida: ${key}`);
  return value;
}
