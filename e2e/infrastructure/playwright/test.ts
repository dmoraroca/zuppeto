import { randomUUID } from 'node:crypto';
import { expect, test as base } from '@playwright/test';

import { BrowserDiagnostics } from './browser-diagnostics';

interface TestMetadata {
  correlationId: string;
  code: string;
  role: string;
  browser: string;
}

interface ObservabilityFixtures {
  observabilityMetadata: TestMetadata;
}

function metadata(testTitle: string, projectName: string): TestMetadata {
  const code = testTitle.match(/ZUP-\d{3}/i)?.[0].toUpperCase() ?? 'UNMAPPED';
  const normalizedTitle = testTitle.toUpperCase();
  const role = ['ADMIN', 'DEVELOPER', 'VIEWER', 'USER'].find((candidate) =>
    normalizedTitle.includes(candidate)
  ) ?? (normalizedTitle.includes('SENSE SESSIÓ') ? 'SENSE_SESSIO' : 'UNSPECIFIED');

  return {
    correlationId: randomUUID(),
    code,
    role,
    browser: projectName.replace(/[^a-zA-Z0-9_.-]/g, '_').slice(0, 32)
  };
}

/** Fixture comuna: correlaciona l'API i converteix els errors del navegador en errors E2E. */
export const test = base.extend<ObservabilityFixtures>({
  observabilityMetadata: async ({}, use, testInfo) => {
    await use(metadata(testInfo.titlePath.join(' '), testInfo.project.name));
  },
  context: async ({ context, observabilityMetadata }, use) => {
    await context.route('**/api/**', async (route) => {
      await route.fallback({
        headers: {
          ...route.request().headers(),
          'X-Correlation-ID': observabilityMetadata.correlationId,
          'X-Zuppeto-Test-Code': observabilityMetadata.code,
          'X-Zuppeto-Test-Role': observabilityMetadata.role,
          'X-Zuppeto-Test-Browser': observabilityMetadata.browser
        }
      });
    });

    await use(context);
  },
  request: async ({ playwright, observabilityMetadata }, use) => {
    const request = await playwright.request.newContext({
      extraHTTPHeaders: {
        'X-Correlation-ID': observabilityMetadata.correlationId,
        'X-Zuppeto-Test-Code': observabilityMetadata.code,
        'X-Zuppeto-Test-Role': observabilityMetadata.role,
        'X-Zuppeto-Test-Browser': observabilityMetadata.browser
      }
    });

    await use(request);
    await request.dispose();
  },
  page: async ({ page }, use, testInfo) => {
    const diagnostics = new BrowserDiagnostics(page);
    diagnostics.start();

    await use(page);

    diagnostics.stop();
    await diagnostics.attachWhenPresent(testInfo);
    expect(diagnostics.errors(), 'No hi ha d’haver errors JavaScript, de consola, xarxa o HTTP 5xx').toEqual([]);
  }
});

export { expect };
export type { APIRequestContext, Page } from '@playwright/test';
