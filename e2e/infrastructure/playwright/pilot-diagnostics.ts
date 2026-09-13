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

  public constructor(private readonly page: Page) {}

  public start(): void {
    this.page.on('pageerror', (error) => this.javascript.push(redact(error.message)));
    this.page.on('console', (message) => {
      if (message.type() === 'error') this.javascript.push(redact(message.text()));
    });
    this.page.on('response', (response) => {
      if (response.status() >= 400) this.network.push(response.status() + ' ' + safeUrl(response.url()));
    });
  }

  public snapshot(): PilotDiagnostics {
    return {
      javascriptErrors: this.javascript.join('\n'),
      networkErrors: this.network.join('\n'),
      finalUrl: safeUrl(this.page.url())
    };
  }
}
