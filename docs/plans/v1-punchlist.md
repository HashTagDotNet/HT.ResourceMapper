# HT.ResourceMapper — v1 Punchlist

> **This is the authoritative to-do list to v1.** It supersedes the "outstanding" sections of
> `filter-plan/RESUME.md`, `explorer/00-implementation-plan-list.md`'s resume marker, and
> `__ProjectNotes/SessionResumeContext.md`. Captured 2026-08-02.

## Context

All three feature builds are complete and pushed: home grid + filters, editor (slices 1–9),
explorer (slices 1–10 + UI-polish passes 1 & 2). `home-page` is **134 commits ahead of `main`**
and `main` holds nothing `home-page` lacks — so the app is not unfinished, it is **unmerged**,
and carrying a punchlist of real-use defects.

**Bar: v1 complete, not a POC.** Deployment/hosting is explicitly out of scope — v1 is the codebase.

**Standing prioritisation rule (Steve):** *be aggressive on usability.* Usability items are v1
requirements here, not polish. They are the first things cut under time pressure, so they are
marked ⚑ below and must be cut **knowingly**, not by default.

**How to read Steve's items:** his walkthrough comments are **descriptive, not prescriptive** —
they state the problem observed, not the required solution. The problem is authoritative; proposed
fixes are proposals and placement/design details are TBD until the fix phase.

## Status of this list

| Source | Count |
|---|---:|
| Steve's walkthrough (PL-07 → PL-44) | 30 |
| Audit-sourced (PL-A1 → PL-A10) | 9 |
| Pre-existing, verified (PL-01 → PL-06) | 6 |
| **Fixed during capture** (PL-25a, PL-26) | 2 |

**Verified this session:** 416 unit tests — 2 fail (both pre-existing, PL-01), no new failures.
Playwright regression suite — **8/8 pass**. `.sqlproj` cannot build under `dotnet` CLI (MSB4278,
needs full MSBuild/VS) — documented, not a defect.

---

## v1-BLOCKING

Ship-stoppers. Both are small.

| id | Item | Where |
|---|---|---|
| **PL-22** | **Cancel leaves a permanently stuck loading bar over a blank page.** `_ = LoadAsync(...)` fire-and-forget from a *synchronous* `void Cancel()`; `finally` clears `_loading` but never calls `StateHasChanged()`, so nothing repaints. Dead end — user must reload. Fix: `async Task Cancel()` + `await`. | `ResourceEditor.razor:288-292, 297-315` |
| **PL-42** | **Edges between two on-canvas neighbours are never drawn.** `Resource_GetForExplorer` returns only edges *incident to the center*. Confirmed: seed `DEMOEXP-checkout` shows Pricing Service and Inventory Service both on canvas with `Pricing Service -> Inventory Service` in the data and **no edge rendered**. For a dependency map this is a correctness defect — the picture asserts "unrelated" when they are related, with no signal of omission. | sproc + `ExplorerService` + `explorer-canvas.js` |

## SECURITY

| id | Item | Where |
|---|---|---|
| **PL-A8** | **XSS via shared diagram name.** `printDiagram()` writes the user-supplied diagram name into `<title>` unescaped. Vector is not self-harm — slice 7b shares diagrams, so a name like `</title><img src=x onerror=…>` executes in the *recipient's* browser when they print. `escapeHtml` already exists in the same file (`:620`) and `svgWithHeader` uses it, so this is an oversight. One-line fix. | `explorer-canvas.js:590` |

---

## Batches

Grouped by **touched file/pattern, not by issue** — the single biggest speed lever. Ten tweaks to
one file is one work item. Batches marked ⚑ are usability-critical per the standing rule.

### B1 — `ResourceEditor.razor` Cancel path · **S**
Same method, same file. Fixing separately means opening this code twice.
- **PL-22** stuck loading bar *(v1-blocking)*
- **PL-27** Cancel discards silently — should confirm only when changed. **Wiring, not building:**
  `IsDirty` (`ResourceEditorModel:25`), `UnsavedChangesDialog.razor` (already 3-way Cancel/Discard/Save),
  and `OnLocationChanging` (`:621+`, already gates on `!IsDirty`) all exist. `Cancel()` bypasses them
  in all 3 branches. The persisted branch never navigates, so it needs an explicit check.

### B2 — Missing re-render / silent async failures · **S**
One bug class, four sites. Established by two confirmed instances.
- **PL-A1** "Showing 0 of 0" while grid is full — `LoadGridData` sets `_items`/`_total` with zero
  `StateHasChanged()`; sibling `ResourceFilterBar` keeps stale values (`Home.razor:246-282`)
- **PL-A9** export failures swallowed (`savePng` `catch { /* ignore */ }`, `copyPng` `catch { return false }`)
- **PL-A10** `_ = PersistViewAsync(...)` fire-and-forget — view settings silently stop persisting (`Home.razor:314`)
- *(PL-22 is the same class but lives in B1)*

### B3 — Sticky selectors · **S**
Sweep is **complete and bounded**: `grep 'Value="null"'` finds exactly 2 sites, no others.
- **PL-16** add-tag autocomplete (`TagsTab.razor:64`) and dependency picker (`ResourcePicker.razor:11`)
  both retain the last-picked item's text. **Fix already exists in-repo:** the `@key`-increment remount
  in `TagValueEditor.razor` (`:16`, `:69`, `:107`). Documented in `tools/e2e/README.md` gotchas —
  fixed once, missed twice. Dependencies picker needs no repositioning; clearing suffices.

### B4 — ⚑ Editor layout & hierarchy · **M**
All in the editor's chrome; converging on the same Azure-blade shape.
- **PL-34** inner panel should scroll inside fixed chrome. **Pattern exists:** `.rm-page` is documented
  as *"Home page: filter bar (static) stacked above a grid that fills the rest and scrolls"* — the
  editor just uses `.rm-main { overflow-y:auto }` and scrolls everything (`app.css:15-21`)
- **PL-15** Azure-style Next/Previous wizard nav. Prereq: `@bind-ActivePanelIndex` on `MudTabs`
  (nothing drives the tabs programmatically today). Copy the specifics: sticky footer; `< Previous`
  disabled on step 1; `Next : <NextTabName> >` labelled with the destination; primary action always
  available. *Assumption: Create/wizard mode only; Edit keeps free tab-clicking — confirm.*
- **PL-30** General tab → Subscription → Type → Key, single column. Matches the Identity preview's
  own order (`prod / Azure App Insights`). Name/Description placement TBD
- **PL-31** panel subtitles indistinguishable from field labels — all four tabs use identical
  `Typo.body2` + `Color.Secondary`; one type decision covers all four sites
- **PL-32** Review sub-section headers read as data rows (`<th>` with no fill/weight)

### B5 — ⚑ Tag editor · **S/M**
- **PL-17** row controls not vertically centred
- **PL-18** "add" affordances inconsistently placed/labelled across tabs (tags + dependencies).
  **Problem is authoritative; placement TBD.** "CREATE NEW…" already does nested-create (slice #9),
  so this is likely relabel/reposition, not new functionality
- **PL-20** add-tag dropdown empty → **hide the control when there are no candidates** (Steve).
  Impl note: candidates are computed only inside `SearchAddableTags`, so hiding needs the filter
  hoisted to a computed `HasAddableTags`. **Do not "fix" by unfiltering** — multi-value lives
  *inside* a row as chips (`IsMultiValued` → `MudChipSet`), so unfiltering creates duplicate rows
- **PL-19** flow "doesn't feel fluid" — **re-evaluate after the above three**; don't design against
  it in isolation

### B6 — Explorer canvas · **M**
Order matters here.
1. **PL-37** icon and short code are both drawn dead-centre, superimposed (`:87`, `:91-92`, `:99`) —
   breaks pass 2's premise that "the code is always visible". Data is fine (`SVC`, `DB`, `APP`…)
2. **PL-39** Re-tidy produces a meaningless layout. `breadthfirst, directed:true, roots:seed` walks
   *outgoing* edges, but **the seed is a sink** (`:193` — "edges leading INTO the seed"), so BFS
   reaches nothing and everything is dumped in a row
3. **PL-40** no visible arrows — **likely a symptom of PL-39** (arrows *are* configured); re-verify after
4. **PL-44** grid-open vs collapse-then-expand differ — two placement paths; `collapseAll()` fits on
   a single node (→ `maxZoom 3`) then expansion places parent-relative. **Sequence after PL-39**
5. **PL-38** neighbour node redesign (rounded rects, name + type) — **GATED on PL-37.** ⚠ This reverses
   pass 2's deliberate fix for label overlap; the screen may be unreadable only because the codes are
   illegible. Re-evaluate after the overlap fix
6. **PL-42** missing neighbour edges *(v1-blocking, listed above)*
7. **PL-41** dashed/solid legend. Semantics: dashed = depends-on-the-seed, solid = seed-depends-on.
   **Must composite into exports** — slice 9d already proved `cy.png/svg` capture the graph only,
   not HTML overlays. Reuse 9d's compositing path
8. **PL-A8** print XSS *(security, listed above)*
9. **PL-A9** silent export failures *(also in B2)*
10. **PL-43** diagram management: Delete of the *current* diagram is missing (exists only per-row in
    Open-Recent); toolbar mixes lifecycle vs view concerns across 8 ungrouped controls. *"New" dropped from scope.*

### B7 — ⚑ Copy & clarity (cross-cutting) · **M**
**Rationale — record this, it's why it isn't cosmetic:** *"Users will not frequent these pages"* —
no familiarity accrues, every visit is effectively a first visit.
**Sized: ~90 user-facing strings** (22 `Label`, 27 `Text`, 20 `aria-label`, 10 `Placeholder`,
5 `Title`, 6 help) + toast/error text. Telling ratio: **4 `HelperText` against 22 `Label`s**.
**Approach: one string inventory → one rewrite pass → apply**, not screen-by-screen.
- **PL-24** the pass itself
- **PL-23** create-tag dialog is the worst-affected screen. Fix the jargon, don't just fill gaps:
  "Requirement Level → **Error**" is developer vocabulary; "Multi-valued (multiple discrete values)"
  is circular; "Display Order" unexplained. **"Allow custom value" × "Allowed Values" is genuinely
  ambiguous** — a conceptual decision, then wording. `MudCheckBox` has no `HelperText`
- **PL-33** same field is "Domain" on Review/tooltip but "Subscription" on General — 3 sites
- **PL-35** primary-tag star has no tooltip/title/aria-label. *Steve designed it and couldn't recall
  what it meant* — also an a11y gap
- **PL-07** link row-action icon has no tooltip; the other 3 do. Two states, two fixes: disabled
  button needs a wrapper; the populated state is a `MudMenu` opening on **hover**, so a hover tooltip
  would fight it

### B8 — ⚑ Groom selectors (cross-cutting) · **M**
- **PL-21** Steve's rule: *"if no other tags are available I should not have a tag selector… groom so
  only valid items are available."* Controls with no valid choices hidden, not shown empty
- **PL-04** *verified still true* — `GetAllTagKeysAsync` (`:156-165`) applies **no `IsSystemTag`/
  `IsDomainTag` filter**. Impact today is one tag (`Domain`), but with no filter any future system tag
  leaks automatically
- Sweep: add-filter menu, tag selectors, vocab selects, dependency picker, explorer presets

### B9 — Identity & rename · **M**
- **PL-13** move identity editing behind an explicit **Rename dialog** (Name + Key + Subscription;
  Rename/Cancel). **Supersedes** `resource-logical-model-and-editor-ux.md:497` ("Key editable + validated").
  Inline Name/Key become read-only; Subscription stays frozen inline; the dialog is the single guarded
  path. Renaming is **safe by design** (edges are by `ResourceId`; import realigns to the current key),
  so no cascade work. *Consequence: Name becomes dialog-only despite being the most-edited field.*
  Verify `Resource_CheckUnique` excludes self
- **PL-14** design doc stale on post-save mutability — **resolved: code is right**. Update to: Type
  permanently frozen; Name/Key/Subscription changeable **only via Rename**. Absorb PL-30's ordering
  change too, or the doc contradicts the build again. **Same batch as PL-13**

### B10 — Grid & navigation · **S/M**
- **PL-08** no way back to the grid — add Home to the app menu + make the title clickable
  (`MainLayout.razor:11-17`; menu has only Create Resource + Import, title is plain `MudText`, no
  `NavMenu` component exists)
- **PL-10** no menu to manage active filters (edit/remove individually, clear-all) — chips only
- **PL-36** URLs should render as active `_blank` links. Inconsistency, not a feature — `Home.razor:51`
  already does it; `RowValueDisplay` ignores `ContentType`. **Must ride along:** reuse the existing
  scheme guard (`EditorValidation.cs:94`) since tag values are user-supplied, and add
  `rel="noopener noreferrer"` (MudBlazor won't)
- **PL-12** small screens: no horizontal overflow at 1366/1024/768 — it degrades by **density collapse**.
  (a) Description wraps to 5–6 lines → ~6 rows visible vs ~22; (b) Tags column reserves width while
  empty; (c) row actions are hover-only (`app.css:79-89`) so **unreachable on touch** — (c) is a
  separate fix

### B11 — ⚑ Azure visual language (design-led) · **L**
- **PL-11** Reference is the Azure "Create Web App" blade. The dominant driver of "bootstrap-ish" is
  the **outlined-box + floating-label** treatment; Azure's ~200px label-beside-field grid reads far
  denser at the same font size. Also: section headings + descriptive text, ~32px fields, underline tabs,
  breadcrumb + large title, sticky footer, ⓘ icons.
  **⚠ CONSTRAINT CONFLICT — decide before building:** UX-Plan-V2 says *"AVOID style or behavior
  customization; prefer native stack components."* A 2-col label grid **is** MudBlazor customisation.
  "Azure-inspired" and "avoid customisation" cannot both hold at full strength.
  **Needs a design pass (brainstorming) before implementation.** Absorbs B4's items.

### B12 — New surfaces (gaps) · **L**
- **PL-28** **Resource Type CRUD screens.** Better than it looks: `ResourceType_Upsert` **already
  exists**, reachable only from import (`ImportSqlRepository.cs:61`) — so this is service + UI, **not
  DB work**. Must cover `ShortCode`/`IconKey` (they drive explorer rendering) and the `ResourceTypeTag`
  entry-point template. Delete needs a dependents check. *Needs a design pass.*
- **PL-29** inline "add new…" on type selectors — mirrors `TagsTab`'s existing inline-create pattern.
  **Depends on PL-28**
- **PL-09** **catalog export (JSON, import round-trip).** Design already settled —
  `ImportExportApiDesign.md` #4/#5 fix the format; **zero implementation exists**.
  **Open decision:** that doc specifies `POST /api/resources/export`, but the app's current rule is
  in-process services / no HTTP controllers

### B13 — Hygiene · **S**
- **PL-01** 2 failing tests (`NotFound`→404 mapping) — verified, pre-existing, no new failures
- **PL-A5** **`ResourceTag` has no unique constraint on `(ResourceId, TagDefinitionId)`** — only the
  clustered PK on identity. Single-valuedness (incl. the required Domain tag) is enforced **purely in
  app code**; nothing at the DB level prevents duplicates
- **PL-A6** demo grid seed regenerates `ResourceUid` via `NEWID()` every run — invalidates every
  `/resources/{uid}` permalink for those 75 rows. `DEMOEXP-*` are stable
- **PL-A3** console 404 on first page load only; did not reproduce. **Pin down, don't guess**
- **PL-A7** Key shows red "Required" on an apparently untouched Create form while empty Name isn't
  flagged. **Unconfirmed** — reproduce on a fresh `/resources` load
- **PL-A2** "Azure Servcie Fabric" typo in resource-type data (multiple rows)
- **PL-06** supersede the stale resume docs by pointing them here

### B14 — Ship · **S**
- **PL-05** merge `home-page` (134 commits) → `main`. Verify it merges clean before opening the PR

---

## Already fixed during capture

| id | Item |
|---|---|
| PL-25a | Demo grid seed assigned no Domain tag → 75 resources couldn't take dependencies. Now an idempotent `MERGE` scoped to ids -1..-75, resolving the definition via `IsDomainTag=1`. Verified: 85 total, 0 missing, 30 non-prod / 55 prod, 0 duplicates. **Uncommitted.** |
| PL-26 | Same script opened with an **unqualified `DELETE HTResourceMapper.Resource`** — wiped the explorer dataset and would hard-fail on `NO_ACTION` FKs; its "IDEMPOTENT" header was false. Removed; two consecutive clean runs verify. **Uncommitted.** |
| PL-25b | *Remaining:* Dependencies tab says "Choose a Subscription on the General tab first" while that field is disabled — the app instructs an action it forbids. Defensive fix still open. **S** |

---

## Recurring theme — worth an explicit sweep

**Good patterns are established once and applied inconsistently.** Four instances surfaced without
looking for them:

| Pattern | Solved in | Missing in |
|---|---|---|
| `@key` remount for sticky selectors | `TagValueEditor.razor:16` | 2 sites (PL-16) |
| `MudTooltip` on row actions | 3 of 4 in `Home.razor` | link icon (PL-07) |
| `.rm-page` static-header/scrolling-body | Home page | editor (PL-34) |
| `StateHasChanged` after async mutation | most handlers | 2 sites (PL-22, PL-A1) |

Each is small **because the answer already exists in the repo**. A deliberate consistency sweep is
likely higher yield per hour than fixing these one at a time.

---

## Browser verification pass — 2026-08-03

Driven live at 1600×1000 against the merged build. All 5 routes returned 200.

**Confirmed fixed (seen, not inferred):**

| Item | Evidence |
|---|---|
| PL-A1 | Header reads **"Showing 85 of 85"** against 85 rendered rows |
| PL-08 | Title is an `<a href="/">`; Home + Resource Types in the menu |
| PL-37 | Node icon now sits **above** the short code — `APP`/`SVC`/`DB`/`SBQ` all legible, no overlap glyph |
| PL-42 | **Dashed edges now render between two non-seed nodes** on `/explore/DEMOEXP-checkout` — exactly the neighbour-to-neighbour case that never drew before |
| PL-41 | Legend ("Focus depends on" / "Depends on focus") renders inside the canvas, so it composites into exports |
| PL-39 | No longer one degenerate row — sources top, seed mid, sinks bottom |
| **PL-40** | **RESOLVED — was never a styling bug.** Arrowheads are clearly visible on every edge. It was a PL-39 layout artifact. The agent was right to refuse to call it from code alone. |
| PL-43 | Delete control present in the explorer toolbar |
| PL-25a | Domain renders on every grid row with a real prod/non-prod mix |
| PL-28 | `/resource-types` lists 12 types with live node previews, usage counts, delete disabled while in use |

**PL-20 — earlier diagnosis was WRONG.** I attributed the empty add-tag dropdown to slice #7
pre-seeding every applicable entry-point row. Measured reality:

```
tagDefinitions = 2      Domain (system+domain, correctly filtered out)
                        DemoExplorerPrimaryUrl
resourceTypeTag rows = 0
```

Nothing is pre-seeded — there are **zero** type templates. The dropdown is empty because the catalog
contains exactly **one** usable tag definition, and once it is on a resource there is nothing left to
offer. The PL-20 fix (hide the control when there are no candidates) is still correct behaviour, but
it was fixing a symptom of empty data, not of over-seeding.

### PL-45 — seed data does not exercise the app's headline feature ⚑

**2 tag definitions and 0 type templates** across the whole catalog. Tagging is the product's core
value proposition (UX-Plan-V2: *"Tags provide a mechanism for associating meta data with a
resource… Tags can have links"*), and the demo data barely touches it:

- multi-valued tags, controlled vocabularies, `AllowCustomValue`, Link-vs-Text content types, and
  primary-tag selection are all **undemonstrable and effectively untested** against real data
- the `ResourceTypeTag` entry-point template feature has **never been exercised** — 0 rows
- a demo of this app cannot show its main differentiator over the wiki it is meant to replace

This is the seed-currency rule biting again, the same class as PL-25a: seed scripts that don't
produce data the app can actually be judged on. Extend the demo seed with a realistic tag
vocabulary (owner, environment, cost-centre, runbook link, on-call) plus per-type entry-point
templates. **S/M, and it gates any credible demo.**

### PL-49 — import silently resets tag-definition flags, clearing `IsDomainTag` ⚑

**Data-integrity defect, found while building export.** `ImportSqlRepository.UpsertTagDefinitionAsync`
does not pass `@DisplayName`, `@RequirementLevel`, `@IsDomainTag`, `@IsSystemTag` or `@DisplayOrder`.
On the **update** path `TagDefinition_Upsert` therefore resets them to defaults — including
**`IsDomainTag = 0`**.

The Domain tag is the identity anchor: resource identity is `(Domain + Type + Key)`. Clearing that
flag breaks identity resolution catalog-wide, and nothing in the UI would show it happening.
Reachable today by importing any document containing a `tagDefinitions` section that names an
existing tag.

Export ships its `tagDefinitions` section **opt-in** specifically to avoid tripping this, so the
workaround is in place but the bug is live. Fix the repository call to pass all five, and add a
guard so `IsDomainTag`/`IsSystemTag` can never be cleared by import. **S — but high severity.**

### PL-50 — cross-domain dependency edges cannot round-trip

`ImportService.ValidateAndResolveDependencies` resolves a bare dependency key **scoped to the
dependent's own domain**, and the import format has no syntax for a cross-domain target. Two of the
20 demo edges are cross-domain, so export omits them (with a warning) rather than emitting something
that would fail validation and abort the whole import.

Import is additive for relationships, so re-importing never *deletes* the live edges — but a
backup/restore built on export would lose them. Needs a format addition (e.g. a qualified
`domain/key` target). **M**

### PL-51 — `ImportExportApiDesign.md` has drifted from the shipped import

The doc is no longer a reliable spec. Confirmed drift: identity is `(Domain+Type+Key)` not bare
`key`; `type` is required (doc said optional); `contentType` is a constrained `Text|Link` set (doc
decision #13 said free-form with auto-registration); the summary has a 4th `resourceRelationships`
section; and `ResourceDependency_SetForResource`, `Resource_GetKeysByKeys` and the `ResourceKeyList`
TVP described in the doc were never built. Its controller premise also targets
`UI/ResourceMapper.UI.Server` hosting Blazor WASM — a project that no longer exists.

`ImportService.cs` is the de-facto source of truth. Either update the doc or mark it historical.
Leaving it as-is will mislead the next person. **S**

### PL-46 — two tag definitions share the DisplayName "Portal URL"

Introduced by PL-45 and confirmed in the live DB (`duplicateDisplayNames = 1`):

| Key | DisplayName | Owner |
|---|---|---|
| `DemoExplorerPrimaryUrl` | Portal URL | explorer seed (`Demo_Insert_ExplorerGraph.sql`) |
| `PortalUrl` | Portal URL | new vocabulary seed |

Keys differ so the DB is satisfied, but the **editor labels both rows identically** — a resource
carrying both would show two indistinguishable "Portal URL" rows. The seed sidesteps it today by
skipping `PortalUrl` on the 8 resources that already hold `DemoExplorerPrimaryUrl`, so no resource
currently has both. That is a workaround, not a fix: a user adding Portal URL to any of those 8 hits
it immediately.

**Resolution: retire `DemoExplorerPrimaryUrl`** and migrate its 8 resources (and their
`PrimaryTagDefinitionId`) to `PortalUrl`. It exists only because the explorer seed predated a real
vocabulary. Touches `Demo_Insert_ExplorerGraph.sql` and needs a data migration for the 8. **S**

### PL-47 — grid row height explodes now that resources actually have tags ⚑

Confirmed live after PL-45. Tags stack vertically in the Tags column, so a resource with
Domain + Owner + PortalUrl + 2× Runbook renders ~5 lines. Rows went from ~39px to ~100–200px and
**visible rows dropped from ~22 to ~7** at 1600×1000.

This was invisible before PL-45 because almost nothing was tagged — the thin seed was masking a
real density problem. It partially undoes PL-12 and contradicts UX-Plan-V2's "visually light weight
and compact / visual focus on data".

Options (design call, not decided): cap visible tags per row with a "+3 more" affordance (note
`Resource_GetItems` already has a `@TagLimit` param); render tags as compact inline chips rather
than stacked lines; move non-Link tags out of the grid into the detail view; or make the Tags column
opt-in per user. **Do not solve it by removing the seed data** — the data is correct; the display is
what does not scale. **M**

### PL-48 — grid shows internal TagKey instead of DisplayName

`Home.razor:51` binds `@tag.TagKey`, so the grid renders `PortalUrl`, `CostCenter`, `OnCall` where
the definitions carry the user-facing DisplayNames "Portal URL", "Cost Center", "On-Call Rotation".
The read model already carries what's needed — `TagDefinition_GetAll` COALESCEs
`DisplayName, TagDefinitionKey`, so the grid query should project the same. Same
internal-vocabulary leak as PL-33 (Domain vs Subscription); fold into the PL-24 copy pass. **S**

## Coverage gaps in this capture

Findings here are only as good as what was exercised. Not covered:

- **`/import`** — never driven. Code review was clean (20MB limit set, errors caught and surfaced),
  but it was never used
- **`/explore/shared/{id}`** — read-only share + save-a-copy never exercised
- **Grid filters / search / sort / Back-Forward** — inspected visually; behaviour never driven.
  `filter-plan/RESUME.md`'s verification list (facet counts shifting, two-tag-key AND, NotEquals,
  `(blank)` bucket) remains **unverified**

A short second pass on `/import` before locking scope is worthwhile.

## Effort and the cut list

**Unit: traditional human developer-hours** (S ≤30min · M ≤2h · L >2h), assuming a focused session
with the app already running. See "Agent-driven re-cast" below — this project is built agentically,
so these numbers are **not** the hours Steve will actually spend.

| Batch | Effort | ⚑ |
|---|---:|---|
| B1 Cancel path | 0.5h | |
| B2 Missing re-render / silent failures | 1.0h | |
| B3 Sticky selectors | 0.5h | |
| B4 Editor layout & hierarchy | 4.0h | ⚑ |
| B5 Tag editor | 2.5h | ⚑ |
| B6 Explorer canvas | 6.0h | |
| B7 Copy & clarity | 4.0h | ⚑ |
| B8 Groom selectors | 2.0h | ⚑ |
| B9 Identity & rename | 3.0h | |
| B10 Grid & navigation | 3.0h | |
| B11 Azure visual language | 8.0h | ⚑ |
| B12 New surfaces (type CRUD, inline add, export) | 11.5h | |
| B13 Hygiene | 2.5h | |
| B14 Ship | 0.5h | |
| **Total** | **≈49h** | |

**This needs saying plainly:** UX-Plan-V2 set a target of *"a week-end (20-30 hours of human time)"*
for the whole project. This punchlist **alone** is ~49h — roughly double that, on top of everything
already built. Scope has to give somewhere, and it's better decided now than discovered at hour 30.

### Cut list — three shapes, pick one

**A. Ship the codebase (≈16h).** B1, B2, B3, B6, B13, B14 + PL-A8. Fixes every v1-blocking defect
and the security bug, merges to `main`. Leaves the app functionally correct but with the usability
problems intact — which **directly contradicts "be aggressive on usability"**. Only choose this if
shipping the merge matters more than the experience.

**B. Correct + usable (≈29h).** A, plus B4, B5, B7, B8. Every ⚑ batch except B11. Closes the
usability complaints that drove this walkthrough, honours the standing rule, and lands near the
original weekend budget. **Recommended.**

**C. Everything (≈49h).** Adds B9, B10, B11, B12. B12 alone is 11.5h of genuinely new surfaces
(type CRUD, export) — those are *features*, not punchlist items, and are the cleanest thing to
defer to a v1.1 without weakening v1.

The honest read: **B12 is not punchlist work** and B11 is a design project wearing a punchlist
label. Cutting both is what makes B viable — and neither is a defect, so nothing ships broken.

### Agent-driven re-cast (the number that actually matters)

The table above is human developer-hours. This project is built agentically, so the operative
budget is **Steve's wall-clock time** — prompting, reviewing, deciding, verifying — which compresses
very unevenly:

| Work type | Compression | Why |
|---|---|---|
| Mechanical / pattern application (B1, B2, B3, B13) | High | The answer already exists in-repo; agent applies it, Steve skims the diff |
| Diagnostic (B6 explorer layout) | Moderate | Iterative and visual — needs eyes on renders between attempts |
| Design / judgement (B4, B5, B7, B11) | Low | Bounded by the **review loop**, not by typing speed |
| Verification by eye | ~None | Irreducible |

**Measured data point, this session:** the PL-25a + PL-26 seed fix was ~1–1.5h of traditional work
(understand schema, author an idempotent MERGE, verify re-runnability, catch the latent unqualified
`DELETE`). Agent wall-clock: **3.5 minutes**, plus ~5 min verification and review. ≈**10×** — on the
most favourable kind of task: crisply specified, mechanically verifiable, no aesthetic judgement.
**Do not extrapolate that factor to B11.**

**Option B re-cast ≈ 10–13 hours of Steve's time:**

| | Steve-hours |
|---|---:|
| Mechanical batches (B1, B2, B3) | 1.0–1.5 |
| Explorer canvas (B6) — visual iteration resists compression | 2.0–3.0 |
| Design-led usability (B4, B5, B7, B8) — dominated by review | 5.0–7.0 |
| Hygiene + ship (B13, B14) | ~1.0 |
| **Total** | **≈10–13h** |

That lands **inside** the original 20–30h weekend budget rather than at double it.

**The floor is review, not implementation.** This capture session alone ran a couple of hours to
produce 30 findings — and that was Steve talking, with no code written. Design batches have the same
shape. Adding agents does not move that floor.

## Open decisions (block their batches, not the list)

1. **PL-11** — how far to take Azure styling against "avoid customisation"?
2. **PL-09** — HTTP controller (per the design doc) or in-process service (per current convention)?
3. **PL-15** — wizard nav in Create mode only, or Edit too?
4. **PL-18** — relabel/reposition, or an additional affordance?
5. **PL-20** — was same-tag-twice-as-separate-rows ever intended? (contradicts the chips design)

## Verification

- `dotnet test` — expect 416 tests, 2 known failures, no new ones
- `cd tools/e2e && npm run test:e2e` — expect 8/8
- Drive the app: `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5200" dotnet run --project UI/ResourceMapper.UI.Web --no-launch-profile`
- **Stop the app before building** — it locks build output
- Screenshot driver: `tools/e2e` helpers; from Git Bash use `MSYS_NO_PATHCONV=1` or a `/` route
  argument gets rewritten into a Windows path
- DB checks: `sqlcmd -S '(localdb)\MSSQLLocalDB' -d ResourceMapper` with `SET QUOTED_IDENTIFIER ON;`
  — **not** the SQL MCP
