import { test, expect } from '@playwright/test';
import { API_BASE_URL, apiLogin, ensureRoleUsers } from './helpers';

test.beforeAll(async ({ request }) => {
  await ensureRoleUsers(request);
});

test('admin api endpoints respond', async ({ request }) => {
  const token = await apiLogin(request, 'admin@admin.adm', 'Admin123');

  const users = await request.get(`${API_BASE_URL}/api/admin/users`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(users.ok()).toBeTruthy();

  const permissions = await request.get(`${API_BASE_URL}/api/admin/permissions`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(permissions.ok()).toBeTruthy();

  const menus = await request.get(`${API_BASE_URL}/api/admin/menus`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(menus.ok()).toBeTruthy();

  const documents = await request.get(`${API_BASE_URL}/api/admin/documents`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(documents.ok()).toBeTruthy();
});

test('user cannot access admin endpoints', async ({ request }) => {
  const token = await apiLogin(request, 'user.e2e@yeppet.local', 'Admin123');

  const users = await request.get(`${API_BASE_URL}/api/admin/users`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(users.status()).toBe(403);
});

test('navigation menu for admin includes admin node', async ({ request }) => {
  const token = await apiLogin(request, 'admin@admin.adm', 'Admin123');

  const response = await request.get(`${API_BASE_URL}/api/navigation/menu`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(response.ok()).toBeTruthy();
  const menu = (await response.json()) as Array<{ key: string }>;
  expect(menu.some((item) => item.key === 'admin')).toBeTruthy();
});
