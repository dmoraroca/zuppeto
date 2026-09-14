import type { Browser } from '@playwright/test';

export interface BrowserLauncher {
  launch(headed: boolean): Promise<Browser>;
}
