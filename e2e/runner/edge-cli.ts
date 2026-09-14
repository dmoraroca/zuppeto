import { edgeTarget } from '../domain/browser-target.js';
import { EdgeLauncher } from '../infrastructure/playwright/edge-launcher.js';
import { runBrowser } from './browser-cli.js';

void runBrowser(edgeTarget, new EdgeLauncher(), process.argv.slice(2)).catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut de Microsoft Edge.');
  process.exitCode = 1;
});
