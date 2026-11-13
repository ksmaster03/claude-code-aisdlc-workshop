import { test, expect } from '@playwright/test';

test('operator creates a work request and sees it in the list', async ({ page }) => {
  const title = `E2E ทดสอบแจ้งซ่อม ${Date.now()}`;

  await page.goto('/');
  await page.getByTestId('user-name').fill('op.e2e');
  await page.getByTestId('user-role').selectOption('Operator');

  await page.getByTestId('equipment').selectOption('SMT2-AOI-01');
  await page.getByTestId('priority').selectOption('P2');
  await page.getByTestId('title').fill(title);
  await page.getByTestId('description').fill('สร้างจากเทสต์ Playwright');
  await page.getByTestId('submit').click();

  await expect(page.getByTestId('message')).toContainText('บันทึกงานซ่อม WR-');

  const row = page.getByTestId('work-request-table').locator('tbody tr', { hasText: title });
  await expect(row).toHaveCount(1);
  await expect(row).toContainText('SMT2-AOI-01');
  await expect(row).toContainText('รอรับงาน');
});
