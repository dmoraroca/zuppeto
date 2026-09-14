import { join } from 'node:path';
import { expect, type Browser, type BrowserContext, type Page } from '@playwright/test';
import { CleanupCoordinator, type CleanupIssue } from '../../application/cleanup/cleanup-coordinator.js';
import { RoleSessionFixture } from '../../application/fixtures/role-session-fixture.js';
import type { AuthenticatedRole, E2ERole } from '../../domain/e2e-role.js';
import type { ScenarioDefinition } from '../../domain/scenario.js';
import { TestDataIdentityFactory } from '../../domain/test-data-identity.js';
import type { ScenarioExecutionResult, ScenarioExecutor } from '../../ports/scenario-executor.js';
import type { PilotEnvironment } from '../config/local-e2e-environment.js';
import { AuthenticationApiAdapter } from '../api/authentication-api-adapter.js';
import { FavoriteApiAdapter } from '../api/favorite-api-adapter.js';
import { FavoriteFactory } from '../api/favorite-factory.js';
import { FavoriteStateFixture } from '../api/favorite-state-fixture.js';
import { ProfileStateFixture } from '../api/profile-state-fixture.js';
import { AdminRoleApiAdapter } from '../api/admin-role-api-adapter.js';
import { AdminRoleFactory } from '../api/admin-role-factory.js';
import { AdminUserApiAdapter } from '../api/admin-user-api-adapter.js';
import { AdminUserFactory } from '../api/admin-user-factory.js';
import { AdminMenuApiAdapter } from '../api/admin-menu-api-adapter.js';
import { AdminMenuFactory } from '../api/admin-menu-factory.js';
import { AdminPermissionApiAdapter } from '../api/admin-permission-api-adapter.js';
import { RolePermissionStateFixture } from '../api/role-permission-state-fixture.js';
import { AdminGeographyApiAdapter } from '../api/admin-geography-api-adapter.js';
import { AdminGeographyFactory } from '../api/admin-geography-factory.js';
import { AdminPlaceApiAdapter } from '../api/admin-place-api-adapter.js';
import { AdminPlaceFactory } from '../api/admin-place-factory.js';
import { PlaceApiAdapter } from '../api/place-api-adapter.js';
import type { ChromeScenarioCatalog } from '../../scenarios/chrome/chrome-scenario-catalog.js';
import { PilotArtifactWriter } from './pilot-artifact-writer.js';
import { PilotChromeLauncher } from './pilot-chrome-launcher.js';
import { PilotDiagnosticsCollector, type PilotDiagnostics } from './pilot-diagnostics.js';
import { redact } from './pilot-redactor.js';
import { PlaywrightPageApiTransport } from './playwright-page-api-transport.js';
import { PlaywrightSessionDriver } from './playwright-session-driver.js';
import { BrowserNotificationFactory } from './browser-notification-factory.js';
import type { BrowserLauncher } from './browser-launcher.js';

export interface ChromeEvidence { readonly javascriptErrors: string; readonly networkErrors: string; readonly cleanupErrors: string; readonly evidencePaths: string; }

export class ChromePlaywrightExecutor implements ScenarioExecutor {
  private readonly evidence = new Map<string, ChromeEvidence>();
  private browserVersion = 'unknown';
  public constructor(
    private readonly environment: PilotEnvironment,
    private readonly artifactsRoot: string,
    private readonly headed: boolean,
    private readonly catalog: ChromeScenarioCatalog,
    private readonly launcher: BrowserLauncher = new PilotChromeLauncher(),
    private readonly browserName = 'Chrome'
  ) {}

  public evidenceFor(scenarioId: string, attempt: number): ChromeEvidence {
    return this.evidence.get(`${scenarioId}--${attempt}`) ?? { javascriptErrors: '', networkErrors: '', cleanupErrors: '', evidencePaths: '' };
  }
  public version(): string { return this.browserVersion; }

  public async execute(definition: ScenarioDefinition, attempt: number, runId: string): Promise<ScenarioExecutionResult> {
    const scenario = this.catalog.find(definition.id.value);
    if (scenario === undefined) return { outcome: 'blocked', message: `Escenari ${this.browserName} absent del catàleg congelat.` };
    if (!scenario.inventory.automatable) return { outcome: 'skipped', message: scenario.inventory.exclusionReason ?? 'Escenari no automatitzable.' };
    let browser: Browser | undefined, browserContext: BrowserContext | undefined, page: Page | undefined, cleanup: CleanupCoordinator | undefined;
    let traceStarted = false;
    let diagnostics: PilotDiagnostics = { javascriptErrors: '', networkErrors: '', finalUrl: '' }, cleanupIssues: readonly CleanupIssue[] = [];
    try {
      browser = await this.launcher.launch(this.headed); this.browserVersion = browser.version();
      browserContext = await browser.newContext({ baseURL: this.environment.webBaseUrl });
      if (isTraceSafe(scenario.sessionRole, scenario.inventory.testCode)) {
        await browserContext.tracing.start({ screenshots: true, snapshots: true, sources: false });
        traceStarted = true;
      }
      page = await browserContext.newPage();
      const collector = new PilotDiagnosticsCollector(page); collector.start();
      await page.goto(this.environment.webBaseUrl + '/login');
      const transport = new PlaywrightPageApiTransport(page, this.environment.webBaseUrl, this.environment.apiBaseUrl);
      const authentication = new AuthenticationApiAdapter(transport);
      const session = await new RoleSessionFixture(this.environment.accounts, authentication, new PlaywrightSessionDriver(browserContext, page)).prepare(scenario.sessionRole);
      cleanup = new CleanupCoordinator();
      const identity = new TestDataIdentityFactory().create(scenario.inventory.testCode, scenario.inventory.role, runId, `a${String(attempt).padStart(2, '0')}`);
      const favoriteAdapter = new FavoriteApiAdapter(transport);
      const adminMenuAdapter = new AdminMenuApiAdapter(transport);
      const adminPermissionAdapter = new AdminPermissionApiAdapter(transport);
      const adminGeographyAdapter = new AdminGeographyApiAdapter(transport);
      const adminPlaceAdapter = new AdminPlaceApiAdapter(transport);
      await scenario.execute({
        page, session, cleanup, identity, transport,
        notificationFactory: new BrowserNotificationFactory(page),
        allowHttpStatus: (status, path) => collector.allowHttpStatus(status, path),
        allowConsoleError: (pattern) => collector.allowConsoleError(pattern),
        favoriteFactory: new FavoriteFactory(favoriteAdapter, new PlaceApiAdapter(transport)),
        favoriteStateFixture: new FavoriteStateFixture(favoriteAdapter),
        profileStateFixture: new ProfileStateFixture(transport),
        adminRoleFactory: new AdminRoleFactory(new AdminRoleApiAdapter(transport)),
        adminUserFactory: new AdminUserFactory(new AdminUserApiAdapter(transport)),
        adminMenuAdapter,
        adminMenuFactory: new AdminMenuFactory(adminMenuAdapter),
        adminPermissionAdapter,
        rolePermissionStateFixture: new RolePermissionStateFixture(adminPermissionAdapter),
        adminGeographyAdapter,
        adminGeographyFactory: new AdminGeographyFactory(adminGeographyAdapter),
        adminPlaceAdapter,
        adminPlaceFactory: new AdminPlaceFactory(adminPlaceAdapter),
        account: (role) => requiredAccount(this.environment, role),
        loginViaUi: async (role) => {
          const account = requiredAccount(this.environment, role);
          await browserContext!.clearCookies();
          await page!.evaluate(() => { localStorage.removeItem('zuppeto-auth-session'); sessionStorage.clear(); });
          await page!.goto(this.environment.webBaseUrl + '/login');
          await page!.getByLabel('Email').fill(account.email);
          await page!.getByLabel('Contrasenya').fill(account.password);
          await page!.getByRole('button', { name: 'Iniciar sessió' }).click();
          await expect(page!.getByLabel('Primary navigation').getByRole('link', { name: 'Inici' })).toBeVisible();
        }
      });
      diagnostics = collector.snapshot(); cleanupIssues = await cleanup.run();
      this.saveEvidence(definition.id.value, attempt, diagnostics, cleanupIssues, '');
      if (cleanupIssues.length > 0) return { outcome: 'blocked', message: `CLEANUP: ${cleanupIssues.length} incidència(es).` };
      if (diagnostics.javascriptErrors || diagnostics.networkErrors) {
        const message = 'S’han detectat errors JavaScript o HTTP durant l’escenari.';
        const executionId = `${definition.id.value}--a${String(attempt).padStart(2, '0')}`;
        const writer = new PilotArtifactWriter(join(this.artifactsRoot, 'artifacts'));
        const artifactPath = await writer.writeFailure(executionId, page, diagnostics, message);
        if (traceStarted) await browserContext.tracing.stop({ path: join(artifactPath, 'trace.zip') });
        this.saveEvidence(definition.id.value, attempt, diagnostics, cleanupIssues, artifactPath);
        return { outcome: 'failed', message };
      }
      if (traceStarted) await browserContext.tracing.stop();
      return { outcome: 'passed', message: `Escenari ${this.browserName} completat correctament.` };
    } catch (error) {
      if (cleanup !== undefined) cleanupIssues = await cleanup.run();
      const message = redact(error instanceof Error ? error.message : `Error desconegut de ${this.browserName}.`);
      const executionId = `${definition.id.value}--a${String(attempt).padStart(2, '0')}`;
      const writer = new PilotArtifactWriter(join(this.artifactsRoot, 'artifacts'));
      const artifactPath = await writer.writeFailure(executionId, page, diagnostics, message).catch(() => '');
      if (browserContext !== undefined && traceStarted) await browserContext.tracing.stop({ path: join(artifactPath || join(this.artifactsRoot, 'artifacts', executionId), 'trace.zip') }).catch(() => undefined);
      this.saveEvidence(definition.id.value, attempt, diagnostics, cleanupIssues, artifactPath);
      return { outcome: /Chrome Flatpak|Failed to launch|instance id|browser.*closed/i.test(message) ? 'blocked' : 'failed', message };
    } finally {
      await browserContext?.close().catch(() => undefined); await browser?.close().catch(() => undefined);
    }
  }

  private saveEvidence(id: string, attempt: number, diagnostics: PilotDiagnostics, cleanup: readonly CleanupIssue[], path: string): void {
    this.evidence.set(`${id}--${attempt}`, {
      javascriptErrors: diagnostics.javascriptErrors, networkErrors: diagnostics.networkErrors,
      cleanupErrors: cleanup.map((issue) => `CLEANUP ${issue.resource}: ${redact(issue.message)}`).join('\n'), evidencePaths: path
    });
  }
}

export function isTraceSafe(sessionRole: E2ERole, testCode: string): boolean {
  if (sessionRole !== 'SENSE_SESSIO') return false;
  const code = Number(testCode.slice(4));
  return ![2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13].includes(code);
}

function requiredAccount(environment: PilotEnvironment, role: AuthenticatedRole) {
  const account = environment.accounts.get(role);
  if (account === undefined) throw new Error(`Falta el compte E2E local ${role}.`);
  return account;
}
