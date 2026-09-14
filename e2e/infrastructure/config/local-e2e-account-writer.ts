import { chmod, readFile, rename, writeFile } from 'node:fs/promises';
import type { E2EAccount } from '../../domain/e2e-role.js';

export class LocalE2EAccountWriter {
  public constructor(private readonly path: string) {}

  public async save(account: E2EAccount): Promise<void> {
    const source = await readFile(this.path, 'utf8');
    const values = new Map<string, string>([
      [`E2E_${account.role}_EMAIL`, account.email],
      [`E2E_${account.role}_PASSWORD`, account.password]
    ]);
    const seen = new Set<string>();
    const lines = source.split(/\r?\n/).map((line) => {
      const separator = line.indexOf('=');
      if (separator <= 0 || line.trimStart().startsWith('#')) return line;
      const key = line.slice(0, separator).trim();
      const replacement = values.get(key);
      if (replacement === undefined) return line;
      seen.add(key);
      return `${key}=${replacement}`;
    });
    for (const [key, value] of values) if (!seen.has(key)) lines.push(`${key}=${value}`);
    const temporary = `${this.path}.tmp`;
    await writeFile(temporary, lines.join('\n'), { encoding: 'utf8', mode: 0o600 });
    await chmod(temporary, 0o600);
    await rename(temporary, this.path);
  }
}
