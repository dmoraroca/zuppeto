import { expect, type APIRequestContext } from '@playwright/test';

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

export async function ensureUser(
  request: APIRequestContext,
  adminToken: string,
  payload: { email: string; password: string; role: string; displayName: string }
) {
  const usersResponse = await request.get(`${API_BASE_URL}/api/admin/users`, {
    headers: { Authorization: `Bearer ${adminToken}` }
  });
  expect(usersResponse.ok()).toBeTruthy();
  const users = (await usersResponse.json()) as Array<{ email: string }>;
  if (users.some((user) => user.email === payload.email.toLowerCase())) {
    return;
  }

  const createResponse = await request.post(`${API_BASE_URL}/api/admin/users`, {
    headers: { Authorization: `Bearer ${adminToken}` },
    data: payload
  });
  expect(createResponse.ok()).toBeTruthy();
}

export async function ensureRoleUsers(request: APIRequestContext) {
  await ensureApiReady(request);
  const adminToken = await apiLogin(request, 'admin@admin.adm', 'Admin123');

  await ensureUser(request, adminToken, {
    email: 'viewer.e2e@yeppet.local',
    password: 'Admin123',
    role: 'VIEWER',
    displayName: 'Viewer E2E'
  });

  await ensureUser(request, adminToken, {
    email: 'user.e2e@yeppet.local',
    password: 'Admin123',
    role: 'USER',
    displayName: 'User E2E'
  });

  await ensureUser(request, adminToken, {
    email: 'developer.e2e@yeppet.local',
    password: 'Admin123',
    role: 'DEVELOPER',
    displayName: 'Developer E2E'
  });
}
