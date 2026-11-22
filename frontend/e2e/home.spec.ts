import { test, expect } from '@playwright/test';

test.describe('Home and Navigation Suite', () => {
  test.beforeEach(async ({ page }) => {
    const login = await page.request.post('/api/v1/auth/login', {
      data: { email: 'learner@languagelearning.app', password: process.env.PLAYWRIGHT_LEARNER_PASSWORD || 'Learner@123456' }
    });
    expect(login.ok()).toBeTruthy();
    await page.goto('/leaderboard');
  });

  test('should load application and display brand title', async ({ page }) => {
    await expect(page).toHaveTitle(/LinguaFlow/i);
    await expect(page.locator('aside')).toBeVisible();
    await expect(page.locator('main')).toBeVisible();
  });

  test('should navigate across main views via sidebar', async ({ page }) => {
    await page.getByRole('navigation').getByRole('button', { name: /my decks/i }).click();
    await expect(page).toHaveURL(/\/decks$/);
    await expect(page.locator('main')).toBeVisible();

    await page.getByRole('navigation').getByRole('button', { name: /(review|ôn tập)$/i }).click();
    await expect(page).toHaveURL(/\/review$/);
    await expect(page.locator('main')).toBeVisible();
  });
});
