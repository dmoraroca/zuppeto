import { chromeTarget } from '../domain/browser-target.js';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';
import { runBrowser } from './browser-cli.js';

void runBrowser(chromeTarget, new PilotChromeLauncher(), process.argv.slice(2)).catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut de Chrome.');
  process.exitCode = 1;
});
