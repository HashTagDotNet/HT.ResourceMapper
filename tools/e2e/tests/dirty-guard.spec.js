// Slice #9 regression: RegisterLocationChangingHandler dirty guard (Save/Discard/Cancel), covering
// both an in-app link click (the hamburger menu) and the browser back/forward buttons (RD1).
//
// IMPORTANT test-methodology note (learned the hard way while building this spec): `page.goto()`
// performs a full cross-document navigation for this Blazor Server app, which tears down and
// rebuilds the interactive circuit from scratch — losing all in-memory dirty state AND the
// browser's real SPA-level history stack the interactive router relies on. Steps that need to
// stay within ONE continuous circuit (making the model dirty, then triggering an in-app nav or a
// real back/forward) must therefore use in-app clicks, not `page.goto`, between them. `page.goto`
// is only safe at a scenario's start, or to independently verify PERSISTED server state afterward.
const { test, expect } = require('@playwright/test');
const { selectTypeAndDomain, fillNameAndWaitForSlug, fillRequiredTags } = require('../mud-helpers');

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

async function editNameInPlace(page, newName) {
  await page.getByRole('button', { name: 'Edit', exact: true }).click();
  await page.waitForTimeout(300);
  // Edit mode doesn't auto-slug Key from Name (GeneralTab.OnNameChanged only does that
  // pre-persist), so there's no cheap DOM-observable "the C# model received it" signal here the
  // way there is in Create mode — a generous fixed wait is the pragmatic choice (proven plenty
  // under load in manual verification; the immediate next step here only reads IsDirty, not a
  // hard validation gate, so this is lower-risk than the Create-mode Save race was).
  await page.getByLabel('Name', { exact: true }).fill(newName);
  await page.keyboard.press('Tab');
  await page.waitForTimeout(800);
}

async function openHamburgerCreateResource(page) {
  await page.getByRole('button', { name: 'Application menu' }).click();
  await page.waitForTimeout(300);
  await page.getByRole('link', { name: 'Create Resource' }).click();
  await page.waitForTimeout(700);
}

test.describe('Dirty guard — unsaved-changes prompt (slice #9)', () => {
  test('Cancel keeps the edit, Discard drops it, Save persists it — via an in-app link click', async ({ page }) => {
    const consoleErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));

    const baseName = 'E2E Dirty Subject ' + Date.now();
    const subjectUrl = await createAndSaveSubject(page, baseName);

    // (a) Cancel -> stays put, edit intact.
    await editNameInPlace(page, baseName + ' ModA');
    await openHamburgerCreateResource(page);
    let dialog = page.locator('.mud-dialog');
    await expect(dialog).toContainText('unsaved changes');
    await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
    await page.waitForTimeout(300);
    expect(page.url()).toBe(subjectUrl, 'Cancel must not navigate away');
    await expect(page.getByLabel('Name', { exact: true })).toHaveValue(baseName + ' ModA',
      'Cancel must leave the in-progress edit intact, not discard it');

    // (b) Discard (still dirty from (a)) -> navigation proceeds, edit is NOT persisted.
    await openHamburgerCreateResource(page);
    dialog = page.locator('.mud-dialog');
    await dialog.getByRole('button', { name: 'Discard', exact: true }).click();
    await page.waitForTimeout(700);
    expect(page.url()).toContain('/resources');
    expect(page.url()).not.toBe(subjectUrl, 'Discard must let the navigation through');

    await page.goto(subjectUrl, { waitUntil: 'networkidle' });
    await expect(page.locator('.rm-shell-title')).toHaveText(baseName,
      'the discarded edit must not have persisted — the name should still be the original');

    // (c) Save (fresh edit) -> navigation proceeds AND the edit is persisted.
    await editNameInPlace(page, baseName + ' ModC');
    await openHamburgerCreateResource(page);
    dialog = page.locator('.mud-dialog');
    await dialog.getByRole('button', { name: 'Save', exact: true }).click();
    await page.waitForTimeout(1000);
    expect(page.url()).not.toBe(subjectUrl, 'a confirmed Save must let the navigation through');

    await page.goto(subjectUrl, { waitUntil: 'networkidle' });
    await expect(page.locator('.rm-shell-title')).toHaveText(baseName + ' ModC',
      'choosing Save in the dirty-guard dialog must persist the edit before leaving');

    const meaningfulErrors = consoleErrors.filter(e => !IGNORED_CONSOLE_PATTERNS.some(rx => rx.test(e)));
    expect(meaningfulErrors, `Unexpected console errors: ${JSON.stringify(meaningfulErrors)}`).toHaveLength(0);
  });

  test('the browser back/forward buttons trip the same guard (RD1)', async ({ page }) => {
    const baseName = 'E2E Dirty Back Subject ' + Date.now();
    const subjectUrl = await createAndSaveSubject(page, baseName);

    // Establish a REAL adjacent SPA history entry via an in-app nav (not page.goto), so a later
    // goForward() has somewhere concrete, in-app-pushed to target.
    await page.getByRole('button', { name: 'Application menu' }).click();
    await page.waitForTimeout(300);
    await page.getByRole('link', { name: 'Import…' }).click();
    await page.waitForTimeout(700);
    expect(page.url()).toContain('/import');

    await page.goBack();
    await page.waitForTimeout(700);
    expect(page.url()).toBe(subjectUrl, 'a clean goBack must proceed with no guard involved');

    // Now dirty the model, then goForward (targeting the /import entry immediately ahead) —
    // the browser-driven navigation must be intercepted exactly like the in-app link click was.
    await editNameInPlace(page, baseName + ' ModBack');
    await page.goForward();
    await page.waitForTimeout(700);

    const dialog = page.locator('.mud-dialog');
    await expect(dialog).toBeVisible();
    expect(page.url()).toBe(subjectUrl, 'PreventNavigation must hold the URL while the dialog is open');

    await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
    await page.waitForTimeout(300);
    await expect(page.getByLabel('Name', { exact: true })).toHaveValue(baseName + ' ModBack',
      'Cancel on a browser-back-triggered prompt must leave the edit intact, same as the link-click path');
  });
});
