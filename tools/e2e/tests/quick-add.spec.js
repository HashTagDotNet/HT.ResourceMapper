// Beta slice 8 regression: the quick-add form (/resources/new) and the dependency picker's
// "Create new…" opening that same form as a dialog. Relies on the E2eDepType fixture and the
// e2e-candidate-* picker baseline seeded by ../global-setup.js, and on the DefaultDomain
// ('non-prod') the webServer env in ../playwright.config.js sets.
const { test, expect } = require('@playwright/test');
const { DOMAIN_LABEL, clickSelectInScope, pickOption } = require('../mud-helpers');

const IGNORED_CONSOLE_PATTERNS = [/404 \(Not Found\)/];
const IDENTITY_LABELS = ['Type', 'Name', 'Key', DOMAIN_LABEL];

/** Picks the Type, then waits for the template's Key Tags to render (the re-seed round-trip). */
async function selectType(scope, page, typeName) {
  await scope.locator('.rm-form-row').filter({ has: page.locator('.rm-form-label', { hasText: /^Type/ }) })
    .locator('.mud-select-input:visible').first().click();
  await pickOption(page, typeName);
  await expect(scope.getByText('Key Tags', { exact: true })).toBeVisible({ timeout: 8000 });
}

/** Types the Name and waits for the uniqueness check, which runs once Type + Name + domain are set. */
async function fillName(scope, page, name) {
  await scope.getByLabel('Name', { exact: true }).fill(name);
  await page.keyboard.press('Tab');
  await expect(scope.getByText('Not yet catalogued as this type.')).toBeVisible({ timeout: 8000 });
}

/** Fills every required tag row on the form (any the catalog marks RequirementLevel = 'Error'). */
async function fillRequiredTags(scope, page) {
  const rows = scope.locator('.rm-form-row').filter({ has: page.locator('.rm-form-label-req') });
  for (let i = 0; i < await rows.count(); i++) {
    const row = rows.nth(i);
    const label = (await row.locator('.rm-form-label').innerText()).replace('*', '').trim();
    if (IDENTITY_LABELS.includes(label)) continue;

    if (await row.locator('.mud-select-input:visible').count() > 0) {
      await clickSelectInScope(row);
      await page.getByRole('option').first().click();
    } else {
      await row.locator('input:visible').first().fill('e2e');
      await page.keyboard.press('Tab');
      const add = row.getByRole('button', { name: 'Add this value to the tag' });
      if (await add.count() > 0) await add.click();
    }
    await page.waitForTimeout(250);
  }
}

function tagRow(scope, page, label) {
  return scope.locator('.rm-form-row').filter({ has: page.locator('.rm-form-label', { hasText: label }) }).first();
}

test.describe('Quick add (Beta slice 8)', () => {
  test('Name, Type, one Azure id, one tag and one dependency save from one form with no tab or dialog', async ({ page }) => {
    const consoleErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));

    const name = 'E2E Quick Add ' + Date.now();
    const armId = `/subscriptions/00000000-0000-0000-0000-000000000003/resourceGroups/rg-e2e-quick/providers/Microsoft.Web/sites/quick-${Date.now()}`;

    await page.goto('/');
    await page.getByRole('link', { name: 'Add Resource' }).click();
    await page.waitForURL(/\/resources\/new$/);

    // DefaultDomain is set, so the domain select is not on the main form.
    await expect(tagRow(page, page, DOMAIN_LABEL)).toHaveCount(0);

    await selectType(page, page, 'E2eDepType');
    await fillName(page, page, name);

    const azure = tagRow(page, page, 'Azure Resource Id');
    await azure.locator('input:visible').first().fill(armId);
    await azure.getByRole('button', { name: 'Add this value to the tag' }).click();
    await expect(azure.locator('.mud-chip', { hasText: armId })).toBeVisible();

    await tagRow(page, page, 'Overview').locator('input:visible').first().fill('https://example.com/quick-add');
    await page.keyboard.press('Tab');
    await fillRequiredTags(page, page);

    await page.getByPlaceholder('Add existing resource…').fill('E2E Candidate One');
    await page.getByRole('option', { name: /E2E Candidate One/ }).click();
    await expect(page.locator('.rm-dep-row', { hasText: 'E2E Candidate One' })).toBeVisible();

    // Key and domain sit under More Options: Key slugged from Name, domain the configured default.
    await page.getByRole('button', { name: 'More Options' }).click();
    const expectedKey = name.toLowerCase().replace(/[^a-z0-9]+/g, '-');
    await expect(page.getByLabel('Key', { exact: true })).toHaveValue(expectedKey);
    await expect(tagRow(page, page, DOMAIN_LABEL).locator('.mud-select-input:visible').first()).toContainText('non-prod');

    await expect(page.getByRole('tab')).toHaveCount(0);
    await expect(page.locator('.mud-dialog')).toHaveCount(0);

    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForURL(/\/resources\/[0-9a-f-]{36}\/view$/, { timeout: 10000 });

    await expect(page.locator('[data-testid="azure-ids"] .rm-view-idrow', { hasText: 'rg-e2e-quick' })).toHaveCount(1);
    await expect(page.locator('[data-testid="depends-on"]').getByRole('link', { name: 'E2E Candidate One' })).toBeVisible();
    // Other tags sit in the view's collapsed section, so check presence rather than visibility.
    await expect(page.locator('a[href="https://example.com/quick-add"]')).toHaveCount(1);

    const unexpected = consoleErrors.filter(e => !IGNORED_CONSOLE_PATTERNS.some(p => p.test(e)));
    expect(unexpected, 'no console errors').toEqual([]);
  });

  test('"Create new…" opens the quick form as a dialog and links the saved resource without leaving the page', async ({ page }) => {
    const parentName = 'E2E Quick Parent ' + Date.now();
    const childName = 'E2E Quick Child ' + Date.now();

    await page.goto('/resources/new');
    await selectType(page, page, 'E2eDepType');
    await fillName(page, page, parentName);
    await fillRequiredTags(page, page);

    await page.locator('.rm-dep-create-new').click();
    const dialog = page.locator('.mud-dialog');
    await expect(dialog).toBeVisible();
    expect(page.url()).toMatch(/\/resources\/new$/);

    await selectType(dialog, page, 'E2eDepType');
    await fillName(dialog, page, childName);
    await fillRequiredTags(dialog, page);
    await dialog.getByRole('button', { name: 'Save', exact: true }).click();

    await expect(dialog).toHaveCount(0, { timeout: 10000 });
    await expect(page.locator('.rm-dep-row', { hasText: childName })).toBeVisible();
    await expect(page.getByLabel('Name', { exact: true })).toHaveValue(parentName);

    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForURL(/\/resources\/[0-9a-f-]{36}\/view$/, { timeout: 10000 });
    await expect(page.locator('[data-testid="depends-on"]').getByRole('link', { name: childName })).toBeVisible();
  });
});
