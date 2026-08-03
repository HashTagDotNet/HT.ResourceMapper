// Reusable Playwright helpers for driving MudBlazor components in the HT.ResourceMapper UI.
//
// MudBlazor renders form controls in ways that break naive Playwright locators — these helpers
// encode the DOM patterns discovered while driving the editor (see ../README.md "Gotchas").
//
// Usage:
//   const { launch, clickSelect, clickSelectInScope, pickOption, waitForServer } = require('./mud-helpers');

const { chromium } = require('playwright-core');
const { expect } = require('@playwright/test');

/**
 * Launches system Chrome (no bundled Chromium download needed) and returns { browser, page }.
 * Pass { headless: false } to watch it run.
 */
async function launch(options = {}) {
  const browser = await chromium.launch({
    channel: 'chrome',
    args: ['--no-sandbox'],
    headless: options.headless !== false,
  });
  const page = await browser.newPage({ viewport: options.viewport || { width: 1400, height: 1000 } });

  const consoleErrors = [];
  page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
  page.on('pageerror', err => consoleErrors.push('pageerror: ' + err.message));
  page.consoleErrors = consoleErrors; // attach for callers to inspect after the run

  return { browser, page };
}

/**
 * Clicks a MudSelect identified by its label text (e.g. "Resource Type", "Subscription").
 *
 * Why not a plain `getByLabel(...).click()`: MudSelect renders its trigger in one of two shapes
 * depending on whether a value is currently set —
 *   - unset:  a single VISIBLE `<input type="text" aria-label="...">`
 *   - set:    a HIDDEN `<input type="hidden" aria-label="...">` plus a separate VISIBLE sibling
 *             `<div class="...mud-select-input..." tabindex="0">{displayText}</div>` with NO
 *             aria-label at all.
 * Targeting the aria-label directly is therefore unreliable once a value is set. Scoping to the
 * container that holds the label text, then picking the `:visible` trigger inside it, works for
 * both shapes.
 *
 * Two container shapes, because azure-ux-design-v1 moved editor labels out of the input:
 *   - `.rm-form-row`        — the label lives in a sibling `.rm-form-label`, NOT inside
 *                             `.mud-input-control`, so the old scope no longer matches.
 *   - `.mud-input-control`  — MudBlazor's floating label, still used by surfaces not yet converted.
 * Tried in that order so a converted row is never matched by the legacy path.
 */
async function clickSelect(page, labelText) {
  const formRow = page.locator('.rm-form-row').filter({
    has: page.locator('.rm-form-label', { hasText: labelText }),
  }).first();

  const control = (await formRow.count()) > 0
    ? formRow
    : page.locator('.mud-input-control', { hasText: labelText }).first();

  const trigger = control.locator('.mud-select-input:visible').first();

  // Click, then CONFIRM the popover actually opened, and click once more if it did not.
  //
  // Why: the very first select interaction of a suite run can swallow its click. The server has
  // just started, so that click is also paying JIT plus the first SignalR circuit setup, and
  // MudSelect's open is a server round-trip. Observed concretely — delete.spec's first test timed
  // out at 60s waiting for an option that never appeared, while the identical call passed in 16s on
  // a warm run. A missed first click is indistinguishable from a slow one, so waiting longer does
  // not help; re-clicking does. Bounded at one retry so a genuinely broken locator still fails fast
  // rather than looping.
  await trigger.click();
  try {
    await page.locator('.mud-popover-open .mud-list-item, .mud-popover-open [role=option]')
      .first().waitFor({ state: 'visible', timeout: 5000 });
  } catch {
    await trigger.click();
  }
}

/**
 * Same idea as clickSelect, but for a MudSelect with no Label (e.g. a bare per-row value picker)
 * — pass a scoped locator (a table row, a dialog, etc.) instead of a label.
 */
async function clickSelectInScope(scopeLocator) {
  await scopeLocator.locator('.mud-select-input:visible').first().click();
}

/** Clicks an option in an already-open MudSelect/MudAutocomplete popover. */
async function pickOption(page, optionText, exact = true) {
  await page.getByRole('option', { name: optionText, exact }).click();
}

/**
 * Selects the editor's Resource Type + Subscription (Domain), then waits for BOTH choices to be
 * reflected in the read-only Identity preview before returning.
 *
 * Why this matters: selecting Type triggers OnResourceTypeChanged (re-seeds the Tags tab's rows
 * and calls StateHasChanged on the whole editor), and selecting Domain triggers its own change
 * handler — each of these completes over its own SignalR round-trip, independent of when the
 * *next* Playwright action fires. If the test moves on to fill the Name field before those
 * handlers' own re-renders have landed, a late-arriving re-render can silently overwrite the
 * freshly-typed Name back to the (still-empty) model value — observed for real running this
 * suite under load: the Name field ends up visibly blank even though `.fill()` reported success.
 * Waiting for the Identity preview (driven by the same OnFieldChanged path) to show both values
 * is a directly observable, self-throttling proof that those handlers have fully settled.
 */
async function selectTypeAndDomain(page, typeName, domainName) {
  await clickSelect(page, 'Resource Type');
  await pickOption(page, typeName);
  await clickSelect(page, 'Subscription');
  await pickOption(page, domainName);
  // The identity preview moved from its own card (.rm-identity-preview) into a form row
  // (.rm-identity-inline) when the General tab became a single column — azure-ux-design-v1 §6.
  await expect(page.locator('.rm-identity-inline')).toContainText(`${domainName} / ${typeName}`, { timeout: 8000 });
}

/**
 * Fills every REQUIRED tag on the Tags tab, then returns to the General tab.
 *
 * Why the specs need this now (punchlist PL-52): commit b8a01ff (PL-45) seeded a realistic tag
 * vocabulary in which 'Owner' / "Owning Team" carries RequirementLevel = 'Error'. That is a genuine
 * required field, so Save legitimately refuses with "'Owning Team' is required" — and because these
 * specs predate it and never fill one, ALL EIGHT began failing at Save. The app was right and the
 * specs were stale; this is the fix on the spec side.
 *
 * Deliberately driven off the rendered `.rm-tag-required` marker rather than a hardcoded
 * "Owning Team": if the seed later makes another tag required, these specs keep working instead of
 * failing the same way a second time.
 */
async function fillRequiredTags(page) {
  await page.getByRole('tab', { name: 'Tags' }).click();
  await page.waitForSelector('.rm-tag-row', { timeout: 10000 });

  const requiredRows = page.locator('.rm-tag-row').filter({ has: page.locator('.rm-tag-required') });

  for (let i = 0; i < await requiredRows.count(); i++) {
    const row = requiredRows.nth(i);

    // A controlled-vocabulary tag renders a MudSelect; anything else renders a text input.
    if (await row.locator('.mud-select-input:visible').count() > 0) {
      await clickSelectInScope(row);
      await page.getByRole('option').first().click();
    } else {
      await row.locator('input:visible').first().fill('e2e');
      await page.keyboard.press('Tab');
    }
    await page.waitForTimeout(250);   // let the model round-trip land before the next row
  }

  await page.getByRole('tab', { name: 'General' }).click();
  await page.waitForTimeout(250);
}

/**
 * Fills the editor's Name field and waits for the round-trip to the C# model to actually
 * complete, using the Key field's auto-slug-from-Name as an observable readiness signal.
 *
 * Why not a blind `waitForTimeout` after `.fill()` + Tab: MudTextField's ValueChanged fires on
 * blur (Tab), but that only dispatches the SignalR round-trip — it doesn't block until the
 * server has processed it and re-rendered. A short fixed wait is usually enough, but under
 * heavier system load (observed running a full multi-spec suite back-to-back) it can be too
 * tight: the very next action (e.g. clicking Save, or calling page.goBack()) then races ahead of
 * the model update, acting on a stale/empty Name. Waiting for the Key field's own auto-slugged
 * value is a directly observable, self-throttling signal that the update has actually landed.
 */
async function fillNameAndWaitForSlug(page, name, options = {}) {
  await page.getByLabel(options.nameLabel || 'Name', { exact: true }).fill(name);
  await page.keyboard.press('Tab');
  const expectedKeyPrefix = name.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
  await expect(page.getByLabel(options.keyLabel || 'Key', { exact: true }))
    .toHaveValue(expectedKeyPrefix, { timeout: options.timeout || 8000 });
}

/** Polls a URL until it responds (200/302) or the timeout elapses. Use before driving the app. */
async function waitForServer(url, timeoutMs = 60000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      const res = await fetch(url);
      if (res.status === 200 || res.status === 302) return true;
    } catch { /* server not up yet */ }
    await new Promise(r => setTimeout(r, 1000));
  }
  throw new Error(`Server at ${url} did not respond within ${timeoutMs}ms`);
}

module.exports = { launch, clickSelect, clickSelectInScope, pickOption, selectTypeAndDomain, fillNameAndWaitForSlug, fillRequiredTags, waitForServer };
