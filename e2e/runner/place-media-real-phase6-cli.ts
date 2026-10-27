import assert from 'node:assert/strict';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';
import { requiredEnvironment } from '../infrastructure/config/required-environment.js';

const webBaseUrl = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:4200';
const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:5211';
const placeId = process.env.E2E_MEDIA_PLACE_ID ?? '8de3f0cd-52b3-49de-9af4-68dc1cf29ea2';
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@admin.adm';
const adminPassword = requiredEnvironment('E2E_ADMIN_PASSWORD');

async function main(): Promise<void> {
  const login = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: 'POST', headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ email: adminEmail, password: adminPassword })
  });
  assert.equal(login.status, 200, `Login ADMIN: HTTP ${login.status}`);
  const session = await login.json() as {
    accessToken: string;
    user: { email: string; displayName?: string; name?: string };
  };
  session.user.name ??= session.user.displayName ?? session.user.email;
  const response = await fetch(`${apiBaseUrl}/api/places/${placeId}`, {
    headers: { authorization: `Bearer ${session.accessToken}` }
  });
  assert.equal(response.status, 200, `Detall públic: HTTP ${response.status}`);
  const place = await response.json() as {
    name: string;
    coverImageUrl: string;
    media: Array<{ verificationStatus: string; identityVerified: boolean; servedUrl: string }>;
  };
  assert.equal(place.name, 'Bar Restaurant Capote');
  assert.match(place.coverImageUrl, /^\/media\/place-media\/.*\.jpg$/);
  assert.equal(place.media.length, 1, 'Només el media verificat ha d’arribar al contracte públic.');
  assert.equal(place.media[0].verificationStatus, 'Verified');
  assert.equal(place.media[0].identityVerified, true);

  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const context = await browser.newContext({ baseURL: webBaseUrl });
    await context.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session', value: session
    });
    const page = await context.newPage();
    await page.goto(`/places/${placeId}`, { waitUntil: 'domcontentloaded' });
    await page.getByRole('heading', { level: 1, name: 'Bar Restaurant Capote' }).waitFor();
    const image = page.locator('.place-detail-page__hero-cover img');
    await image.waitFor();
    assert.match(await image.getAttribute('src') ?? '', /\/media\/place-media\/.*\.jpg$/);
    assert.ok(await image.evaluate(element => (element as HTMLImageElement).naturalWidth > 0),
      'El JPEG governat s’ha de carregar realment.');
    await page.getByText(/Foto:.*Mapillary.*toniserra/i).waitFor();
    await page.getByRole('link', { name: 'CC-BY-SA' }).waitFor();
    assert.equal(await page.getByText('Il·lustració PetiLoc').count(), 0,
      'El detall amb fotografia verificada no ha de mostrar el placeholder.');
    await context.close();
    console.log('PASS Chrome media real Fase VI: fotografia local, atribució i filtratge públic verificats.');
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
