# Browser-driven verification (Playwright)

Reusable tooling for actually driving the running Blazor app in a real browser — the
`CLAUDE.md` rule "for UI or frontend changes, start the dev server and use the feature in a
browser before reporting the task as complete" needs this, and there's no `chromium-cli` (or
similar) available in this environment. This uses `playwright-core` (no bundled Chromium
download) against the **system-installed Chrome**.

## Setup (one-time per machine/session)

```bash
cd tools/e2e
npm install
```

`node_modules` is gitignored; `package-lock.json` is committed so installs are reproducible.

## Running the app

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
- **Dialogs**: scope locators to `.mud-dialog` (e.g. `page.locator('.mud-dialog').getByLabel(...)`)
  since a dialog's fields can otherwise collide with same-named fields on the page behind it.
- **Vocab/multi-value "add" selects**: after picking a value, MudSelect's own displayed value can
  stick instead of resetting to the placeholder even after the bound C# field is nulled. A `@key`
  that increments on each pick forces a clean remount (see `TagValueEditor.razor`'s
  `_vocabSelectKey` for the pattern) if you hit this while building a new value editor.

## Files

- `mud-helpers.js` — the reusable helpers (`launch`, `clickSelect`, `clickSelectInScope`,
  `pickOption`, `waitForServer`).
- `example-drive-editor.js` — a minimal template showing the helpers in use.
- `package.json` / `package-lock.json` — `playwright-core` only; no bundled browser download.
