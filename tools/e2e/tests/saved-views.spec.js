// Saved Views: the round trip through the UI — save, reopen, rename, star, delete, and the startup
// resolution a starred view drives. Everything is reached from the application menu, which is the
// point of the feature: there is no toolbar.
const { test, expect } = require('@playwright/test');

const MENU = '[aria-label="Application menu"]';
const API = '/api/saved-views';

/** Opens the hamburger and hovers the Saved Views submenu so its items are reachable. */
async function openSavedViewsMenu(page) {
  await page.locator(MENU).click();
  await expect(page.locator('.mud-popover-open').first()).toBeVisible();
  await page.locator('.mud-popover-open').getByText('Saved Views', { exact: false }).first().hover();
  await expect(page.getByText('Save As…', { exact: true })).toBeVisible();
}

/** True when the menu's Save entry is disabled — Mud marks it with a class, not the attribute. */
async function saveIsDisabled(page) {
  return page.evaluate(() => {
    const el = [...document.querySelectorAll('.mud-menu-item')].find(i => i.innerText.trim() === 'Save');
    return el ? el.className.includes('mud-disabled') : null;
  });
}

/** Closes any open menu. Escape does not dismiss a MudMenu, and its overlay blocks later clicks. */
async function closeMenus(page) {
  await page.locator('h1, .mud-typography-h5, header').first().click({ force: true });
  await expect(page.getByText('Save As…', { exact: true })).toBeHidden();
}

/**
 * Types into the grid filter and waits for the URL to actually carry it.
 * Row-count gates are not enough: whatever the grid restored on load may already satisfy them, so
 * the test can race ahead and save a query it never set.
 */
async function filterGrid(page, term) {
  await page.getByPlaceholder(/Filter for any field/i).fill(term);
  if (term) {
    await expect.poll(() => new URL(page.url()).search, { timeout: 15000 }).toContain('q=' + term);
  } else {
    await expect.poll(() => new URL(page.url()).search, { timeout: 15000 }).not.toContain('q=');
  }
  await expect(page.locator('table tbody tr').first()).toBeVisible();
}

/** Saves the grid's current query under a name, via the menu. */
async function saveAs(page, name) {
  await openSavedViewsMenu(page);
  await page.getByText('Save As…', { exact: true }).click();
  await page.locator('.mud-dialog input[type=text]').first().fill(name);
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await expect(page.getByText('Save As…', { exact: true })).toBeHidden();
}

/** Removes anything this spec created, whatever state a test ended in. */
async function purgeE2eViews(page) {
  await page.evaluate(async (api) => {
    await fetch(`${api}/default`, { method: 'PUT' });
    const list = await (await fetch(api)).json();
    for (const v of (list.data || []).filter(v => v.name.startsWith('E2e'))) {
      await fetch(`${api}/${v.savedViewUid}`, { method: 'DELETE' });
    }
  }, API);
}

test.describe('Saved Views (slice #12)', () => {
  test.afterEach(async ({ page }) => {
    await page.goto('/');
    await purgeE2eViews(page);
  });

  test('saves the current query, reopens it from the menu, and only enables Save once the grid moves away from it', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('table tbody tr').first()).toBeVisible();

    await filterGrid(page, 'Redis');
    const filteredRows = await page.locator('table tbody tr').count();

    await openSavedViewsMenu(page);
    expect(await saveIsDisabled(page)).toBe(true);
    await closeMenus(page);

    await saveAs(page, 'E2eRedis');

    // Clearing the filter must genuinely leave the saved view behind.
    await filterGrid(page, '');

    await openSavedViewsMenu(page);
    await page.getByText('E2eRedis', { exact: false }).first().click();
    await expect.poll(() => new URL(page.url()).search).toContain('q=Redis');
    await expect.poll(() => page.locator('table tbody tr').count()).toBe(filteredRows);

    // Open and unchanged: Save has nothing to do.
    await openSavedViewsMenu(page);
    expect(await saveIsDisabled(page)).toBe(true);
    await closeMenus(page);

    // Open and changed: Save wakes up.
    await filterGrid(page, 'Queue');
    await openSavedViewsMenu(page);
    expect(await saveIsDisabled(page)).toBe(false);
  });

  test('a starred view opens on a bare URL, but an explicit URL still wins', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('table tbody tr').first()).toBeVisible();

    await filterGrid(page, 'Redis');

    await saveAs(page, 'E2eDefault');

    await page.goto('/saved-views');
    await expect(page.getByText('E2eDefault')).toBeVisible();
    // By name, not .first(): the manage page lists every saved view in manual order, so .first()
    // stars whichever view happens to sort first - which may not be this test's.
    await page.locator('[aria-label="Set E2eDefault as the default view"]').click();
    await expect(page.locator('[aria-label="Clear E2eDefault as the default view"]')).toHaveCount(1);

    // A bare URL lands on the starred view. The contract is the restored QUERY; row counts vary
    // with catalog data, so they are only used as a coarse "it actually filtered" check.
    await page.goto('/');
    await expect.poll(() => new URL(page.url()).search).toContain('q=Redis');

    // An explicit query beats it — a shared link must always show what it says.
    await page.goto('/?q=Queue&n=100&s=ResourceName:asc');
    await expect.poll(() => new URL(page.url()).search).toContain('Queue');
    expect(new URL(page.url()).search).not.toContain('Redis');
  });

  test('renames on the manage page, and refuses a name another view already has', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('table tbody tr').first()).toBeVisible();

    await saveAs(page, 'E2eFirst');
    await filterGrid(page, 'Queue');
    await saveAs(page, 'E2eSecond');

    // Save As onto a taken name warns and offers Replace rather than silently creating a duplicate.
    await openSavedViewsMenu(page);
    await page.getByText('Save As…', { exact: true }).click();
    await page.locator('.mud-dialog input[type=text]').first().fill('E2eFirst');
    await expect(page.locator('.mud-dialog').getByText(/already exists/i)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Replace', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();

    // Rename succeeds when the name is free.
    await page.goto('/saved-views');
    await page.locator('[aria-label="Rename E2eSecond"]').click();
    await page.locator('input[aria-label="New name"]').fill('E2eRenamed');
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(page.getByText('E2eRenamed')).toBeVisible();

    // And is blocked at the field when it is not, before any round trip.
    await page.locator('[aria-label="Rename E2eRenamed"]').click();
    await page.locator('input[aria-label="New name"]').fill('E2eFirst');
    await expect(page.getByText(/already has that name/i)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Save', exact: true })).toBeDisabled();
  });

  test('deleting a view drops it from the manage page and the menu together', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('table tbody tr').first()).toBeVisible();

    await saveAs(page, 'E2eDoomed');

    await page.goto('/saved-views');
    await page.locator('[aria-label="Delete E2eDoomed"]').click();
    await page.getByRole('button', { name: 'Delete', exact: true }).click();
    // Scoped to the table: the success snackbar quotes the name back, so an unscoped locator
    // matches the toast and never reaches zero.
    await expect(page.locator('table').getByText('E2eDoomed')).toHaveCount(0);

    // The menu reads the same list, so it must have dropped it too.
    await page.goto('/');
    await openSavedViewsMenu(page);
    await expect(page.locator('.mud-popover-open').getByText('E2eDoomed')).toHaveCount(0);
  });
});
