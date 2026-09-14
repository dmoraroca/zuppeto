import { access } from 'node:fs/promises';
import { chromium, type Browser } from '@playwright/test';
import type { BrowserLauncher } from './browser-launcher.js';

const flatpakChromeExecutable = '/var/lib/flatpak/exports/bin/com.google.Chrome';

export class PilotChromeLauncher implements BrowserLauncher {
  public async launch(headed: boolean): Promise<Browser> {
    try {
      await access(flatpakChromeExecutable);
    } catch {
      throw new Error('Google Chrome Flatpak no està disponible al camí esperat.');
    }
    return chromium.launch({
      executablePath: flatpakChromeExecutable,
      headless: !headed,
      args: ['--no-first-run', '--no-default-browser-check']
    });
  }
}
