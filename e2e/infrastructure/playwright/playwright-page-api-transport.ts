import type { Page } from '@playwright/test';
import type { ApiRequest, ApiResponse, ApiTransport } from '../../ports/api-transport.js';

export class PlaywrightPageApiTransport implements ApiTransport {
  private initialized: boolean;
  public constructor(private readonly page: Page, private readonly webBaseUrl: string, private readonly apiBaseUrl: string) {
    this.initialized = page.url().startsWith(webBaseUrl);
  }

  public async send<T>(request: ApiRequest): Promise<ApiResponse<T>> {
    if (!this.initialized) {
      await this.page.goto(this.webBaseUrl + '/login');
      this.initialized = true;
    }
    return await this.page.evaluate(async ({ apiBaseUrl, request }) => {
      const headers: Record<string, string> = {};
      if (request.accessToken) headers.Authorization = `Bearer ${request.accessToken}`;
      if (request.body !== undefined) headers['Content-Type'] = 'application/json';
      const response = await fetch(apiBaseUrl + request.path, {
        method: request.method, headers,
        body: request.body === undefined ? undefined : JSON.stringify(request.body)
      });
      const text = await response.text();
      return { status: response.status, body: text === '' ? undefined : JSON.parse(text) } as ApiResponse<T>;
    }, { apiBaseUrl: this.apiBaseUrl, request });
  }
}
