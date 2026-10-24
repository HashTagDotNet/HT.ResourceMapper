// Slice #8 regression: same-domain picker + relationship reconciliation. The Dependent On authoring tab is
// hidden (Beta slice 4), so only the DependsOn direction is driven here.
// Relies on the fixture seeded by ../global-setup.js (../sql/seed.sql):
//   - ResourceType 'E2eDepType'
//   - Picker candidates: e2e-candidate-1, e2e-candidate-2 (non-prod), e2e-candidate-prod (prod)
const { test, expect } = require('@playwright/test');
const { DOMAIN_LABEL, clickSelect, pickOption, fillRequiredTags } = require('../mud-helpers');

// Pre-existing, non-blocking artifact already noted in slice #6/#7 execution notes.
const IGNORED_CONSOLE_PATTERNS = [/404 \(Not Found\)/];

async function fillPickerAndPick(page, text, optionText) {
  const picker = page.locator('.rm-deps-picker');
  const input = picker.locator('input').first();
  await input.click();
  await page.waitForTimeout(200); // MudAutocomplete's first keystroke can be dropped right after focus
  await input.type(text, { delay: 30 });
  await page.waitForTimeout(600); // debounce + server round-trip
  await page.getByRole('option', { name: optionText }).click();
}

test.describe('Dependencies tab (slice #8; Dependent On tab hidden in Beta slice 4)', () => {
  test('same-domain picker excludes cross-domain candidates; the DependsOn edge round-trips and can be removed', async ({ page }) => {
    const consoleErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));

    // 1. Create the subject resource (non-prod, E2eDepType).
    await page.goto('/resources');
    await clickSelect(page, 'Resource Type');
    await pickOption(page, 'E2eDepType');
    await clickSelect(page, DOMAIN_LABEL);
    await pickOption(page, 'non-prod');
    await page.getByLabel('Name', { exact: true }).fill('E2E Subject');
    await page.keyboard.press('Tab');

    // 2. Dependencies tab — the picker must offer only the non-prod candidates.
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    const picker = page.locator('.rm-deps-picker');
    await picker.locator('input').first().click();
    await page.waitForTimeout(200);
    await picker.locator('input').first().type('E2E Candidate', { delay: 30 });
    await page.waitForTimeout(600);

    await expect(page.getByRole('option', { name: /E2E Candidate One/ })).toBeVisible();
    await expect(page.getByRole('option', { name: /E2E Candidate Two/ })).toBeVisible();
    await expect(page.getByRole('option', { name: /E2E Candidate Prod/ })).toHaveCount(0,
      'the prod candidate must never appear in a non-prod subject\'s picker results');

    await page.getByRole('option', { name: /E2E Candidate One/ }).click();
    await expect(page.locator('.rm-dep-row', { hasText: 'E2E Candidate One' })).toBeVisible();

    // 4. Save. PL-52: 'Owning Team' is a required tag since PL-45, so Save refuses without it.
    await fillRequiredTags(page);
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1500);
    const subjectUrl = page.url();
    expect(subjectUrl).toMatch(/\/resources\/[0-9a-f-]{36}$/);

    // 5. Reload — confirm both directions round-tripped.
    await page.reload({ waitUntil: 'networkidle' });
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    await expect(page.getByText('E2E Candidate One')).toBeVisible();
    // 7. Back to the subject — remove the DependsOn edge, save, confirm it's gone.
    await page.goto(subjectUrl, { waitUntil: 'networkidle' });
    await page.getByRole('button', { name: 'Edit', exact: true }).click();
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    await page.locator('.rm-dep-row', { hasText: 'E2E Candidate One' }).getByRole('button').click();
    await expect(page.locator('.rm-dep-row', { hasText: 'E2E Candidate One' })).toHaveCount(0);
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1200);

    await page.reload({ waitUntil: 'networkidle' });
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    await expect(page.getByText('E2E Candidate One')).toHaveCount(0,
      'the removed DependsOn edge must not reappear after reload');
    const meaningfulErrors = consoleErrors.filter(e => !IGNORED_CONSOLE_PATTERNS.some(rx => rx.test(e)));
    expect(meaningfulErrors, `Unexpected console errors: ${JSON.stringify(meaningfulErrors)}`).toHaveLength(0);
  });
});
