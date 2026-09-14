import { access } from 'node:fs/promises';
import { chromium, type Browser } from '@playwright/test';
import type { BrowserLauncher } from './browser-launcher.js';

const flatpakEdgeExecutable = '/var/lib/flatpak/exports/bin/com.microsoft.Edge';

export class EdgeLauncher implements BrowserLauncher {
  public async launch(headed: boolean): Promise<Browser> {
    try {
      await access(flatpakEdgeExecutable);
    } catch {
      throw new Error('Microsoft Edge Flatpak no està disponible al camí esperat.');
    }
    return chromium.launch({
      executablePath: flatpakEdgeExecutable,
      headless: !headed,
      args: ['--no-first-run', '--no-default-browser-check']
    });
  }
}
