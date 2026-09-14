import type { Page } from '@playwright/test';
import { redact, safeUrl } from './pilot-redactor.js';

export interface PilotDiagnostics {
  readonly javascriptErrors: string;
  readonly networkErrors: string;
  readonly finalUrl: string;
}

export class PilotDiagnosticsCollector {
  private readonly javascript: string[] = [];
  private readonly network: string[] = [];
  private readonly allowedHttpResponses: { readonly status: number; readonly path: string }[] = [];
  private readonly allowedConsoleErrors: string[] = [];

  public constructor(private readonly page: Page) {}

  public start(): void {
    this.page.on('pageerror', (error) => this.javascript.push(redact(error.message)));
    this.page.on('console', (message) => {
      const text = message.text();
      if (message.type() === 'error' && !this.allowedConsoleErrors.some((pattern) => text.includes(pattern))) this.javascript.push(redact(text));
    });
    this.page.on('response', (response) => {
      const url = safeUrl(response.url());
      const allowed = this.allowedHttpResponses.some((item) => item.status === response.status() && url.includes(item.path));
      if (response.status() >= 400 && !allowed) this.network.push(response.status() + ' ' + url);
    });
  }

  public allowHttpStatus(status: number, path: string): void {
    this.allowedHttpResponses.push({ status, path });
  }

  public allowConsoleError(pattern: string): void {
    this.allowedConsoleErrors.push(pattern);
  }

  public snapshot(): PilotDiagnostics {
    return {
      javascriptErrors: this.javascript.join('\n'),
      networkErrors: this.network.join('\n'),
      finalUrl: safeUrl(this.page.url())
    };
  }
}
