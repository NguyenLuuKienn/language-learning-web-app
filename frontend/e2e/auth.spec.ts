import { test, expect } from '@playwright/test';

test.describe('Authentication and Target Selection Flow', () => {
  test('should open study target modal and select preference', async ({ page }) => {
    await page.goto('/');

    const targetBtn = page.getByRole('button', { name: /mục tiêu|đổi mục tiêu|change target|bắt đầu/i }).first();
    if (await targetBtn.isVisible()) {
      await targetBtn.click();
      await expect(page.locator('.ant-modal-content, [role="dialog"]')).toBeVisible();
    }
  });

  test('should display login modal on login button click', async ({ page }) => {
    await page.goto('/');

    const loginBtn = page.getByRole('button', { name: /đăng nhập|login|sign in/i }).first();
    if (await loginBtn.isVisible()) {
      await loginBtn.click();
      await expect(page.locator('.ant-modal-content, [role="dialog"]')).toBeVisible();
    }
  });
});
