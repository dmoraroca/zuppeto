import { webkitTarget } from '../domain/browser-target.js';
import { WebKitLauncher } from '../infrastructure/playwright/webkit-launcher.js';
import { runBrowser } from './browser-cli.js';

void runBrowser(webkitTarget, new WebKitLauncher(), process.argv.slice(2)).catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut de WebKit.');
  process.exitCode = 1;
});
