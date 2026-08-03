# Azure-Inspired UX Design — v1

> **Status:** design agreed 2026-08-02 via brainstorming + visual companion. Implements punchlist
> batch **B11** (PL-11) and absorbs **B4** (PL-15, PL-30, PL-31, PL-32, PL-34) and parts of
> **B7** (PL-23, PL-24, PL-35, PL-07) and **B10** (PL-08).
> Mockups: `.superpowers/brainstorm/906-1785728872/content/` (`form-density.html`, `page-chrome.html`).

## Context

The app was judged "too loose / bootstrap-ish" and not compact enough. That is a **documented
requirement miss**, not taste — UX-Plan-V2 asks for "visually light weight and compact" with
"visual focus on data, not operations".

The diagnosis matters: the dominant driver is MudBlazor's **outlined-box + floating-label**
treatment, not spacing. Azure's blade reads denser at the same font size because labels sit in
their own column. Equally important, Steve's emphasis was **"use Mud to make a similar Azure UX
experience, especially around help, input verbiage"** — Azure's blade feels better largely because
every field explains itself before you type.

**Standing rule:** *be aggressive on usability.* These are v1 requirements, not polish.

**Why it matters for this product:** *"Users will not frequent these pages."* No familiarity ever
accrues — every visit is effectively a first visit.

## Agreed decisions

### 1. Customization boundary

Work in these layers, in order of preference. Layer 4 is a last resort.

| Layer | Mechanism | Use for |
|---|---|---|
| 1 | **`MudTheme` in C#** — `Typography` (Body1/Body2/Input/Subtitle2), `LayoutProperties`, existing `PaletteLight` (`MainLayout.razor:30-50`) | Type scale, field text size, border radius, colour |
| 2 | **Layout CSS around Mud** — `.rm-*` classes | Label-beside-field grid, spacing rhythm, section blocks, page shell |
| 3 | **`--mud-*` CSS variables** | Theming gaps layer 1 doesn't expose |
| 4 | Overriding `.mud-*` internals | **Last resort only.** Each instance documented inline with why. Breaks on MudBlazor 9.x upgrades. |

This satisfies UX-Plan-V2's "avoid style customization / prefer native components" as far as it can
while still achieving the density. Where the two genuinely conflict, density wins — decided
knowingly, not by default.

### 2. Field anatomy (the core decision)

Every input in the app follows one shape:

```
Label * ⓘ      [ value                    ]
                 helper text
```

- **Label** — left of the field, in its own column
- **`*`** — required, red
- **ⓘ tooltip** — *why* this field exists / what it affects (`MudTooltip` + `MudIcon`)
- **Helper text** — *how* to fill it: format, constraint, example (`HelperText`)

The tooltip/helper split is load-bearing: two distinct jobs, so neither crowds the other, and the
format guidance is **always visible** rather than hidden behind hover (which does not exist on
touch). Prefer **examples over definitions**.

Today `HelperText` is used **4 times in the entire app** against 22 `Label`s. That ratio is the
work.

### 3. Grouping

**Tab architecture is preserved** (General / Tags / Dependencies / Dependent On / Review).
*Within* each tab, fields group under a **section heading + one-line description**:

```
Identity
How this resource is uniquely identified.

  Subscription * ⓘ  [ prod            ▾ ]
                      Locked after save. Change via Rename.
```

This creates a real 3-level hierarchy — heading → description → field label → helper — which
**resolves PL-31 and PL-32 as a side effect** (today all four panels use identical
`Typo.body2` + `Color.Secondary`, so subtitle and field label carry the same weight).

### 4. Density — "Compact" (selected)

| Property | Value |
|---|---|
| Label column | **128px** |
| Field height | **~30px target** |
| Label / field text | ~11.5px / 12.5px |
| Helper text | ~10.5px |
| Row gap | ~7px |

### 4a. MEASURED — real MudBlazor numbers (2026-08-03)

Measured on a real component page (`/uxlab`, `UxLab.razor`) at 1500px. **Two design assumptions
were wrong; these numbers supersede the mockups.**

| Probe | Height |
|---|---:|
| Outlined + `Margin.Normal` (today) | **56px** |
| Outlined + `Margin.Dense` | **40px** |
| Dense, no floating label | **40px** |
| Dense + `--mud-typography-input-size: .78rem` | **40px** (variable had **no effect**) |
| `MudSelect` Dense | **40px** |

**Finding 1 — the dense floor is 40px, not 30px.** Field height is padding-driven, not
font-driven, so no layer-1/2/3 lever moves it. **Decision: accept 40px.** Do not override
`.mud-input` internals to chase 30px.

**Finding 2 — the compact single-column form is TALLER than today, not shorter.**
Measured: proposed **456px** vs today **266px** (+71%) for the same five fields. The mockup only
looked denser because it drew 30px fields and tighter gaps than Mud produces. Cause: single column
stacks 5 rows instead of 3, and every ⓘ + helper line adds height.
**Decision: accept the height.** The guidance is the point, the fixed shell scrolls, and Azure's
own blades are tall. But state it plainly — this design trades vertical space for clarity.

### 4b. Vertical compression (decided, verify during implementation)

Tighten the label → field → helper rhythm:

- **Layer 2 (free):** label `margin-bottom: 1px`, `line-height: 1.2`; reduce `.rm-form-row` gap
- **Layer 4, margins only (accepted exception):** `.mud-input-control { margin-bottom: 0 }` and
  `.mud-input-helper-text { margin-top: 1px; line-height: 1.25; font-size: .7rem }`

This is the low-risk end of layer 4 — margin-only overrides on two well-known classes. A MudBlazor
upgrade shifts spacing at worst; it cannot break layout. Documented here so the exception is
deliberate rather than accidental. Verify the rendered result during implementation.

### 5. Page shell — "Azure footer" (selected)

Fixed chrome, single scrolling region:

```
┌─ Home › Resources › Orders API        ← breadcrumb   (fixed)
│  Orders API                           ← page title   (fixed)
│  General | Tags | Deps | ... | Review ← tab strip    (fixed)
├───────────────────────────────────────
│  … form content …                     ← ONLY this scrolls
├───────────────────────────────────────
│ [Review + Save] [‹ Previous] [Next : Tags ›]      [Cancel]   (fixed)
└───────────────────────────────────────
```

- **Only the content region scrolls** → fixes **PL-34**
- **Breadcrumb** → fixes **PL-08** (no way back to the grid) more naturally than a Home menu item
- **Footer**: primary `Review + Save` always reachable from any tab; `‹ Previous` disabled on the
  first tab; **`Next : <NextTabName> ›` names its destination** → **PL-15**
- Prerequisite: `@bind-ActivePanelIndex` on `MudTabs` — nothing drives the tabs programmatically today

**Known trade, accepted:** Azure's bar belongs to a *create wizard*; this editor is mostly used to
*edit*, where bottom-right Save is the stronger convention. Chosen anyway for consistency with the
reference. **Check the committed Playwright specs** — they may target the current Save position.

### 6. Field order

General tab becomes **single column**, ordered **Subscription → Type → Key**, then Name,
Description (**PL-30**). This matches the Identity preview's own order (`prod / Azure App Insights`),
so the fields finally read in the same order as the value they compose.

### 7. Scope — all surfaces

Maximum consistency (Steve: *"let's be consistent as possible"*).

| Surface | Treatment |
|---|---|
| `/resources` editor + all dialogs | **Full** — anatomy, sections, density, shell |
| `/` grid page, `/import`, `/explore` | **Shell** — breadcrumb, fixed title, scrolling body, footer where applicable |
| Grid **table** | Density + type scale to match; keep Azure-DevOps-list behaviour |
| Explorer **canvas** | **STAGED LAST** — see below |

**Explorer canvas sequencing (important):** its node rendering is currently **broken**
(PL-37 — icon and short code drawn superimposed) and an agent is actively editing
`explorer-canvas.js`. Restyling a canvas whose nodes don't render correctly repeats the PL-38 trap
of designing around a defect. **Do the canvas only after PL-37/PL-39 are fixed and browser-verified.**
Its *page chrome* is not blocked and ships with everything else.

### 8. Responsive

Below ~900px the label column **stacks above the field** (labels cannot survive a 128px column on a
narrow screen). This is separate from **PL-12**, which is about the *grid table's* density collapse
and hover-only row actions — that stays in B10.

## Implementation order

1. **Theme + primitives** — `MudTheme.Typography`/`LayoutProperties`; `.rm-form-row`, `.rm-section`,
   `.rm-page-shell` CSS. **Measure the real field-height floor here** and record it.
2. **Page shell** — breadcrumb, fixed title/tabs, scrolling body, footer + `@bind-ActivePanelIndex`
   (PL-34, PL-08, PL-15).
3. **General tab** — first full application: single column, new order, sections, anatomy (PL-30).
   Validate the system on one tab before spreading it.
4. **Remaining editor tabs + dialogs** — Tags, Dependencies, Dependent On, Review (PL-31, PL-32).
5. **Copy pass** — fill every ⓘ and helper slot (**B7**: PL-24, PL-23, PL-35, PL-07, PL-33).
   ~90 strings; do it as one inventory → one rewrite → apply.
6. **Other page shells** — grid, import, explorer chrome.
7. **Grid table density.**
8. **Explorer canvas** — only after PL-37/PL-39 are verified.

## Verification

- Drive every route in a real browser at **1600px and 900px**; the label column must stack cleanly
- Confirm only the content region scrolls — title, tabs and footer stay put on every page
- `dotnet test` — baseline 416 tests, 2 known pre-existing failures
- `cd tools/e2e && npm run test:e2e` — **specs are updated at the end, not kept green throughout**
  (Steve's call). Breakage during implementation is expected and is not a stop condition.
  **But run the suite once immediately after step 2 (page shell)** — the footer move is the single
  riskiest change for the specs, and one early run isolates the blast radius to one change instead
  of discovering it tangled with twelve. Specs appear to locate by class (`.rm-deps-picker`,
  `.rm-dep-create-new`) rather than text or position, so they may survive more than expected — the
  early run is how we find out. Final gate: 8/8 green again before merge.
- Record the measured native field-height floor in this doc so the layer-4 decision is evidence-based
- Screenshot before/after of the General tab as the density record

## Out of scope

- Deployment/hosting (v1 is the codebase)
- Explorer canvas node/edge *semantics* — that's B6, not this
- Grid table behaviour (sorting, filtering, server paging) — visual density only
- PL-12's hover-only row actions on touch — stays in B10
