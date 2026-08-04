# Browser-driven verification (Playwright)

Two things live here:
1. **A committed regression suite** (`@playwright/test`, `tests/*.spec.js`) — the permanent home
   for E2E specs, with its own DB fixture (`sql/seed.sql` / `sql/cleanup.sql`) and an
   auto-managed dev server (`playwright.config.js`'s `webServer`). Run it with `npm run test:e2e`.
2. **Ad-hoc drive-script helpers** (`mud-helpers.js`, `example-drive-editor.js`) — for driving the
   app manually while building/debugging a feature, per `CLAUDE.md`'s rule "for UI or frontend
   changes, start the dev server and use the feature in a browser before reporting the task as
   complete." There's no `chromium-cli` (or similar) available in this environment, so both paths
   use `playwright-core`/`@playwright/test` (no bundled Chromium download) against the
   **system-installed Chrome**.

## Setup (one-time per machine/session)

```bash
cd tools/e2e
npm install
```

`node_modules` and `test-results/` are gitignored; `package-lock.json` is committed so installs
are reproducible. No `npx playwright install` is needed — `channel: 'chrome'` uses the
system-installed browser.

## Running the committed regression suite

```bash
cd tools/e2e
npm run test:e2e
```

This is fully self-contained: `playwright.config.js`'s `webServer` starts
`dotnet run --project UI/ResourceMapper.UI.Web` (reusing an already-running dev server on
`:5200` instead of double-starting one — set `CI=1` to force a fresh server instead), waits for
it, then a `globalSetup` seeds the DB fixture (cleanup-then-seed, so a crashed prior run leaves
no residue) before any spec runs, and a `globalTeardown` cleans it up after. Requires the dacpac
to already be published to `(localdb)\MSSQLLocalDB\ResourceMapper` (see the root `CLAUDE.md`) —
this suite doesn't build/publish the DB itself.

The fixture (`sql/seed.sql`) seeds a dedicated `E2eDepType` resource type + two `E2e`-prefixed
tag definitions + entry-point templates + a read-only picker-candidate baseline (2 `non-prod` +
1 `prod`), all referencing the **shared** domain `TagDefinition` by lookup only — cleanup
(`sql/cleanup.sql`) never touches that shared row, only `E2e`-prefixed/typed data. Mutating specs
create their own subject resource via the UI's Create flow rather than touching the shared
candidate baseline, so specs don't need to run in a particular order — but `workers: 1` /
`fullyParallel: false` is still set, since a single shared localdb has no real parallel isolation.

Add new specs under `tests/*.spec.js`; reuse `mud-helpers.js`'s locator helpers (see Gotchas
below) — `@playwright/test`'s own `page` fixture replaces `launch()` from the ad-hoc path.

**CI wiring is out of scope for now** — a pipeline would additionally need Chrome + a local SQL
instance + a published dacpac, none of which this suite sets up itself.

## Running the app (for ad-hoc drive scripts)

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5200" \
  dotnet run --project UI/ResourceMapper.UI.Web --no-launch-profile &
```

Wait for it with `waitForServer` (see `mud-helpers.js`) rather than a fixed sleep. When done,
stop it — on Windows there's no reliable PID-based kill for a backgrounded `dotnet run` from a
bash tool, so `taskkill //F //IM dotnet.exe` is the pattern used so far (kills *all* dotnet
processes — fine for a solo dev box, but be aware of that if something else is running).

## Writing a driver script

Copy `example-drive-editor.js` to a scratch location (or a temp scratchpad directory) and adapt
it — don't accumulate one-off slice scripts in this folder; it's meant to stay a small, generic
toolkit (`mud-helpers.js` + one example), not a growing pile of per-feature scripts. Import the
helpers with `require('<path-to-repo>/tools/e2e/mud-helpers')`.

Each run should:
1. `waitForServer(url)` before touching the page.
2. Screenshot at each meaningful step (`page.screenshot({ path, fullPage: true })`) — read the
   screenshots back and actually look at them; don't just check for a thrown exception.
3. Collect console errors via `page.consoleErrors` (attached by `launch()`) and report them.
4. If the flow needs seed data (a resource type, tag definitions, etc.), seed it via `sqlcmd`
   beforehand and **delete it afterward** — see the root `CLAUDE.md` for the sqlcmd path and
   `SET QUOTED_IDENTIFIER ON;` requirement. Don't leave demo data behind.

## Gotchas (hard-won — read before fighting a locator for 20 minutes)

- **MudSelect's trigger element changes shape depending on whether a value is set.** Unset: a
  single visible `<input type="text" aria-label="Label">`. Once a value is set: that same input
  flips to `type="hidden"`, and a separate *sibling* `<div class="...mud-select-input..."
  tabindex="0">{displayText}</div>` (no `aria-label`) becomes the actual visible/clickable
  trigger. A plain `getByLabel(...).click()` or a positional `.nth(n)` locator will intermittently
  hit the hidden element and time out. Use `clickSelect(page, labelText)` /
  `clickSelectInScope(scopeLocator)` from `mud-helpers.js` — they scope to the `.mud-input-control`
  containing the label (or a caller-supplied scope) and click whichever trigger is currently
  `:visible`.
- **Radio groups use `MudRadioGroup<T>`, not per-item `Checked`/`CheckedChanged`.** Wrap the whole
  set of `MudRadio<T>` in one `MudRadioGroup<T>` bound to the shared value; don't look for a
  `Checked` parameter on `MudRadio` itself (it doesn't have one).
- **`MudTextField` is debounced by default** (`Immediate="false"`): `ValueChanged` fires on blur
  or Enter, not on every keystroke. Playwright's `.fill()` does dispatch input events but leaves
  focus in the field — follow it with `page.keyboard.press('Tab')` (or click elsewhere) if you
  need the value to have reached the Blazor circuit before your next assertion/screenshot.
- **A `MudTabPanel`'s own children can lag a visual refresh while that panel stays continuously
  active** — e.g. a field's live `Error`/`ErrorText` style may not repaint until you switch to
  another tab and back, even though the underlying bound value already updated (verify via a
  different tab, like Review, which does reflect the change immediately). This looks like a
  MudBlazor rendering/JS-interop nuance tied to an already-active tab panel, not a data bug —
  don't assume a validation binding is broken just because the *same-render* visual state didn't
  refresh; check whether the underlying model actually updated first.
- **`MudMenu` items are `.mud-menu-item` anchors, not `.mud-list-item`.** The app-bar menu renders into
  a `.mud-popover` as `<a class="mud-menu-item" href="…">`, so a `.mud-list-item` locator (which is what
  a MudSelect's popover uses) silently matches nothing and the failure looks like "the menu entry is
  missing" rather than "the selector is wrong". Use
  `page.locator('.mud-menu-item', { hasText: /^Subscriptions$/ })`, and note the menu button is reached
  by its aria-label: `getByRole('button', { name: /Application menu/i })`.
- **Dialogs**: scope locators to `.mud-dialog` (e.g. `page.locator('.mud-dialog').getByLabel(...)`)
  since a dialog's fields can otherwise collide with same-named fields on the page behind it.
- **Vocab/multi-value "add" selects**: after picking a value, MudSelect's own displayed value can
  stick instead of resetting to the placeholder even after the bound C# field is nulled. A `@key`
  that increments on each pick forces a clean remount (see `TagValueEditor.razor`'s
  `_vocabSelectKey` for the pattern) if you hit this while building a new value editor.
- **A freshly-focused text input can drop its first typed character.** `input.type('E2E Foo')`
  right after `input.click()` sometimes lands as `'2E Foo'` — the very first keystroke races the
  field's focus/animation. Add a short `page.waitForTimeout(200)` between click and type on any
  field you just focused, especially `MudAutocomplete`.
- **Passing a component parameter without `@` silently passes literal text, not the C# value —
  and it still compiles.** For a `string`-typed Blazor component parameter, `Attr="foo.Bar"`
  (no `@`) is valid Razor and compiles cleanly, but `foo.Bar` is treated as a **string literal**,
  not evaluated as an expression — the component receives the literal text `"foo.Bar"`. Only
  non-`string`-typed parameters (bool, int, a class, a list, …) get inferred as C# expressions
  without `@`. This bit slice #8 for real: `<DependencyTab Domain="_model.Domain.EditedValue" />`
  silently passed the string `"_model.Domain.EditedValue"` to a same-domain filter, so it matched
  nothing — no compiler warning, no exception, just zero search results. If a `string` parameter
  ever looks suspiciously wrong at runtime, check for a missing `@` before anything else.

## Files

- `mud-helpers.js` — the reusable helpers (`launch`, `clickSelect`, `clickSelectInScope`,
  `pickOption`, `waitForServer`), shared by both the ad-hoc path and the committed suite.
- `example-drive-editor.js` — a minimal template showing the helpers in use (ad-hoc path).
- `playwright.config.js` — the committed suite's runner config (`webServer`, `globalSetup`/
  `globalTeardown`, `workers: 1`, `channel: 'chrome'`).
- `global-setup.js` / `global-teardown.js` — shell out to `sqlcmd` to seed/clean the DB fixture.
- `sql/seed.sql` / `sql/cleanup.sql` — the DB fixture itself (idempotent; FK-safe cleanup order).
- `tests/*.spec.js` — the committed regression specs.
- `package.json` / `package-lock.json` — `playwright-core` + `@playwright/test`; no bundled
  browser download.
