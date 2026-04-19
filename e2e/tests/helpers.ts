import { expect, type APIRequestContext, type Page } from '@playwright/test';

export const WEB_BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:4200';
export const API_BASE_URL = process.env.E2E_API_URL ?? 'http://localhost:5211';

export async function apiLogin(request: APIRequestContext, email: string, password: string) {
  const response = await request.post(`${API_BASE_URL}/api/auth/login`, {
    data: { email, password }
  });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return body.accessToken as string;
}

export async function ensureApiReady(request: APIRequestContext) {
  const response = await request.get(`${API_BASE_URL}/health/db`);
  expect(response.ok()).toBeTruthy();
}

/** Login via UI; espera la navegació principal autenticada. */
export async function loginViaUi(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Contrasenya').fill(password);
  await page.getByRole('button', { name: 'Iniciar sessió' }).click();
  await expect(
    page.getByLabel('Primary navigation').getByRole('link', { name: 'Inici' })
  ).toBeVisible();
}

/** Primer lloc del catàleg públic (per provar detall). */
export async function fetchFirstPlaceId(request: APIRequestContext): Promise<string | null> {
  const response = await request.get(`${API_BASE_URL}/api/places`);
  if (!response.ok()) {
    return null;
  }
  const places = (await response.json()) as Array<{ id: string }>;
  return places[0]?.id ?? null;
}

export async function ensureUser(
  request: APIRequestContext,
  adminToken: string,
  payload: {
    email: string;
    password: string;
    role: string;
    displayName: string;
    city?: string;
    country?: string;
  }
) {
  const usersResponse = await request.get(`${API_BASE_URL}/api/admin/users`, {
    headers: { Authorization: `Bearer ${adminToken}` }
  });
  expect(usersResponse.ok()).toBeTruthy();
  const users = (await usersResponse.json()) as Array<{ email: string }>;
  const emailLower = payload.email.toLowerCase();
  if (users.some((user) => user.email.toLowerCase() === emailLower)) {
    return;
  }

  const createResponse = await request.post(`${API_BASE_URL}/api/admin/users`, {
    headers: { Authorization: `Bearer ${adminToken}` },
    data: {
      email: payload.email,
      password: payload.password,
      role: payload.role,
      displayName: payload.displayName,
      city: payload.city ?? 'Barcelona',
      country: payload.country ?? 'Espanya'
    }
  });

  if (createResponse.status() === 409) {
    return;
  }

  expect(createResponse.ok()).toBeTruthy();
}

export async function ensureRoleUsers(request: APIRequestContext) {
  await ensureApiReady(request);
  const adminToken = await apiLogin(request, 'admin@admin.adm', 'Admin123');

  await ensureUser(request, adminToken, {
    email: 'viewer.e2e@yeppet.local',
    password: 'Admin123',
    role: 'Viewer',
    displayName: 'Viewer E2E'
  });

  await ensureUser(request, adminToken, {
    email: 'user.e2e@yeppet.local',
    password: 'Admin123',
    role: 'User',
    displayName: 'User E2E'
  });

  await ensureUser(request, adminToken, {
    email: 'developer.e2e@yeppet.local',
    password: 'Admin123',
    role: 'Developer',
    displayName: 'Developer E2E'
  });
}
