// Beta slice 7 regression: the Home grid's Azure Id column and the search-by-ARM-id path.
// Relies on the e2e-view-root fixture seeded by ../global-setup.js (two AzureResourceId values,
// the usspi- one classifying as Old via the webServer env in ../playwright.config.js).
const { test, expect } = require('@playwright/test');

const IGNORED_CONSOLE_PATTERNS = [/404 \(Not Found\)/];
const ARM_ID = '/subscriptions/00000000-0000-0000-0000-000000000002/resourceGroups/rg-e2e-eastus2/providers/Microsoft.Sql/servers/sql-e2e-eus2-01/databases/E2eViewRoot';

test.describe('Home (Beta slice 7)', () => {
  test('searching a full ARM id returns the owning resource with its chip highlighted; name link leaves the URL clean', async ({ page }) => {
    const consoleErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));

    await page.goto('/');
    await expect(page.getByRole('link', { name: 'Add Resource' })).toHaveAttribute('href', '/resources/new');

    const search = page.getByLabel('Filter for any field');
    await search.fill(ARM_ID);

    const row = page.locator('.mud-table-row', { hasText: 'E2E View Root' });
    await expect(row).toHaveCount(1);
    await expect(page.locator('.mud-table-body .mud-table-row')).toHaveCount(1);

    const chips = row.locator('.rm-azid');
    await expect(chips).toHaveCount(2);
    await expect(chips.filter({ hasText: 'sql-e2e-eus2-01/E2eViewRoot' })).toHaveAttribute('data-hit', 'true');
    await expect(chips.filter({ hasText: 'sql-e2e-eus2-01/E2eViewRoot' }).locator('.rm-view-badge')).toHaveText('New');
    await expect(chips.filter({ hasText: 'usspi-sql-e2e-01/E2eViewRoot' })).toHaveAttribute('data-hit', 'false');
    await expect(chips.filter({ hasText: 'usspi-sql-e2e-01/E2eViewRoot' }).locator('.rm-view-badge')).toHaveText('Old');

    // Race: click the name link while the search box still has focus.
    await search.focus();
    await row.getByRole('link', { name: 'E2E View Root' }).click();
    await page.waitForURL(/\/resources\/e2e-view-root\/view/);
    await page.waitForTimeout(1500);
    expect(page.url()).toMatch(/\/resources\/e2e-view-root\/view$/);

    const unexpected = consoleErrors.filter(e => !IGNORED_CONSOLE_PATTERNS.some(p => p.test(e)));
    expect(unexpected, 'no console errors').toEqual([]);
  });
});
