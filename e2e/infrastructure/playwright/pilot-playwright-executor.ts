import { join } from 'node:path';
import type { Page } from '@playwright/test';
import type { ScenarioDefinition } from '../../domain/scenario.js';
import type { ScenarioExecutionResult, ScenarioExecutor } from '../../ports/scenario-executor.js';
import type { PilotEnvironment } from '../config/local-e2e-environment.js';
import { PilotArtifactWriter } from './pilot-artifact-writer.js';
import { PilotChromeLauncher } from './pilot-chrome-launcher.js';
import { PilotDiagnosticsCollector, type PilotDiagnostics } from './pilot-diagnostics.js';
import { redact } from './pilot-redactor.js';
import { findPilotScenario } from '../../scenarios/pilot/pilot-scenarios.js';

export interface PilotEvidence {
  readonly javascriptErrors: string;
  readonly networkErrors: string;
  readonly evidencePaths: string;
}

export class PilotPlaywrightExecutor implements ScenarioExecutor {
  private readonly evidence = new Map<string, PilotEvidence>();
  private browserVersion = 'unknown';

  public constructor(
    private readonly environment: PilotEnvironment,
    private readonly artifactsRoot: string,
    private readonly headed: boolean,
    private readonly interruptOnceScenarioId?: string,
    private readonly launcher = new PilotChromeLauncher()
  ) {}

  public evidenceFor(scenarioId: string, attempt: number): PilotEvidence {
    return this.evidence.get(key(scenarioId, attempt)) ?? { javascriptErrors: '', networkErrors: '', evidencePaths: '' };
  }

  public version(): string { return this.browserVersion; }

  public async execute(scenario: ScenarioDefinition, attempt: number): Promise<ScenarioExecutionResult> {
    const pilot = findPilotScenario(scenario.id.value);
    if (pilot === undefined) return { outcome: 'blocked', message: 'L’escenari pilot no està registrat.' };
    let page: Page | undefined;
    let diagnostics: PilotDiagnostics = { javascriptErrors: '', networkErrors: '', finalUrl: '' };
    try {
      const browser = await this.launcher.launch(this.headed);
      this.browserVersion = browser.version();
      const context = await browser.newContext();
      page = await context.newPage();
      const collector = new PilotDiagnosticsCollector(page);
      collector.start();
      if (this.interruptOnceScenarioId === scenario.id.value && attempt === 1) {
        diagnostics = collector.snapshot();
        this.evidence.set(key(scenario.id.value, attempt), { javascriptErrors: diagnostics.javascriptErrors, networkErrors: diagnostics.networkErrors, evidencePaths: '' });
        await context.close(); await browser.close();
        return { outcome: 'interrupted', message: 'Interrupció controlada mentre l’escenari estava en estat running.' };
      }
      await pilot.execute(page, this.environment);
      diagnostics = collector.snapshot();
      this.evidence.set(key(scenario.id.value, attempt), { javascriptErrors: diagnostics.javascriptErrors, networkErrors: diagnostics.networkErrors, evidencePaths: '' });
      await context.close(); await browser.close();
      if (diagnostics.javascriptErrors !== '' || diagnostics.networkErrors !== '') {
        return { outcome: 'failed', message: 'S’han detectat errors JavaScript o HTTP durant l’escenari.' };
      }
      return { outcome: 'passed', message: 'Escenari pilot completat correctament.' };
    } catch (error) {
      const message = redact(error instanceof Error ? error.message : 'Error desconegut del navegador.');
      const writer = new PilotArtifactWriter(join(this.artifactsRoot, 'artifacts'));
      const path = await writer.writeFailure(scenario.id.value + '--a' + String(attempt).padStart(2, '0'), page, diagnostics, message).catch(() => '');
      this.evidence.set(key(scenario.id.value, attempt), { javascriptErrors: diagnostics.javascriptErrors, networkErrors: diagnostics.networkErrors, evidencePaths: path });
      return { outcome: isLaunchFailure(message) ? 'blocked' : 'failed', message };
    }
  }
}

function key(scenarioId: string, attempt: number): string { return scenarioId + '--' + attempt; }
function isLaunchFailure(message: string): boolean {
  return /Chrome Flatpak|executable|browser has been closed|Failed to launch/i.test(message);
}
