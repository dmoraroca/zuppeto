import { access } from 'node:fs/promises';
import { chromium, type Browser } from '@playwright/test';
import type { BrowserLauncher } from './browser-launcher.js';
import { FlatpakBrowserProfileCleanup, flatpakTemporaryDirectory } from './flatpak-browser-profile-cleanup.js';

const flatpakChromeExecutable = '/var/lib/flatpak/exports/bin/com.google.Chrome';

export class PilotChromeLauncher implements BrowserLauncher {
  public async launch(headed: boolean): Promise<Browser> {
    if (process.env.CI === 'true') {
      return chromium.launch({ channel: 'chrome', headless: !headed, args: ['--no-first-run', '--no-default-browser-check'] });
    }
    try {
      await access(flatpakChromeExecutable);
    } catch {
      throw new Error('Google Chrome Flatpak no està disponible al camí esperat.');
    }
    const cleanup = new FlatpakBrowserProfileCleanup(flatpakTemporaryDirectory('com.google.Chrome'));
    return cleanup.launch(() => chromium.launch({
      executablePath: flatpakChromeExecutable,
      headless: !headed,
      args: ['--no-first-run', '--no-default-browser-check']
    }));
  }
}
