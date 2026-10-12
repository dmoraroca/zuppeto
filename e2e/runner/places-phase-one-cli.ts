import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';
import { runPlacesPhaseOneAudit } from '../scenarios/phase-one/places-phase-one-audit.js';

const launcher = new PilotChromeLauncher();
void run().catch((error: unknown) => {
  console.error(error);
  process.exitCode = 1;
});

async function run(): Promise<void> {
  const browser = await launcher.launch(false);
  try {
    const result = await runPlacesPhaseOneAudit(browser, process.env.E2E_BASE_URL ?? 'http://localhost:4200');
    console.log(JSON.stringify(result));
  } finally {
    await browser.close();
  }
}
