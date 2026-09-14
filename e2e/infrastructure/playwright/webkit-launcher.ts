import { webkit, type Browser } from '@playwright/test';
import type { BrowserLauncher } from './browser-launcher.js';

export class WebKitLauncher implements BrowserLauncher {
  public launch(headed: boolean): Promise<Browser> {
    return webkit.launch({ headless: !headed });
  }
}
