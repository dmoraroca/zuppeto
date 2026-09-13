import { readFile } from 'node:fs/promises';

export interface PilotEnvironment {
  readonly webBaseUrl: string;
  readonly apiBaseUrl: string;
  readonly userEmail: string;
  readonly userPassword: string;
  readonly developerEmail: string;
  readonly developerPassword: string;
}

export async function loadPilotEnvironment(path: string): Promise<PilotEnvironment> {
  const source = await readFile(path, 'utf8');
  const values = new Map<string, string>();
  for (const line of source.split(/\r?\n/)) {
    const separator = line.indexOf('=');
    if (separator <= 0 || line.trimStart().startsWith('#')) continue;
    values.set(line.slice(0, separator).trim(), line.slice(separator + 1));
  }
  return {
    webBaseUrl: required(values, 'E2E_BASE_URL'), apiBaseUrl: required(values, 'E2E_API_URL'),
    userEmail: required(values, 'E2E_USER_EMAIL'), userPassword: required(values, 'E2E_USER_PASSWORD'),
    developerEmail: required(values, 'E2E_DEVELOPER_EMAIL'), developerPassword: required(values, 'E2E_DEVELOPER_PASSWORD')
  };
}

function required(values: ReadonlyMap<string, string>, key: string): string {
  const value = values.get(key);
  if (value === undefined || value.length === 0) throw new Error('Falta la variable local requerida: ' + key);
  return value;
}
