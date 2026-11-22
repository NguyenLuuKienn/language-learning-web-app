import { test, expect } from '@playwright/test';

const protectedPaths = [
  '/home', '/roadmap', '/courses', '/listening', '/speaking', '/reading',
  '/writing', '/grammar', '/character-lab', '/library', '/leaderboard',
  '/ai-chat', '/decks', '/review', '/quiz', '/stats', '/community', '/sync', '/admin'
];

test('guests stay on the landing page when opening learning pages directly', async ({ page }) => {
  for (const path of protectedPaths) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/$/);
    await expect(page.locator('aside')).toHaveCount(0);
  }
});
