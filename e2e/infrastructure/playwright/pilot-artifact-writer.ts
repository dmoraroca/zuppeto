import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import type { Page } from '@playwright/test';
import type { PilotDiagnostics } from './pilot-diagnostics.js';

export class PilotArtifactWriter {
  public constructor(private readonly artifactsRoot: string) {}

  public async writeFailure(executionId: string, page: Page | undefined, diagnostics: PilotDiagnostics, message: string): Promise<string> {
    const directory = join(this.artifactsRoot, executionId);
    await mkdir(directory, { recursive: true });
    await writeFile(join(directory, 'diagnostics.json'), JSON.stringify({ ...diagnostics, message }, null, 2), 'utf8');
    if (page !== undefined && !page.isClosed()) await page.screenshot({ path: join(directory, 'failure.png'), fullPage: true });
    return directory;
  }
}
