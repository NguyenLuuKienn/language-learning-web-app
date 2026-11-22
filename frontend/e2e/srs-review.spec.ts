import { test, expect } from '@playwright/test';

test('SRS asks for a study target when the learner has none', async ({ page }) => {
  const login = await page.request.post('/api/v1/auth/login', {
    data: { email: 'learner@languagelearning.app', password: process.env.PLAYWRIGHT_LEARNER_PASSWORD || 'Learner@123456' }
  });
  expect(login.ok()).toBeTruthy();

  await page.goto('/review');
  await expect(page.locator('main')).toBeVisible();
  await expect(page.locator('main')).toContainText(/choose a study goal/i);
});
