import type { ConsoleMessage, Page, Request, Response, TestInfo } from '@playwright/test';

export type BrowserDiagnosticKind =
  | 'javascript'
  | 'console.error'
  | 'request.failed'
  | 'response.5xx';

export interface BrowserDiagnostic {
  kind: BrowserDiagnosticKind;
  message: string;
  url: string;
  timestamp: string;
}

const IGNORED_BROWSER_EXTENSION_ERROR =
  /runtime\.lastError|message port closed before a response was received/i;

/** Recull errors reals del navegador sense barrejar-los amb la lògica funcional del test. */
export class BrowserDiagnostics {
  private readonly diagnostics: BrowserDiagnostic[] = [];

  private readonly onPageError = (error: Error): void => {
    if (!IGNORED_BROWSER_EXTENSION_ERROR.test(error.message)) {
      this.record('javascript', error.stack ?? error.message, this.page.url());
    }
  };

  private readonly onConsole = (message: ConsoleMessage): void => {
    if (message.type() !== 'error' || IGNORED_BROWSER_EXTENSION_ERROR.test(message.text())) {
      return;
    }

    const location = message.location();
    this.record('console.error', message.text(), location.url || this.page.url());
  };

  private readonly onRequestFailed = (request: Request): void => {
    if (!['document', 'script', 'stylesheet', 'xhr', 'fetch'].includes(request.resourceType())) {
      return;
    }

    this.record(
      'request.failed',
      request.failure()?.errorText ?? 'Petició fallida sense detall',
      request.url()
    );
  };

  private readonly onResponse = (response: Response): void => {
    if (response.status() >= 500) {
      this.record('response.5xx', `HTTP ${response.status()} ${response.statusText()}`, response.url());
    }
  };

  constructor(private readonly page: Page) {}

  start(): void {
    this.page.on('pageerror', this.onPageError);
    this.page.on('console', this.onConsole);
    this.page.on('requestfailed', this.onRequestFailed);
    this.page.on('response', this.onResponse);
  }

  stop(): void {
    this.page.off('pageerror', this.onPageError);
    this.page.off('console', this.onConsole);
    this.page.off('requestfailed', this.onRequestFailed);
    this.page.off('response', this.onResponse);
  }

  async attachWhenPresent(testInfo: TestInfo): Promise<void> {
    if (this.diagnostics.length === 0) {
      return;
    }

    await testInfo.attach('browser-diagnostics', {
      body: Buffer.from(JSON.stringify(this.diagnostics, null, 2), 'utf8'),
      contentType: 'application/json'
    });
  }

  errors(): readonly BrowserDiagnostic[] {
    return this.diagnostics;
  }

  private record(kind: BrowserDiagnosticKind, message: string, url: string): void {
    this.diagnostics.push({
      kind,
      message,
      url,
      timestamp: new Date().toISOString()
    });
  }
}
