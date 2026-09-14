import { access } from 'node:fs/promises';
import { chromium, type Browser } from '@playwright/test';
import type { BrowserLauncher } from './browser-launcher.js';
import { FlatpakBrowserProfileCleanup, flatpakTemporaryDirectory } from './flatpak-browser-profile-cleanup.js';

const flatpakEdgeExecutable = '/var/lib/flatpak/exports/bin/com.microsoft.Edge';

export class EdgeLauncher implements BrowserLauncher {
  public async launch(headed: boolean): Promise<Browser> {
    if (process.env.CI === 'true' || process.platform === 'win32') {
      return chromium.launch({ channel: 'msedge', headless: !headed, args: ['--no-first-run', '--no-default-browser-check'] });
    }
    try {
      await access(flatpakEdgeExecutable);
    } catch {
      throw new Error('Microsoft Edge Flatpak no està disponible al camí esperat.');
    }
    const cleanup = new FlatpakBrowserProfileCleanup(flatpakTemporaryDirectory('com.microsoft.Edge'));
    return cleanup.launch(() => chromium.launch({
      executablePath: flatpakEdgeExecutable,
      headless: !headed,
      args: ['--no-first-run', '--no-default-browser-check']
    }));
  }
}
