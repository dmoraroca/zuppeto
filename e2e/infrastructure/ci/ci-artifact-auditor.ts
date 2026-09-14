import { execFileSync } from 'node:child_process';
import { readFile, readdir } from 'node:fs/promises';
import { basename, join } from 'node:path';

const prohibitedNames = /(^|\.)(env|cookies?|storage-state)(\.|$)/i;

export interface ArtifactAuditResult { readonly files: number; readonly archives: number; }

export async function auditCiArtifacts(root: string, environment: NodeJS.ProcessEnv = process.env): Promise<ArtifactAuditResult> {
  const secrets = Object.entries(environment)
    .filter(([key, value]) => /(PASSWORD|TOKEN|SECRET|API_KEY)/i.test(key) && value !== undefined && value.length >= 4)
    .map(([, value]) => Buffer.from(value!));
  const files = await listFiles(root);
  let archives = 0;
  for (const path of files) {
    if (prohibitedNames.test(basename(path))) throw new Error('Artefacte CI prohibit per nom.');
    const content = await readFile(path);
    if (containsSecret(content, secrets)) throw new Error('S’ha detectat un secret en un artefacte CI.');
    if (path.toLowerCase().endsWith('.zip')) {
      archives += 1;
      const expanded = execFileSync('unzip', ['-p', path], { maxBuffer: 100 * 1024 * 1024 });
      if (containsSecret(expanded, secrets)) throw new Error('S’ha detectat un secret dins un artefacte CI comprimit.');
    }
  }
  return { files: files.length, archives };
}

async function listFiles(root: string): Promise<string[]> {
  const result: string[] = [];
  async function visit(directory: string): Promise<void> {
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      const path = join(directory, entry.name);
      if (entry.isDirectory()) await visit(path);
      else if (entry.isFile()) result.push(path);
    }
  }
  await visit(root);
  return result;
}

function containsSecret(content: Buffer, secrets: readonly Buffer[]): boolean {
  return secrets.some((secret) => content.includes(secret));
}
