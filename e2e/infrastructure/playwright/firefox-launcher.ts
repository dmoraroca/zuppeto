import { firefox, type Browser } from '@playwright/test';
import type { BrowserLauncher } from './browser-launcher.js';

export class FirefoxLauncher implements BrowserLauncher {
  public launch(headed: boolean): Promise<Browser> {
    return firefox.launch({ headless: !headed });
  }
}
