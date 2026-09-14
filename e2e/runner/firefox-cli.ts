import { firefoxTarget } from '../domain/browser-target.js';
import { FirefoxLauncher } from '../infrastructure/playwright/firefox-launcher.js';
import { runBrowser } from './browser-cli.js';

void runBrowser(firefoxTarget, new FirefoxLauncher(), process.argv.slice(2)).catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut de Firefox.');
  process.exitCode = 1;
});
