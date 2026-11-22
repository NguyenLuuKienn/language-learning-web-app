import { test, expect } from '@playwright/test';

const pages = [
  '/home', '/roadmap', '/courses', '/listening', '/speaking', '/reading',
  '/writing', '/grammar', '/character-lab', '/library', '/leaderboard',
  '/ai-chat', '/decks', '/review', '/quiz', '/stats', '/community', '/sync'
];

test('learner pages render without JavaScript or server errors', async ({ page }) => {
  const login = await page.request.post('/api/v1/auth/login', {
    data: { email: 'learner@languagelearning.app', password: process.env.PLAYWRIGHT_LEARNER_PASSWORD || 'Learner@123456' }
  });
  expect(login.ok()).toBeTruthy();
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(`${page.url()}: ${error.stack ?? error.message}`));
  page.on('response', (response) => {
    if (response.url().includes('/api/v1/') && response.status() >= 400) {
      errors.push(`${response.status()} ${response.url()}`);
    }
  });

  for (const path of pages) {
    await page.goto(path);
    await expect(page.locator('aside')).toBeVisible();
    await expect(page.locator('main')).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`${path.replaceAll('/', '\\/')}$`));
    await page.waitForTimeout(150);
  }

  expect(errors).toEqual([]);
});

test('admin page renders after admin login', async ({ page }) => {
  const login = await page.request.post('/api/v1/auth/login', {
    data: { email: 'admin@languagelearning.app', password: 'Admin@123456' }
  });
  expect(login.ok()).toBeTruthy();
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  page.on('response', (response) => {
    if (response.url().includes('/api/v1/') && response.status() >= 400) {
      errors.push(`${response.status()} ${response.url()}`);
    }
  });

  await page.goto('/admin');
  await expect(page.locator('main')).toBeVisible();
  await page.getByRole('button', { name: /Content quality|Chất lượng nội dung/ }).click();
  await expect(page.getByRole('heading', { name: /Lesson content quality|Chất lượng nội dung bài học/ })).toBeVisible();
  await page.getByRole('button', { name: /Scan content|Quét nội dung/ }).click();
  await expect(page.getByText(/items to review across|mục cần xem lại trong/)).toBeVisible();
  for (const tab of ['Curriculum & Languages', 'User Accounts & Roles', 'Security Audit Logs', 'Executive Dashboard']) {
    await page.getByRole('button', { name: new RegExp(`${tab.replaceAll('&', '\\&')}$`) }).first().click();
    await page.waitForTimeout(150);
  }
  expect(errors).toEqual([]);
});

test('learner cannot open the admin page', async ({ page }) => {
  const login = await page.request.post('/api/v1/auth/login', {
    data: { email: 'learner@languagelearning.app', password: process.env.PLAYWRIGHT_LEARNER_PASSWORD || 'Learner@123456' }
  });
  expect(login.ok()).toBeTruthy();
  await page.goto('/admin');
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('aside')).toHaveCount(0);
});

test('login uses cookies and logout closes the browser session', async ({ page }) => {
  const login = await page.request.post('/api/v1/auth/login', {
    data: { email: 'learner@languagelearning.app', password: process.env.PLAYWRIGHT_LEARNER_PASSWORD || 'Learner@123456' }
  });
  expect(login.ok()).toBeTruthy();
  const payload = await login.json();
  expect(payload.accessToken).toBeUndefined();
  expect(payload.refreshToken).toBeUndefined();
  await page.goto('/home');
  await expect(page.locator('aside')).toBeVisible();
  expect(await page.evaluate(() => localStorage.getItem('access_token'))).toBeNull();
  const csrf = await page.evaluate(() => document.cookie.split('; ').find(part => part.startsWith('ll_csrf='))?.slice(8));
  const logout = await page.request.post('/api/v1/auth/logout', { headers: { 'X-CSRF-Token': decodeURIComponent(csrf || '') } });
  expect(logout.ok()).toBeTruthy();
  await page.goto('/home');
  await expect(page).toHaveURL(/\/$/);
  const profile = await page.request.get('/api/v1/me');
  expect(profile.status()).toBe(401);
});
