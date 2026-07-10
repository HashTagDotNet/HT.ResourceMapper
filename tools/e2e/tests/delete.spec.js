// Slice #9 regression: delete action + confirm dialog (dependents warning) + cascade-delete edges.
// Relies on the fixture seeded by ../global-setup.js (../sql/seed.sql):
//   - ResourceType 'E2eDepType'
//   - Picker candidates: e2e-candidate-1, e2e-candidate-2 (non-prod), e2e-candidate-prod (prod)
const { test, expect } = require('@playwright/test');
const { selectTypeAndDomain, fillNameAndWaitForSlug } = require('../mud-helpers');

// Pre-existing, non-blocking artifact already noted in slice #6/#7/#8 execution notes.
const IGNORED_CONSOLE_PATTERNS = [/404 \(Not Found\)/];

async function createSubject(page, name) {
  await page.goto('/resources');
  await selectTypeAndDomain(page, 'E2eDepType', 'non-prod');
  await fillNameAndWaitForSlug(page, name);
}

async function fillPickerAndPick(page, text, optionText) {
  const picker = page.locator('.rm-deps-picker');
  const input = picker.locator('input').first();
  await input.click();
  await page.waitForTimeout(200); // MudAutocomplete's first keystroke can be dropped right after focus
  await input.type(text, { delay: 30 });
  await page.waitForTimeout(600); // debounce + server round-trip
  await page.getByRole('option', { name: optionText }).click();
}

test.describe('Delete action + confirm dialog (slice #9)', () => {
  test('a subject with a dependent: dialog lists it, Cancel does not delete, confirmed delete cascades the edge', async ({ page }) => {
    const consoleErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));

    await createSubject(page, 'E2E Delete Subject With Deps');
    await page.getByRole('tab', { name: 'Dependent On' }).click();
    await fillPickerAndPick(page, 'E2E Candidate One', /E2E Candidate One/);
    await expect(page.locator('.rm-dep-row', { hasText: 'E2E Candidate One' })).toBeVisible();

    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1500);
    const subjectUrl = page.url();
    expect(subjectUrl).toMatch(/\/resources\/[0-9a-f-]{36}$/);

    // Delete -> confirm dialog must list the dependent.
    await page.getByRole('button', { name: 'Delete', exact: true }).click();
    const dialog = page.locator('.mud-dialog');
    await expect(dialog).toContainText('E2E Candidate One');
    await expect(dialog).toContainText('depend on this service');

    // Cancel -> must NOT delete.
    await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
    await page.waitForTimeout(300);
    expect(page.url()).toBe(subjectUrl);

    // Re-open and confirm delete this time.
    await page.getByRole('button', { name: 'Delete', exact: true }).click();
    await page.locator('.mud-dialog').getByRole('button', { name: 'Delete', exact: true }).click();
    await page.waitForTimeout(1200);
    expect(page.url()).not.toBe(subjectUrl); // redirected away (home)

    // Visiting the deleted uid directly must no longer resolve.
    await page.goto(subjectUrl, { waitUntil: 'networkidle' });
    await expect(page.getByText('Resource not found.')).toBeVisible();

    // The cascade must have removed the edge from the dependent's OWN side too.
    await page.goto('/resources/e2e-candidate-1', { waitUntil: 'networkidle' });
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    await expect(page.getByText('E2E Delete Subject With Deps')).toHaveCount(0,
      'the cascade-deleted edge must not still show on the dependent\'s own Dependencies tab');

    const meaningfulErrors = consoleErrors.filter(e => !IGNORED_CONSOLE_PATTERNS.some(rx => rx.test(e)));
    expect(meaningfulErrors, `Unexpected console errors: ${JSON.stringify(meaningfulErrors)}`).toHaveLength(0);
  });

  test('a subject with no dependents: confirm dialog shows no dependents warning; delete succeeds', async ({ page }) => {
    await createSubject(page, 'E2E Delete Subject No Deps');
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1500);
    const subjectUrl = page.url();
    expect(subjectUrl).toMatch(/\/resources\/[0-9a-f-]{36}$/);

    await page.getByRole('button', { name: 'Delete', exact: true }).click();
    const dialog = page.locator('.mud-dialog');
    await expect(dialog).toContainText('Are you sure you want to delete');
    await expect(dialog).not.toContainText('depend on this service',
      'a subject with no dependents must not show the dependents warning list');

    await dialog.getByRole('button', { name: 'Delete', exact: true }).click();
    await page.waitForTimeout(1200);
    expect(page.url()).not.toBe(subjectUrl);

    await page.goto(subjectUrl, { waitUntil: 'networkidle' });
    await expect(page.getByText('Resource not found.')).toBeVisible();
  });
});
