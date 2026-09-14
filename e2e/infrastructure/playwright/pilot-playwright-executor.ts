import { join } from 'node:path';
import type { Browser, BrowserContext, Page } from '@playwright/test';
import { CleanupCoordinator, type CleanupIssue } from '../../application/cleanup/cleanup-coordinator.js';
import { RoleSessionFixture } from '../../application/fixtures/role-session-fixture.js';
import { TestDataIdentityFactory } from '../../domain/test-data-identity.js';
import type { ScenarioDefinition } from '../../domain/scenario.js';
import type { ScenarioExecutionResult, ScenarioExecutor } from '../../ports/scenario-executor.js';
import type { PilotEnvironment } from '../config/local-e2e-environment.js';
import { PilotArtifactWriter } from './pilot-artifact-writer.js';
import { PilotChromeLauncher } from './pilot-chrome-launcher.js';
import { PilotDiagnosticsCollector, type PilotDiagnostics } from './pilot-diagnostics.js';
import { redact } from './pilot-redactor.js';
import { findPilotScenario } from '../../scenarios/pilot/pilot-scenarios.js';
import { AuthenticationApiAdapter } from '../api/authentication-api-adapter.js';
import { FavoriteApiAdapter } from '../api/favorite-api-adapter.js';
import { FavoriteFactory } from '../api/favorite-factory.js';
import { PlaceApiAdapter } from '../api/place-api-adapter.js';
import { PlaywrightPageApiTransport } from './playwright-page-api-transport.js';
import { PlaywrightSessionDriver } from './playwright-session-driver.js';

export interface PilotEvidence {
  readonly javascriptErrors: string;
  readonly networkErrors: string;
  readonly cleanupErrors: string;
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
    return this.evidence.get(key(scenarioId, attempt)) ?? { javascriptErrors: '', networkErrors: '', cleanupErrors: '', evidencePaths: '' };
  }

  public version(): string { return this.browserVersion; }

  public async execute(scenario: ScenarioDefinition, attempt: number, runId: string): Promise<ScenarioExecutionResult> {
    const pilot = findPilotScenario(scenario.id.value);
    if (pilot === undefined) return { outcome: 'blocked', message: 'L’escenari pilot no està registrat.' };
    let page: Page | undefined;
    let browser: Browser | undefined;
    let context: BrowserContext | undefined;
    let cleanup: CleanupCoordinator | undefined;
    let cleanupIssues: readonly CleanupIssue[] = [];
    let diagnostics: PilotDiagnostics = { javascriptErrors: '', networkErrors: '', finalUrl: '' };
    try {
      browser = await this.launcher.launch(this.headed);
      this.browserVersion = browser.version();
      context = await browser.newContext();
      page = await context.newPage();
      const collector = new PilotDiagnosticsCollector(page);
      collector.start();
      if (this.interruptOnceScenarioId === scenario.id.value && attempt === 1) {
        diagnostics = collector.snapshot();
        this.evidence.set(key(scenario.id.value, attempt), { javascriptErrors: diagnostics.javascriptErrors, networkErrors: diagnostics.networkErrors, cleanupErrors: '', evidencePaths: '' });
        return { outcome: 'interrupted', message: 'Interrupció controlada mentre l’escenari estava en estat running.' };
      }
      await page.goto(this.environment.webBaseUrl + '/login');
      const transport = new PlaywrightPageApiTransport(page, this.environment.webBaseUrl, this.environment.apiBaseUrl);
      const authentication = new AuthenticationApiAdapter(transport);
      const sessionFixture = new RoleSessionFixture(this.environment.accounts, authentication, new PlaywrightSessionDriver(context, page));
      const session = await sessionFixture.prepare(pilot.role);
      cleanup = new CleanupCoordinator();
      const identity = new TestDataIdentityFactory().create(pilot.key.testCode, pilot.role, runId, `a${String(attempt).padStart(2, '0')}`);
      await pilot.execute({
        page, session, cleanup, identity,
        favoriteFactory: new FavoriteFactory(new FavoriteApiAdapter(transport), new PlaceApiAdapter(transport))
      }, this.environment);
      diagnostics = collector.snapshot();
      cleanupIssues = await cleanup.run();
      this.evidence.set(key(scenario.id.value, attempt), {
        javascriptErrors: diagnostics.javascriptErrors, networkErrors: diagnostics.networkErrors,
        cleanupErrors: formatCleanup(cleanupIssues), evidencePaths: ''
      });
      if (cleanupIssues.length > 0) return { outcome: 'blocked', message: `CLEANUP: ${cleanupIssues.length} incidència(es).` };
      if (diagnostics.javascriptErrors !== '' || diagnostics.networkErrors !== '') {
        return { outcome: 'failed', message: 'S’han detectat errors JavaScript o HTTP durant l’escenari.' };
      }
      return { outcome: 'passed', message: 'Escenari pilot completat correctament.' };
    } catch (error) {
      if (cleanup !== undefined) cleanupIssues = await cleanup.run();
      const message = redact(error instanceof Error ? error.message : 'Error desconegut del navegador.');
      const writer = new PilotArtifactWriter(join(this.artifactsRoot, 'artifacts'));
      const path = await writer.writeFailure(`${runId}--${scenario.id.value}--a${String(attempt).padStart(2, '0')}`, page, diagnostics, message).catch(() => '');
      this.evidence.set(key(scenario.id.value, attempt), {
        javascriptErrors: diagnostics.javascriptErrors, networkErrors: diagnostics.networkErrors,
        cleanupErrors: formatCleanup(cleanupIssues), evidencePaths: path
      });
      return { outcome: isLaunchFailure(message) ? 'blocked' : 'failed', message };
    } finally {
      await context?.close().catch(() => undefined);
      await browser?.close().catch(() => undefined);
    }
  }
}

function formatCleanup(issues: readonly CleanupIssue[]): string {
  return issues.map((issue) => `${issue.category} ${issue.resource}: ${redact(issue.message)}`).join('\n');
}

function key(scenarioId: string, attempt: number): string { return scenarioId + '--' + attempt; }
function isLaunchFailure(message: string): boolean {
  return /Chrome Flatpak|executable|browser has been closed|Failed to launch/i.test(message);
}
