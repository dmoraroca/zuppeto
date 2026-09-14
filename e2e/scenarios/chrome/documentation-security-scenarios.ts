import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeDocumentationSecurityScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const { page } = context;
  if (code === 144) {
    await page.goto('/admin/documentacio'); await expect(page).toHaveURL(/\/login/); return;
  }
  if (code === 146 || code === 147) {
    await page.goto('/admin/documentacio');
    await expect(page.getByRole('heading', { name: 'Documents' })).toBeVisible();
    await expect(page.locator('.admin-console-grid--docs')).toBeVisible();
    return;
  }
  if (code === 148 || code === 149) {
    await page.goto('/admin/documentacio');
    const name = code === 148 ? /Documentació tècnica/i : /Documentació funcional/i;
    await page.getByRole('button', { name }).click();
    await expect(page.locator('.admin-console-document')).not.toBeEmpty();
    return;
  }
  if (code === 150) {
    await page.goto('/permissions'); await expect(page.locator('main')).toContainText(/permisos|rols/i); return;
  }
  if (code === 151) {
    await page.goto('/permissions'); await expect(page).not.toHaveURL(/\/permissions$/); return;
  }
  if (code === 152) {
    if (context.session.session === undefined) throw new Error('ZUP-152 requereix sessió USER.');
    context.allowHttpStatus(403, '/api/admin/users'); context.allowConsoleError('status of 403');
    const response = await context.transport.send<unknown>({ method: 'GET', path: '/api/admin/users', accessToken: context.session.session.accessToken });
    expect(response.status).toBe(403); return;
  }
  if (code === 153) {
    if (context.session.session === undefined) throw new Error('ZUP-153 requereix sessió ADMIN.');
    const response = await context.transport.send<readonly { readonly key: string }[]>({ method: 'GET', path: '/api/navigation/menu', accessToken: context.session.session.accessToken });
    expect(response.status).toBe(200); expect(response.body.some((item) => item.key === 'admin')).toBeTruthy(); return;
  }
  throw new Error(`Comportament Documentació/Seguretat no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}
