// Reusable Playwright helpers for driving MudBlazor components in the HT.ResourceMapper UI.
//
// MudBlazor renders form controls in ways that break naive Playwright locators — these helpers
// encode the DOM patterns discovered while driving the editor (see ../README.md "Gotchas").
//
// Usage:
//   const { launch, clickSelect, clickSelectInScope, pickOption, waitForServer } = require('./mud-helpers');

const { chromium } = require('playwright-core');

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
 * Clicks a MudSelect identified by its floating Label text (e.g. "Resource Type", "Subscription").
 *
 * Why not a plain `getByLabel(...).click()`: MudSelect renders its trigger in one of two shapes
 * depending on whether a value is currently set —
 *   - unset:  a single VISIBLE `<input type="text" aria-label="...">`
 *   - set:    a HIDDEN `<input type="hidden" aria-label="...">` plus a separate VISIBLE sibling
 *             `<div class="...mud-select-input..." tabindex="0">{displayText}</div>` with NO
 *             aria-label at all.
 * Targeting the aria-label directly is therefore unreliable once a value is set. Scoping to the
 * `.mud-input-control` that contains the label text, then picking the `:visible` trigger inside
 * it, works for both shapes.
 */
async function clickSelect(page, labelText) {
  const control = page.locator('.mud-input-control', { hasText: labelText }).first();
  await control.locator('.mud-select-input:visible').first().click();
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

module.exports = { launch, clickSelect, clickSelectInScope, pickOption, waitForServer };
