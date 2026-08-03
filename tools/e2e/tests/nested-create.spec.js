// Slice #9 regression: the Dependencies "Create new…" nested-editor flow — domain-locked child,
// pop-back-with-link on the child's Save, persistence timing (child persists on its own Save; the
// edge persists only on the parent's next Save).
//
// Note: after a nested pop-back (RestoreFromFrame swaps the parent's model back in), MudTabs
// resets its active panel to the first tab (General) — a benign cosmetic detail, not a data bug
// (confirmed by driving this exact flow manually). Specs must re-click "Dependencies" after a
// pop-back before asserting on its rows, rather than assume the tab stayed active.
const { test, expect } = require('@playwright/test');
const { clickSelect, pickOption, selectTypeAndDomain, fillNameAndWaitForSlug, fillRequiredTags } = require('../mud-helpers');

const IGNORED_CONSOLE_PATTERNS = [/404 \(Not Found\)/];

async function createAndSaveSubject(page, name) {
  await page.goto('/resources');
  await selectTypeAndDomain(page, 'E2eDepType', 'non-prod');
  await fillNameAndWaitForSlug(page, name);
  // PL-52: 'Owning Team' is a required tag since PL-45, so Save refuses without it.
  await fillRequiredTags(page);
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await page.waitForTimeout(1500);
  const url = page.url();
  expect(url).toMatch(/\/resources\/[0-9a-f-]{36}$/);
  return url;
}

async function openDependenciesAndCreateNew(page) {
  await page.getByRole('button', { name: 'Edit', exact: true }).click();
  await page.waitForTimeout(300);
  await page.getByRole('tab', { name: 'Dependencies' }).click();
  await page.waitForTimeout(300);
  await page.locator('.rm-dep-create-new').click();
  // Wait for the nested page to be FULLY rendered before interacting with it, rather than a blind
  // timeout — this hint text only appears once the nested LoadAsync (including the domain-lock
  // write) has completed. Interacting too early (observed under heavier full-suite load) can hit
  // a field mid-re-render and silently lose the input.
  await expect(page.getByText("Locked to the parent resource's Subscription")).toBeVisible({ timeout: 10000 });
}

async function fillNestedChild(page, name) {
  // Domain is already locked/preset here — only Resource Type needs selecting. Still wait for it
  // to settle (same OnResourceTypeChanged re-render race as selectTypeAndDomain guards against)
  // before touching Name, using the identity preview against the ALREADY-locked domain.
  await clickSelect(page, 'Resource Type');
  await pickOption(page, 'E2eDepType');
  await expect(page.locator('.rm-identity-inline')).toContainText('non-prod / E2eDepType', { timeout: 8000 });
  // fillNameAndWaitForSlug's Key-auto-slug wait matters especially here, right before an
  // immediate goBack() in some tests: a premature navigation would otherwise race the model
  // update and submit/act on an empty Name.
  await fillNameAndWaitForSlug(page, name);
  // PL-52/PL-53: the nested child is a resource in its own right, so it carries the same required
  // tags as the parent and its own Save is refused without them.
  await fillRequiredTags(page);
}

test.describe('Nested "Create new…" dependency target (slice #9)', () => {
  test('domain-locked child, links into the parent on Save, and the edge round-trips after the parent Saves', async ({ page }) => {
    const consoleErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));

    const subjectName = 'E2E Nested Subject ' + Date.now();
    const childName = 'E2E Nested Child ' + Date.now();
    const subjectUrl = await createAndSaveSubject(page, subjectName);

    await openDependenciesAndCreateNew(page);
    expect(page.url()).toContain('?n=1', 'a nested create must carry the ?n nested-level marker');

    // Domain lock (RD7): the Subscription select shows the parent's domain and is disabled.
    // `.mud-input.mud-select-input` (both classes together) uniquely identifies the trigger div —
    // `.mud-select-input` alone also matches the hidden input and an inner text/adornment div.
    const domainControl = page.locator('.mud-input-control', { hasText: 'Subscription' }).first();
    const domainInput = domainControl.locator('.mud-input.mud-select-input');
    await expect(domainInput).toContainText('non-prod');
    await expect(domainInput).toHaveClass(/mud-disabled/);

    await fillNestedChild(page, childName);
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1200);

    // Popped back to the parent, child now linked.
    expect(page.url()).toBe(subjectUrl, 'saving the nested child must pop back to the parent\'s own route');
    await page.getByRole('tab', { name: 'Dependencies' }).click(); // tab resets to General on pop — re-select
    await expect(page.locator('.rm-dep-row', { hasText: childName })).toBeVisible();

    // Persist the edge via the parent's own Save, then confirm the round-trip after reload.
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1500);
    await page.reload({ waitUntil: 'networkidle' });
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    await expect(page.locator('.rm-dep-row', { hasText: childName })).toBeVisible();

    // The child is a standalone, independently-viewable resource (persisted on its own Save).
    const childRow = page.locator('.rm-dep-row', { hasText: childName });
    await expect(childRow).toContainText('E2eDepType');
    await expect(childRow).toContainText('non-prod');

    const meaningfulErrors = consoleErrors.filter(e => !IGNORED_CONSOLE_PATTERNS.some(rx => rx.test(e)));
    expect(meaningfulErrors, `Unexpected console errors: ${JSON.stringify(meaningfulErrors)}`).toHaveLength(0);
  });

  test('an untouched nested create does not persist anything and does not prompt on browser Back', async ({ page }) => {
    const subjectName = 'E2E Nested Clean Subject ' + Date.now();
    const subjectUrl = await createAndSaveSubject(page, subjectName);

    await openDependenciesAndCreateNew(page);
    expect(page.url()).toContain('?n=1');

    // Touch nothing — the domain pre-fill itself must not count as a user edit.
    await page.goBack();
    await page.waitForTimeout(700);

    expect(page.url()).toBe(subjectUrl, 'an untouched nested create must pop back cleanly, with no guard prompt');
    await expect(page.locator('.mud-dialog')).toHaveCount(0,
      'a nested create with no edits must not be considered dirty (the domain pre-fill sets Original AND Edited)');
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    await expect(page.locator('.rm-dep-row')).toHaveCount(0, 'nothing should have been linked from an abandoned, untouched nested create');
  });

  test('a nested child saved via the back-prompt (not the Save button) still links into the parent (RD19)', async ({ page }) => {
    const subjectName = 'E2E Nested RD19 Subject ' + Date.now();
    const childName = 'E2E Nested RD19 Child ' + Date.now();
    const subjectUrl = await createAndSaveSubject(page, subjectName);

    await openDependenciesAndCreateNew(page);
    await fillNestedChild(page, childName);

    // Trigger the ascent via browser Back (not the Save button) while the child is dirty.
    await page.goBack();
    await page.waitForTimeout(700);

    const dialog = page.locator('.mud-dialog');
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1200);

    expect(page.url()).toBe(subjectUrl, 'a save-via-back-prompt must still pop back to the parent\'s route');
    await page.getByRole('tab', { name: 'Dependencies' }).click();
    await expect(page.locator('.rm-dep-row', { hasText: childName })).toBeVisible();
  });
});
