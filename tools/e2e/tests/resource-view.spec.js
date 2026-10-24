// Beta slice 6 regression: the read-only resource view.
// Relies on the fixture seeded by ../global-setup.js (../sql/seed.sql):
//   - e2e-view-root with two AzureResourceId values (one in a usspi- resource group)
//   - e2e-view-mid depends on root, e2e-view-top depends on mid
// The Old badge needs the legacy prefix the webServer env in ../playwright.config.js sets.
const { test, expect } = require('@playwright/test');

const IGNORED_CONSOLE_PATTERNS = [/404 \(Not Found\)/];

test.describe('Resource view (Beta slice 6)', () => {
  test('shows Azure ids with Old/New badges, and Used By expands to the second hop', async ({ page }) => {
    const consoleErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));

    await page.goto('/resources/e2e-view-root/view');

    const ids = page.locator('[data-testid="azure-ids"] .rm-view-idrow');
    await expect(ids).toHaveCount(2);
    await expect(ids.filter({ hasText: 'usspi-rg-e2e-01' }).locator('.rm-view-badge')).toHaveText('Old');
    await expect(ids.filter({ hasText: 'rg-e2e-eastus2' }).locator('.rm-view-badge')).toHaveText('New');

    const usedBy = page.locator('[data-testid="used-by"]');
    const midRow = usedBy.locator('.rm-view-row[data-uid="e2e-view-mid"]');
    await expect(midRow.getByRole('link', { name: 'E2E View Mid' })).toHaveAttribute('href', '/resources/e2e-view-mid/view');
    await expect(usedBy.locator('.rm-view-row[data-uid="e2e-view-top"]')).toHaveCount(0);

    await midRow.getByRole('button', { name: 'Expand E2E View Mid' }).click();
    await expect(usedBy.locator('.rm-view-row[data-uid="e2e-view-top"]').getByRole('link', { name: 'E2E View Top' })).toBeVisible();

    const unexpected = consoleErrors.filter(e => !IGNORED_CONSOLE_PATTERNS.some(p => p.test(e)));
    expect(unexpected, 'no console errors').toEqual([]);
  });
});
