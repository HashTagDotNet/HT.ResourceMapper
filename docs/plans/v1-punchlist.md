# HT.ResourceMapper — v1 Punchlist

> **This is the authoritative to-do list to v1.** It supersedes the "outstanding" sections of
> `filter-plan/RESUME.md`, `explorer/00-implementation-plan-list.md`'s resume marker, and
> `__ProjectNotes/SessionResumeContext.md`. Captured 2026-08-02, **re-triaged 2026-09-13** —
> start at *Authoritative status*, not at the batch write-ups further down.

## Context

All three feature builds are complete and pushed: home grid + filters, editor (slices 1–9),
explorer (slices 1–10 + UI-polish passes 1 & 2), and since then the vocabulary-management surfaces
(domains, tag dictionary, cascade deletes). `home-page` is **many commits ahead of `main`** and
`main` holds nothing `home-page` lacks — so the app is not unfinished, it is **unmerged**, and
carrying a punchlist of real-use defects. *(Check the gap with `git rev-list --left-right --count
origin/main...home-page` rather than trusting a number written here.)*

**Bar: v1 complete, not a POC.** Deployment/hosting is explicitly out of scope — v1 is the codebase.
*(Re-confirmed by Steve, 2026-09-13.)*

**Standing prioritisation rule (Steve):** *be aggressive on usability.* Usability items are v1
requirements here, not polish. They are the first things cut under time pressure, so they are
marked ⚑ below and must be cut **knowingly**, not by default.

**How to read Steve's items:** his walkthrough comments are **descriptive, not prescriptive** —
they state the problem observed, not the required solution. The problem is authoritative; proposed
fixes are proposals and placement/design details are TBD until the fix phase.

## Status of this list

> **RE-TRIAGED 2026-09-13 against the code.** The sections below the re-triage table — the original
> `v1-BLOCKING`, `SECURITY` and `Batches` write-ups — are **historical**. They record what was found
> on 2026-08-02 and why, which is still worth reading, but they are **not a to-do list any more**:
> most of their items have since been fixed, and three later feature commits are absent from them.
> **The re-triage table is the authoritative status.** Read it first.

### Why the list drifted

The list was last reconciled **2026-08-03**. Three substantial feature commits landed **2026-08-04**
and were never folded in — the domain-vocabulary CRUD stack, the tag-dictionary manager, and the
"remove everywhere" cascade deletes for tags / subscriptions / types. Meanwhile the Azure UX pass and
the sessions after it closed most of the batches without the headline sections being updated, so the
document's own `v1-BLOCKING` section listed defects that were **already fixed in code**.

The lesson worth keeping: **a punchlist that is appended to but never reconciled inverts — the top of
the document becomes the least accurate part of it.** Re-triage is cheap; reading a stale list as if
it were current is not.

### Re-triage method

Each item was checked against the source, not against memory. Two levers made this fast and should be
preserved:

1. **Fix sites carry their item id in a comment** (`// PL-A1: MudDataGrid repaints itself…`).
   Grepping the ids across `Modules/ UI/ Database/` finds most fixes in one pass. **Caveat, learned
   here:** a mention is *not* proof of a fix — two ids appear only in comments that *document the
   defect as still present* and work around it. Every hit was read in context.
2. Items with no annotation were verified by reading the named file, sproc or component.

Items whose verdict genuinely needs a running app are marked **needs eyes** rather than guessed at.

### Authoritative status

**Closed since capture — verified in code:** PL-04, PL-06, PL-07, PL-08, PL-09, PL-10, PL-11, PL-12,
PL-15, PL-16, PL-17, PL-20, PL-22, PL-23, PL-25a, PL-25b, PL-26, PL-27, PL-28, PL-29, PL-30,
PL-31, PL-32, PL-33, PL-34, PL-35, PL-37, PL-38, PL-39, PL-41, PL-42, PL-43, PL-44, PL-45 *(superseded by PL-61)*, PL-46,
PL-47, PL-48, PL-49, PL-52, PL-53, PL-54, PL-A1, PL-A8, PL-A9, and — settled in the browser pass
below — **PL-18** (add affordances are consistent) and **PL-A7** (not a defect: the red mark on Key
is the required-field asterisk, and Name genuinely is not required).

**Still open.** Nothing here is a ship-stopper: both items the old header called v1-blocking, and the
security item beside them, are fixed. Every item below has a verdict — **nothing is left unexamined.**
The items marked ⚑ are decisions Steve has taken, not defects.

_Widen the terminal if this renders as stacked Key: value records._

| id | Item | Verdict |
|---|---|---|
| **PL-64** | **Select lists need compact rows and must stop running off the screen** — the Add Filter menu now lists 19 tags, overflows the viewport, and shows internal tag keys rather than display names | **Open** ⚑ |
| **PL-63** | **Swap the single-owner identity for a real provider** — the `ICurrentIdentity` seam lands with Saved Views returning one configured owner (`anonymous`); a real IdP replaces that one implementation | **Roadmap** |
| **PL-62** | **Saved views need an on-screen "you are here" indicator** — Edge's filled star. Parked deliberately while building saved views | **Parked idea** |
| **PL-61** | **Replace simulated data with the real vNext Azure resource links** (wiki page 11783). Steve supplies the extraction — do not scrape it | **Decided — to do** ⚑ |
| **PL-58** | **Dependency diagram dropped from v1** (Steve, 2026-09-13). Hide the entry points; keep the code re-enable-able | **Decided — to do** |
| **PL-59** | Environment mismatch: app pinned `Development` while the machine runs `localhost` | **FIXED 2026-09-13** |
| **PL-56** | Grid link tags rendered with no `rel` and no scheme guard; import validates no URLs | **Open** |
| **PL-57** | Domains / Tags-manager / cascade-delete surfaces have no e2e coverage, no design record | **Open** |
| **PL-13/14** | Rename dialog for resource identity was never built (`RenameDomainValueDialog` is the *vocabulary* one) | **Open** |
| **PL-24** | The copy sweep; editor fields got helper text, other surfaces did not | **Open** |
| **PL-A5** | `ResourceTag` still has no unique constraint on `(ResourceId, TagDefinitionId)`; two seed scripts work around its absence | **Open** |
| **PL-A6** | Demo grid seed regenerates `ResourceUid` via `NEWID()` every run, invalidating permalinks | **Open** |
| **PL-A10** | `_ = PersistViewAsync(_lastQuery)` still fire-and-forget (`Home.razor:366`) | **Open** |
| **PL-A2** | "Azure ServcieFabric" typo — **confirmed rendering in the grid's Type column**, not just in seed files | **Open** |
| **PL-A3** | **Root-caused:** first-load console 404 is `GET /favicon.ico`; the app ships no favicon. Browser caching is why it never reproduced | **Open, trivial** |
| **PL-60** | `CLAUDE.md` documents UI projects that no longer exist and the wrong hosting model | **Open** |
| **PL-55** | Save button placement, and not gated on validity. **Confirmed live:** enabled on an empty form; sits bottom-left with Cancel bottom-right | **Open** |
| **PL-19** | Editor flow "fluidity" — everything it was gated on is resolved; **needs Steve's judgement**, not investigation | **Awaiting Steve** |
| **PL-36** | Link tags render in the grid; the hardening ride-along is PL-56 | **Partly done** |
| **PL-21** | Add-filter menu **verified clean**; vocabulary selects unchecked; explorer presets moot under PL-58 | **Mostly verified** |
| **PL-40** | Explorer arrows — **deferred with the feature** (PL-58). Re-check only if the diagram is switched back on | **Parked** |
| **PL-01** | The two `NotFound`→404 mapping tests still fail; still the only failures | **Open** |
| **PL-05** | Merge to `main` | **Ready** — fast-forward, no conflicts |

---

## Browser verification pass — 2026-09-13

Drove the running app to settle every item the re-triage had marked **needs eyes**. Explorer items
were deliberately **not** driven — the dependency diagram is dropped from v1 (PL-58).

### Settled by this pass

_Widen the terminal if this renders as stacked Key: value records._

| id | Verdict |
|---|---|
| **PL-A3** | **Root-caused.** The first-load console 404 is `GET /favicon.ico` — the app ships no favicon. Browsers cache the 404, which is exactly why it "did not reproduce" on a second load. Not an app-logic defect; a missing static file. **Still open, trivial.** |
| **PL-A7** | **Not a defect — closed.** An untouched Create form shows **no** error text at all. The red mark on Key is the **required-field asterisk**, the same marker Subscription and Resource Type carry; Name has none because Name genuinely is not required. The original report read a required-marker as a validation error. |
| **PL-21** | **Add-filter menu verified clean.** It offers Name, Type, Description and the non-system tag keys only — no Subscription/Domain, so PL-04's filter holds in the live UI. Vocabulary selects remain unchecked; explorer presets are moot under PL-58. |
| **PL-18** | **Resolved.** The add affordance is now in the same place on every surface — directly beneath its field, as a `+ CREATE …` / `+ ADD …` action (General: *Create Subscription…*, *Create new type…*; Tags: *Create new tag…*; Dependencies: *Create new…*). The 2026-08-04 commit closed this. |
| **PL-19** | The three items it was gated on (PL-17, PL-18, PL-20) are all resolved, so the flow is ready to be judged. **Only Steve can close this one** — it is a subjective verdict on whether the flow now feels fluid. |
| **PL-25b** | **Confirmed fixed in the browser.** The Dependencies tab reads "Choose a Subscription on the General tab first" while that field *is* enabled and reachable on the General tab. The app no longer instructs an action it forbids. |
| **PL-A2** | **Confirmed user-visible.** "Azure ServcieFabric" renders in the grid's Type column. Not just seed-file cosmetics — customers would read it. |
| **PL-55** | **Confirmed live.** Save is **enabled on a completely empty Create form**; its only possible outcome is the warning toast. Also observed and not previously recorded: **Save sits bottom-LEFT**, ahead of Previous/Next, while **Cancel sits bottom-right** — the position a primary action conventionally occupies. Worth folding into the placement decision. |

### Checked and found healthy (no item — recorded so it is not re-investigated)

**PL-53's fix is live.** A globally-required tag that no type template mentions still gets a row: with
a type chosen, *Owning Team* appears with its required marker. The blank form shows no tag rows at
all, which is correct — no type is chosen yet, and the form cannot be saved without one. This was
checked specifically because the DB has a required, non-system tag and the blank form showed nothing;
the fix holds.

### Observation, not yet an item

**Subscription is the most prominent tag on every grid row and is the one thing you cannot filter by.**
That follows directly from PL-04/PL-21 (system and domain tags are excluded from the Add-filter
picker), so the code is behaving as specified. Whether the *specification* is right is a question for
Steve: the field the grid displays most is unfilterable. Raise it, don't fix it.

---

## New findings from the re-triage

### PL-56 — grid link tags are rendered without a scheme guard or `rel`

**This is PL-36's ride-along, which the original entry called for and which was never done.** The home
grid renders a Link-typed tag as `<MudLink Href="@tag.TagValue" Target="_blank">` (`Home.razor:64`).
Two problems, one line:

- **No scheme guard.** `TagValue` is user-supplied. The repo already owns the guard —
  `EditorValidation` restricts Link values to http/https (`:93-94`) — but that runs **at save time in
  the editor only**. The **import path does no URL validation at all**, so a value the editor would
  reject reaches the grid and is rendered as a clickable `href`.
- **No `rel="noopener noreferrer"`** on a `target="_blank"` link. The original PL-36 entry called this
  out explicitly (*"MudBlazor won't"*).

**A sibling surface already does this correctly:** `explorer-canvas.js` guards with `isHttpUrl()`
before opening and sets `rel`/`noopener` at three sites. **The recurring theme again** — a good
pattern established once and applied inconsistently.

**Severity depends on how far import is trusted.** Exposure needs a hostile value to arrive through
import or the API rather than through the editor, so this is not the open door that the print XSS
(PL-A8) was. Recorded as a finding to fix, not as a ship-stopper. Fixing it properly means **hoisting
the scheme guard out of `EditorValidation` into something both the display path and import can call**,
not patching the one `Href`.

### PL-57 — three shipped surfaces have no e2e coverage and no design record

The 2026-08-04 commits added a **Domains page**, a **Tags dictionary manager**, and **"remove
everywhere" cascades** (`DomainValue_ReassignAndDelete`, `ResourceType_ReassignAndDelete`,
`TagDefinition_ForceDelete`). Status:

- **Service-layer unit coverage is real** — rename / reassign / delete paths including refusal cases.
- **No e2e coverage.** The Playwright suite is still the same four specs (delete, dependencies,
  dirty-guard, nested-create); none of the three new surfaces is driven.
- **No design record.** None of the three appears in any doc under `docs/plans/`.

**Why this matters more than the count suggests:** cascade delete is the most destructive code in the
app — it rewrites or removes rows across every resource that uses a vocabulary item — and it is the
only major surface with no end-to-end test. The unit tests mock the repository, so the sprocs' own
`MERGE` and scoping guards are exercised **nowhere automatically**.

### PL-58 — dependency diagram is **out of v1** (Steve's decision, 2026-09-13)

**Decision, not a defect.** The dependency diagram / explorer is dropped from v1. **Constraint Steve
attached: nothing done now may prevent enabling it later.**

That makes this a **hide-the-entry-points change, not a deletion**:

- **Keep** `explorer-canvas.js`, `ResourceExplorer.razor`, `ExplorerService`, `Resource_GetForExplorer`,
  the `Diagram` table and the diagram sprocs. Deleting any of them turns re-enabling into a rebuild.
- **Hide** the routes into it — the explorer entry point(s) from the grid row actions and the nav
  menu. The `/explore/{uid}` and `/explore/shared/{id}` routes can stay registered but unlinked, or
  sit behind a switch; leaving them reachable by typed URL is a decision to make, not an oversight.
- **Keep the seed data and its scripts.** Re-enabling against an empty graph would look broken.
- **Do not** revert the explorer fixes already made (PL-37 through PL-44, PL-A8, PL-A9). They are
  done, they cost nothing to keep, and re-earning them later would be pure waste.

**Items this parks (all already fixed — parked, not reopened):** PL-37, PL-38, PL-39, PL-41, PL-42,
PL-43, PL-44. **PL-40** (are arrows actually visible) was the last explorer item still needing eyes —
it is now **deferred with the feature** rather than verified, and must be checked if the diagram is
ever switched back on.

**Two things to check before hiding it**, because they reach outside the explorer:

1. **PL-56's reference implementation lives in `explorer-canvas.js`** — `isHttpUrl()` plus
   `rel="noopener noreferrer"`. Hiding the UI does not remove the code, so the reference survives; but
   whoever fixes PL-56 should not assume the explorer is the place to put the shared guard.
2. **Export/print (PNG, SVG, print) are explorer features.** Dropping the diagram drops them from v1
   too. The **catalog JSON export (PL-09) is a separate feature and stays** — do not conflate them.

### PL-59 — environment mismatch — **FIXED 2026-09-13**

**Fixed by four changes**, all in `Program.cs` unless noted:
1. `AddUserSecrets<Program>(optional: true)` — secrets no longer depend on the environment being *named* `Development`.
2. Re-add `AddEnvironmentVariables()` + `AddCommandLine(args)` after it, so appending secrets does not outrank them.
3. `builder.WebHost.UseStaticWebAssets()` — **the one that mattered most**, see the trap below.
4. Treat `localhost` as local-development for the exception-handler/HSTS guard.
Plus `appsettings.localhost.json` (committed, localdb strings — non-secret), both launch profiles moved to `localhost`, and `playwright.config.js` pinned to both environment variable names.

**The trap, worth remembering:** Static Web Assets are wired up automatically **only when the environment is literally named `Development`**. Under any other name a non-published `dotnet run` returns **500 for `_framework/blazor.web.js`, `_content/MudBlazor/*` and the scoped-CSS bundle**. The page still server-renders, so it *looks* fine in a screenshot — but it is unstyled and the Blazor circuit never starts, so **nothing is clickable**. A 200 on `/` is not evidence the app works.

That is also why the sibling MacroPoint repos adopt `localhost` painlessly: they are services, with no static web assets to lose.

*Original write-up follows.*

### PL-59 — the documented CLI run command fails; Visual Studio is unaffected

**Scope, corrected 2026-09-13 after Steve ran it from Visual Studio:** **F5 from VS works fine** —
the app serves the grid normally on the `https` profile (`https://localhost:7200`), because that
profile sets `ASPNETCORE_ENVIRONMENT=Development` and the VS process does not carry the ambient
`DOTNET_ENVIRONMENT=localhost` that a shell does. **This is a CLI/agent-shell problem, not a broken
app** — the original heading here overstated it.

**Found while trying to drive the app for the browser pass.** From a shell, every page returns
**HTTP 500**:

```
KeyNotFoundException: Configuration value for key
'ResourceMapper:ConnectionStrings:Database:RO' is required.
```

**Cause.** The shell environment carries `ASPNETCORE_ENVIRONMENT=localhost` **and**
`DOTNET_ENVIRONMENT=localhost`. The app ships `appsettings.json` and `appsettings.Development.json` — **there is no
`appsettings.localhost.json`** — and connection strings live in **user secrets**, which
`WebApplication.CreateBuilder` loads **only when the environment is `Development`**. Under `localhost`
the secrets are skipped and the required key is absent.

**Why the documented run command does not save you — and the non-obvious bit:** it sets only
`ASPNETCORE_ENVIRONMENT`, leaving `DOTNET_ENVIRONMENT=localhost` in place, and **`DOTNET_ENVIRONMENT`
wins** — the app still resolved to `localhost`. Verified twice (`Hosting environment: localhost` with
`ASPNETCORE_ENVIRONMENT=Development` set); setting **both** gives `Hosting environment: Development`
and a 200. That precedence is the opposite of what most people assume, which is what makes this cost
an hour rather than a minute. The both-variables form is a workaround, not a fix.

**Why it still matters, given VS works:** it is the **agent/CLI path** that breaks, and that is the
path every automated run uses — the Playwright suite's `webServer`, any script, any future pipeline.
The failure mode is a bare 500 on every page with the reason only in the log. `GlobalConfig` already has `ValidateRequiredKeys` — failing fast at startup with a
message naming the missing key and the resolved environment would turn a mystifying 500 into a
one-line diagnosis. Deployment is out of v1 scope, but *starting the app* is not.

**Steve's reframe, 2026-09-13 — this is the real finding:** *"the problem is that my F5-VS code is
using `Development` as its environment."* **This project is the outlier, not the shell.** The machine
convention is `localhost`: `macropoint.com` and `macropoint-lite.com` both ship
`appsettings.localhost.json` across their services, and the ambient `DOTNET_ENVIRONMENT=localhost`
exists to serve them. HT.ResourceMapper is the only repo that pins `Development` — in its launch
profiles — and it parks its connection strings where **only** `Development` can reach them (user
secrets, which `CreateBuilder` loads for that one environment name).

So forcing `Development` anywhere — in a shell, in an agent's settings, in the docs — **entrenches the
anomaly**. The fix is to make this repo behave like its neighbours.

**Preferred fix (align with the machine convention):**

1. `builder.Configuration.AddUserSecrets<Program>(optional: true)` — load secrets regardless of
   environment name, so the app stops depending on being called `Development`.
2. Add `appsettings.localhost.json` mirroring `appsettings.Development.json`'s Serilog override.
3. Point both launch profiles at `localhost` so Visual Studio and the shell agree.

Note the connection strings here are **not actually secret** — `(localdb)\MSSQLLocalDB` with
Integrated Security, no password — so they could equally live in a committed
`appsettings.localhost.json` and make a fresh clone work with no setup at all. Worth deciding
deliberately rather than leaving them in user secrets by inertia.

**Still worth doing regardless:** fail fast at startup naming the missing key *and the resolved
environment*. `GlobalConfig.ValidateRequiredKeys` already exists. That turns this whole class of
problem into one legible line instead of a bare 500.

### PL-60 — `CLAUDE.md` documents a project layout that no longer exists

`UI/ResourceMapper.UI.Client` and `UI/ResourceMapper.UI.Server` contain **nothing but stale `obj/`
folders** — no `.csproj`. The solution references exactly one UI project, `UI/ResourceMapper.UI.Web`.
Consequences in the checked-in guidance:

- `dotnet build UI/ResourceMapper.UI.Server` and `dotnet run --project UI/ResourceMapper.UI.Server`
  (both in `CLAUDE.md`'s **Development Commands**) target a project that cannot be built or run.
- `CLAUDE.md` describes the app as **"Blazor WebAssembly hosted by ASP.NET Core"**. `Program.cs` is a
  **Blazor Web App — static SSR with interactive-server islands**. Different hosting model, different
  debugging assumptions.
- The empty `obj/` husks are themselves worth deleting, or the next person will assume the projects
  exist and were merely unloaded.

**Small, but it is the file every new session reads first**, so a wrong instruction here costs more
than its size suggests.

### PL-61 — replace the simulated catalog with the real vNext Azure resource links ⚑

**Steve, 2026-09-13.** The demo/simulated data goes; the catalog is populated from the team wiki page
**vNext Azure Resource Links** (`MacroPoint.wiki`, page 11783).

**Steve will provide the extraction.** The page is *not* reliably parseable by an automated agent, so
**do not scrape it** — the input to this work arrives as instructions/content from Steve. Treat any
agent-side parse of that page as untrusted and re-verify against what he supplies.

**Why this is more than a data swap:** every demo script currently in the repo encodes assumptions the
real data may not share — the domain (`prod` / `non-prod`) split, which tag definitions exist, which
resource types exist and their `ShortCode`/`IconKey`, and the entry-point templates per type. The real
links will have their own shape. Expect the *vocabulary* to change, not just the rows.

**Sequencing — do this before the items it moots, not after:**

- **Supersedes PL-45** (seed data does not exercise the headline feature). Real data either exercises
  it or proves the feature wrong; either way the synthetic fix stops being the reference.
- **Probably moots PL-A2** ("Azure ServcieFabric" typo) — that string lives in seeded *type* data which
  real data replaces. Do not spend a separate pass fixing a typo in rows that are about to be deleted;
  re-check afterwards that it is actually gone.
- **Changes the shape of PL-A6** (demo seed regenerates `ResourceUid` via `NEWID()` each run). Real
  catalog rows need **stable, re-runnable identity** far more than demo rows did, because permalinks to
  them will be shared. PL-A6 stops being a demo-hygiene nit and becomes a requirement of the import.
- **Interacts with PL-56.** Real Azure portal links arrive as URLs from an external document and land
  in Link-typed tags rendered as clickable `href`s. That is exactly the unguarded display path PL-56
  describes, and the **import path validates no URLs at all**. Fix PL-56 before or with this, not after.

**Keep the demo scripts runnable.** They are the only fixture that makes the app demonstrable on an
empty database, and the e2e suite has its own separate fixture that must not be disturbed. Replacing
the *catalog contents* should not mean deleting the *ability to seed something*.

**Open questions for Steve** (do not guess these):

1. Is the wiki page a **one-time import** or a **recurring sync**? A recurring sync needs matching
   rules — what counts as the same resource on re-import — and PL-A6's stable-identity requirement
   becomes load-bearing.
2. Do the wiki's groupings map onto the existing **Subscription** domain vocabulary, or do they need
   new domain values?
3. Does real data need **resource types** that do not exist yet, with their `ShortCode`/`IconKey`?

### E2E suite — green again, and the cause was not what it looked like

**All specs pass, verified on two consecutive full runs** with consistent per-test timings (no
flakiness, and the known cold-start race did not surface).

**Correction worth recording.** While the suite was red it was assumed — in this session's own
reporting — to have been *"red since the August feature commits"*, on the reasoning that those commits
shipped without it being run. **That was an inference, and it was wrong.** The suite passes against
current code with every August change in place, so those commits never broke it.

The real cause was environmental, the same root as PL-59: the ambient `DOTNET_ENVIRONMENT=localhost`
overrode the `ASPNETCORE_ENVIRONMENT: 'Development'` that `playwright.config.js` pinned, so the app
under test started with **no connection string at all**. Later, once that was fixed but before
`UseStaticWebAssets()` was added, it failed a second way — the app served pages whose scripts all
404'd, so every spec timed out clicking a control that could never respond.

**Two lessons this cost real time to learn:**

1. **A suite that fails on infrastructure looks exactly like a suite that fails on regressions.**
   Every spec timed out on a `locator.click`, which reads as "the UI changed" — it was actually "the
   UI was never alive". Check that the app under test is genuinely interactive before reading spec
   failures as product defects.
2. **Do not attribute redness to the most recent commits without running them.** The August commits
   were the obvious suspect and were innocent.

**The suite now pins both environment variable names** (`playwright.config.js`), so it no longer
inherits whatever the machine happens to set.

**Unchanged by this:** PL-57 still stands — the suite covers the editor only. Domains, the tag
dictionary manager, and the cascade deletes have no end-to-end coverage, and cascade delete remains
the most destructive code in the app with no automated check above the mocked unit tests.

---

## Historical sections

Everything from here down is the 2026-08-02 capture and the session logs that followed, preserved for
the reasoning they carry. **Status claims in them are superseded by the re-triage table above.**

---

## v1-BLOCKING *(historical — both items below are FIXED)*

Ship-stoppers. Both are small.

| id | Item | Where |
|---|---|---|
| **PL-22** ✅ FIXED | **Cancel leaves a permanently stuck loading bar over a blank page.** `_ = LoadAsync(...)` fire-and-forget from a *synchronous* `void Cancel()`; `finally` clears `_loading` but never calls `StateHasChanged()`, so nothing repaints. Dead end — user must reload. Fix: `async Task Cancel()` + `await`. *(Now `private async Task Cancel()`, `ResourceEditor.razor:415`.)* | `ResourceEditor.razor:288-292, 297-315` |
| **PL-42** ✅ FIXED | **Edges between two on-canvas neighbours are never drawn.** `Resource_GetForExplorer` returns only edges *incident to the center*. For a dependency map this is a correctness defect — the picture asserts "unrelated" when they are related, with no signal of omission. *(The sproc now returns every edge inside the on-canvas set, plus `@KnownResourceUids` backfill.)* | sproc + `ExplorerService` + `explorer-canvas.js` |

## SECURITY *(historical — FIXED)*

| id | Item | Where |
|---|---|---|
| **PL-A8** ✅ FIXED | **XSS via shared diagram name.** `printDiagram()` writes the user-supplied diagram name into `<title>` unescaped. Diagrams are shareable, so a name like `</title><img src=x onerror=…>` executes in the *recipient's* browser when they print. *(Now `escapeHtml(overlayTitle …)`, `explorer-canvas.js:744`.)* | `explorer-canvas.js:590` |

---

## Batches *(historical — see the re-triage table for what is still open)*

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
| PL-28 | `/resource-types` lists 12 types with live node previews, usage counts, delete disabled while in use — **but see PL-54: this confirmation was premature.** PL-28's scope also required the `ResourceTypeTag` entry-point template, which was not built. Verifying "the screen exists and reads correctly" missed a whole half of the item |

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

---

## Session 2026-08-03 (later) — PL-49 / PL-46 / PL-48 fixed

All three verified against live data on `(localdb)\MSSQLLocalDB\ResourceMapper`, not inferred.

### PL-49 — **FIXED**, and it was bigger than captured

The capture said "pass all five params". The better fix was at the data layer, because the import
document genuinely cannot express those columns — so import has no opinion to pass. `TagDefinition_Upsert`
now reads NULL for `@DisplayName`/`@RequirementLevel`/`@IsDomainTag`/`@IsSystemTag`/`@DisplayOrder`
as **"preserve on update"** (documented insert defaults unchanged), and the identity flags are
**monotonic** — settable, never clearable.

**Two additional holes found while building the repro, both now closed:**

1. **Import could reshape a system tag's vocabulary.** The five metadata columns were only half the
   row. Import *does* control `AllowCustomValue` / `IsMultiValued` / `AllowedValues`, so an import
   naming `Domain` turned the required, restricted `["prod","non-prod"]` vocabulary into a free-text
   field (`AllowCustomValue` 0 → 1, `AllowedValues` → NULL). Since identity is `(Domain + Type + Key)`,
   that lets any typo mint a new identity namespace — same severity as the flag reset, different
   column. **A system tag's shape is deployment-owned, so an upsert against one now returns
   `skipped`.** (Found the hard way: my first repro did this to the live Domain tag; restored from
   `Script.PostDeployment1.sql`'s declaration.)
2. **The domain designation could be stolen off a system tag.** The "clear any other row's
   `IsDomainTag`" block runs *before* the update and targets *other* rows, so it bypassed the
   monotonic guard entirely. Passing `@IsDomainTag = 1` for a new tag would have cleared the
   deployment-owned `Domain` row. Unreachable from today's callers, but a latent hole in the same
   defect. Now returns `error` rather than half-applying the move.

Verified — four paths, each observed:

| Path | Result | Domain row after |
|---|---|---|
| Import's exact parameter set | `skipped` | fully intact |
| Explicit `IsDomainTag=0, IsSystemTag=0` | `skipped` | flags intact |
| New tag claiming `IsDomainTag=1` | `error` | designation kept, no row created |
| Non-system tag (regression) | `updated` | shape applied, omitted metadata **preserved** |

`ExportContract`'s caveat is updated: the `tagDefinitions` section is still lossy (it cannot restore
those columns) but is no longer destructive. Left off by default.

### PL-46 — **FIXED**, migration ran clean twice

`DemoExplorerPrimaryUrl` retired. The explorer seed now shares the vocabulary-owned `PortalUrl`,
creating it only if absent and **never updating it**, so the two scripts cannot fight over
`RequirementLevel`/`DisplayOrder` in any run order. Inline migration repoints `ResourceTag` and
`PrimaryTagDefinitionId`, then drops the retired row; it is a no-op once done.

One consequence worth recording: sharing the definition created a *new* overlap the duplicate had
been hiding — both scripts would have written the same `(resource, PortalUrl)` value, and the
vocabulary seed's derived URL would have overwritten the explorer's hand-written per-resource ones.
Resolved with an explicit ownership rule: **the explorer seed owns the portal URLs of its `DEMOEXP-*`
resources**, so the vocabulary seed skips them.

Verified after two consecutive full runs of both seeds:

| Check | Before | After |
|---|---:|---:|
| tag definitions | 12 | 11 |
| duplicate DisplayNames | 1 | **0** |
| retired definition rows | 1 | **0** |
| `DEMOEXP-*` portal URLs kept (hand-written values) | — | **8 of 8** |
| duplicate single-valued `ResourceTag` rows | — | 0 |
| orphaned / unmatched primary pointers | — | 0 |
| Domain tag intact through both runs | — | `ACV=0 IDT=1 IST=1` |

### PL-48 — **FIXED**, confirmed in the browser

`Resource_GetItems` projects `TagDisplayName = COALESCE(td.DisplayName, td.TagDefinitionKey)` — the
same COALESCE `TagDefinition_GetAll` already used, so both read paths agree. `TagKey` is still
projected: filters and URL tokens key off it, and only the *label* was wrong. Driven at 1600×1000,
6 of 11 tags now read properly and **no internal key leaks**: `CostCenter` → "Cost Center",
`OnCall` → "On-Call Rotation", `Owner` → "Owning Team", `Tier` → "Service Tier",
`Repository` → "Source Repository", `PortalUrl` → "Portal URL".

Incidentally closes half of **PL-33**: the grid now says "Subscription" for the Domain tag, matching
the editor's General tab, because that is the DisplayName deployment declares.

**Left out of scope, deliberately:** the *same* leak exists in the **add-filter menu and the filter
chips** (`AddFilterMenu.razor:32` labels options with the bare key; `ResourceFilterModels.cs:175`
does the same for chips). So the grid now reads "Portal URL" while the filter menu above it still
says `PortalUrl`. Fixing it means threading display names through `GetAllTagKeysAsync` →
`ActiveFilter` → chip labels, and `ActiveFilter.TagKey` must stay the URL token — real plumbing with
its own round-trip tests. It belongs in **B7 / PL-24** with the rest of the label work, not bolted on
here. **S/M.**

### PL-52 — the Playwright suite has been red since PL-45 ⚑ (new)

**The punchlist's "8/8 pass" is stale — it is currently 0/8.** Every test fails with
`'Owning Team' is required` → "Could not save". Cause isolated by experiment, not inspection:
relaxing `Owner` to `Suggested` makes a failing test pass, restoring `Error` makes it fail again.
`git log -S` puts the origin at **b8a01ff (PL-45)**, which made `Owner` a `RequirementLevel='Error'`
tag — so the suite has been broken since that commit and nothing re-ran it.

**The app is right and the tests are stale.** PL-45 deliberately introduced a genuinely required tag;
the specs predate it and never fill one. Fix belongs in the specs (fill "Owning Team" in the
create-a-subject helper), *not* in the seed. This also means the punchlist's standing verification
line "expect 8/8" cannot be trusted until the specs are updated — and that any work verified only by
that suite since PL-45 was not actually verified. **S, and it blocks B14 (ship).**

Also observed: **PL-A3 reproduced** — one console `404` on first load of `/`. Previously "did not
reproduce". Still not chased down.

---

## Session 2026-08-03 (later still) — Azure UX steps 1–7 built

`azure-ux-design-v1.md` steps **1–7 of 8** are implemented; that doc now carries the authoritative
status table, four measured corrections to its own §4a, and the reason step 8 was skipped.

**Closed by this work:** PL-34, PL-15 (both directions named), PL-31, PL-32, PL-30 (fully), PL-23,
PL-07, PL-08 (second route, via breadcrumb), PL-11 (the design it called for is now built, minus the
canvas), and PL-54 / the rest of PL-28.

**Still open in those batches:** PL-24's remaining ~90-string sweep (the anatomy now has a helper and
ⓘ slot on every editor field, but the other surfaces' strings were not rewritten wholesale), and
step 8, the explorer canvas.

### PL-52 — **CLOSED.** Playwright suite 8/8, from 0/8

Four spec-side fixes, all consequences of this branch's own changes rather than test rot: a
`fillRequiredTags` helper (driven off the rendered `.rm-tag-required` marker, not a hardcoded tag
name, so the next required tag does not break them again); `clickSelect` retaught where labels live;
`.rm-identity-preview` → `.rm-identity-inline`; `h5.mud-typography-h5` → `.rm-shell-title`.

Also hardened `clickSelect` against a real cold-start race: the first select interaction of a run can
swallow its click while the server pays JIT + first-circuit setup. `delete.spec`'s first test timed
out at 60s twice and passed in 16s warm. A missed click looks exactly like a slow one, so a longer
timeout cannot fix it — the helper now confirms the popover opened and re-clicks once.

### PL-53 — a required tag could be impossible to satisfy ⚑ (new, high severity)

**Found while chasing PL-52; it is what was actually breaking every create-and-save spec.**

The server requires **every** `RequirementLevel='Error'` definition on **every** resource
(`ResourceService.ValidateTags` → `'{name}' is required`). The editor seeded tag rows only from the
resource **type's** `ResourceTypeTag` template. So for any type lacking a template for such a tag,
the form offered **no row for it** and Save was refused for a field the user could not fill — a dead
end with no way to comply. Same class as PL-25b: the app forbids what it demands.

Reachable on any resource type created after the vocabulary seed ran, which is exactly what the e2e
fixture's `E2eDepType` is. The error surfaced as `'Owning Team' is required` on a Tags tab that
showed only two unrelated, non-required rows.

**Fixed** by seeding a row for every globally-required definition regardless of template, mirroring
the server rule exactly. Chose that over scoping requiredness to the type, which would contradict the
server and let resources save without their required tags. **S — but it made create unusable for
affected types.**

### PL-54 — a resource type's tag template was unreachable from the app ⚑ **FIXED**

**PL-28's missing half.** PL-28's scope said *"must cover `ShortCode`/`IconKey` … **and the
`ResourceTypeTag` entry-point template**."* The first part shipped and the browser pass marked PL-28
confirmed on the strength of the screen existing. The template part was never built.

The Resource Types list has always **shown a "Tag Template" count per type** while offering no way to
change it — and the gap went all the way down: no write path existed anywhere, only
`ResourceTypeTag_GetAll`. The only editor was a seed script. A column advertising data the app
forbids you to touch is the PL-25b shape again.

Why it mattered more than completeness:

- these rows decide **which tags a new resource of a type is prompted for**
- `IsDefaultPrimary` decides **which Link tag becomes that resource's click-through link** — the
  star/radio of PL-35 — and was settable only by re-running SQL
- it is the **proper fix for what caused PL-53**: a type whose template omits a globally-required tag
  is now editable, instead of the editor having to compensate

**Built the whole path:** `ResourceTypeTagList` TVP + `ResourceTypeTag_SetForType` (replace
semantics, MERGE so unchanged rows keep their identity), `SetEntryPointTemplatesAsync` on the
repository, `GetEntryPointTemplatesAsync` on the service, `EntryPointTags` on
`SaveResourceTypeRequest`, and a template section in the Edit dialog (rows + per-type requirement
override + default-primary radio + remove + add).

Three sproc guards, each a constraint violation waiting to happen: the default-primary flag is
cleared across the type **before** the MERGE (promoting B while A still holds it violates
`UX_ResourceTypeTag_DefaultPrimary` mid-statement); duplicate `TagDefinitionId`s and extra primaries
are collapsed so a caller bug cannot trip either unique constraint; and `NOT MATCHED BY SOURCE` is
**scoped to the type** — unscoped it would delete every other type's rows.

**`EntryPointTags` is null-vs-empty sensitive on purpose.** Null = "leave the template alone", which
is how the editor's inline *Create new type…* opts out (it has no template UI). Treating null as
"clear" would have silently wiped a template on every inline create. An empty **list** still means
"no tags", which is a real edit.

Two deliberate UI constraints: only Link tags get the primary radio (a text tag cannot be a
click-through link — text rows show a dash explaining why), and Domain/system tags are excluded from
the picker for the same reason the editor excludes them (PL-04/PL-21).

### Regression caught and fixed in the same pass

`@bind-ActivePanelIndex` (needed for PL-15's footer nav) made the active tab component state. Every
editor route is `/resources` or `/resources/{uid}`, so Blazor reuses the instance and the tab
persisted across records — a nested "Create new…" opened on **Dependencies** with its identity fields
unseen. Now reset on record / nested-level change only, so View → Edit on the same record still keeps
your place.

## Session 2026-09-13 — walkthrough resumes

### PL-55 — the editor's Save button: placement, and it invites a refusal ⚑ (new)

**Steve, on returning to the app:** *"I don't think a 'Save' button should be at the bottom of the
screen and if so, it should not be enabled if the record is not valid. I click on save and I get a
yellow toast."*

**Descriptive, not prescriptive** — recorded as three observations; the fix is TBD:

1. **Placement.** Save sits in the sticky footer `.rm-shell-foot` beside the wizard's Previous/Next
   (`ResourceEditor.razor:98`). That footer's own comment already records this as a knowingly
   accepted trade from the Azure UX pass: *"Azure's bar belongs to a create wizard, while this editor
   is mostly used to edit, where bottom-right Save is the stronger convention. Kept for consistency
   with the reference."* So this **re-opens a decision that was made deliberately**, rather than
   correcting an oversight — decide it on its merits, don't just move the button.
2. **Save is never disabled for invalidity.** `Disabled="@_saving"` is the only gate. Validity is
   computed nowhere the button can see — `EditorValidation.ValidateGeneral/ValidateTags` run *inside*
   `SaveInternalAsync` (`:634-641`), at click time.
3. **The yellow toast is that check failing.** `Snackbar.Add("Fix the highlighted fields before
   saving.", Severity.Warning)`. An enabled control whose only outcome is a refusal — **the same
   shape as PL-25b and PL-53:** the app offers an action it will not honour.

**Note the tension between (1) and (2):** a disabled Save needs a live validity signal, and a Save
placed where it must be scrolled to makes *"why is it disabled?"* harder to answer. Whichever
placement wins, the "why can't I save" affordance is part of the same decision — a silent disabled
button trades one dead end for a quieter one.

**Also unresolved:** if validity gates the button, an untouched Create form is invalid from first
paint, so Save would start disabled — which is the neighbourhood of **PL-A7** (Key shows red
"Required" on an apparently untouched Create form while empty Name isn't). Confirm PL-A7 first; these
may be one decision about when validation starts speaking.

**Not yet triaged into a batch.** Touches `ResourceEditor.razor` chrome (B4's file) and the Azure
footer from B11.

## Coverage gaps in this capture

Findings here are only as good as what was exercised. Not covered:

- **`/import`** — never driven. Code review was clean (20MB limit set, errors caught and surfaced),
  but it was never used
- **`/explore/shared/{id}`** — read-only share + save-a-copy never exercised
- **Grid filters / search / sort / Back-Forward** — inspected visually; behaviour never driven.
  `filter-plan/RESUME.md`'s verification list (facet counts shifting, two-tag-key AND, NotEquals,
  `(blank)` bucket) remains **unverified**

A short second pass on `/import` before locking scope is worthwhile.

## Effort and the cut list *(historical — the cut was effectively taken)*

> Most of what "Option B" scoped has since been built, and parts of Option C with it
> (type CRUD, export, the Azure design). Kept for the estimation method and the
> agent-compression analysis, which held up. The batch table below is **not** current status.

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

## Open decisions

**Re-triaged 2026-09-13.** Most of the original five were settled by building the thing. What remains:

1. **When should validation start speaking?** One decision covering the Save button (PL-55) and the
   Key-shows-Required-on-a-fresh-form report (PL-A7). Gating Save on validity means an untouched
   Create form starts disabled, so these cannot be decided separately.
2. **Where does the scheme guard live?** (PL-56) Hoisting it out of `EditorValidation` so the display
   path and import share it is the real fix; patching the one `Href` is not.
3. **Is resource-identity rename still wanted?** (PL-13/14) It was specified, never built, and the app
   has been used without it.

*Settled since capture, kept for the record:* the Azure styling question (PL-11 — the design was built),
the export transport question (PL-09 — in-process service shipped), wizard nav scope (PL-15 — both
modes), and the add-affordance and multi-value questions (PL-18 / PL-20).

## Verification

- `dotnet test` — the **only** expected failures are the two `NotFound`→404 mapping tests (PL-01).
  Any other red is a real signal. *(Deliberately not stating a total: the count grows with every
  feature, so a fixed number here would go stale immediately and make a correct run look wrong.)*
- `cd tools/e2e && npm run test:e2e` — all specs should pass. **Coverage gap (PL-57):** the suite
  drives the editor only; the Domains, Tags-manager and cascade-delete surfaces are untested. Was 0/8 from PL-45 until PL-52 closed it;
  a red run is now a real signal again.
- Drive the app: `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5200" dotnet run --project UI/ResourceMapper.UI.Web --no-launch-profile`
- **Stop the app before building** — it locks build output
- Screenshot driver: `tools/e2e` helpers; from Git Bash use `MSYS_NO_PATHCONV=1` or a `/` route
  argument gets rewritten into a Windows path
- DB checks: `sqlcmd -S '(localdb)\MSSQLLocalDB' -d ResourceMapper` with `SET QUOTED_IDENTIFIER ON;`
  — **not** the SQL MCP

## Session 2026-09-13 (later) — live catalog landed, saved views designed

### PL-62 — saved views have no on-screen "you are here" indicator (parked idea)

**Steve:** *"pin this idea for later: Edge solves the equivalent with a filled star in the address bar."*

**Context.** Saved Views (see `SavedViewsDesign.md`) puts Save / Save As / the view list / Manage
entirely in the hamburger menu — no grid toolbar, by decision. The consequence, raised and
accepted at design time: nothing on the page says which saved view you are in, or that you have
changed it since opening. `Save`'s target is therefore implicit.

Steve's call was that this matches how Edge behaves, and it does — Edge's favourites menu does not
annotate the page either. But Edge *does* have one signal the grid will not: **a filled star in
the address bar**, telling you the current page is already a favourite without opening anything.

**Parked, not rejected.** Deliberately out of scope for the first build, so the menu-only shape
gets used as designed before more chrome is added. If it proves confusing in practice, the
smallest remedy is a name chip on the filter bar showing the open view and a changed marker —
the equivalent of the filled star. Decide it from use, not in advance.

**Related:** the fire-and-forget `PersistViewAsync` call (PL-A10) sits in the same `Home.razor`
startup path that Saved Views changes, and is worth folding in when that code is touched.

### PL-63 — swap the single-owner identity for a real provider

**Steve:** *"pin this, adding identity is on the roadmap."* Then, on being told saved views would
otherwise be browser-scoped: *"or if you can, you can use the current identity (which is always
mine until we add an identity provider)"* — and on what that identity should be, *"or it may be
anonymous or something."*

**What changed because of that.** The first version of this entry recorded a constraint to live
with: everything personal keys on `ClientIdentity`'s random per-browser GUID, so nothing follows a
person. Steve's steer turned it into a seam built now rather than a gap deferred —
`ICurrentIdentity` returns a single `OwnerId` read from `ResourceMapper:Identity:OwnerId`,
defaulting to the literal `anonymous`. Saved views, Explorer diagrams and the grid resume setting
all move onto it (see `SavedViewsDesign.md`).

**What is left for the roadmap.** Only the provider. Because the rows already carry a stable
owner, arriving at real identity is one implementation of `ICurrentIdentity` plus a decision about
what happens to rows owned by `anonymous` — adopt them for the first real user, or leave them.
That decision is much easier taken while the tables are small.

**What this explicitly is not.** Not authentication, not authorisation, not multi-tenancy. Anyone
reaching the deployment is the owner, and every procedure's `@OwnerId` filter is a no-op today.
The point is that ownership has the right *shape* now, so the later change is an implementation
rather than a schema migration across three tables.

**Carries a cost worth knowing.** `ClientSetting.ClientId` and `Diagram.ClientId` should be renamed
to `OwnerId` in the same pass, touching the `Diagram_*` and `ClientSetting_*` procedures and their
repositories. Diagrams already saved under a browser GUID become invisible — a non-event in a dev
database, a one-line `UPDATE` if one ever matters.

### PL-64 — select lists: compact rows, and a way to stop them running off the screen ⚑ (new)

**Steve:** *"select lists should display items in compact form and we need a mechanism so items do
not scroll off the screen."* Observed on the **Add Filter** menu with the live catalog loaded.

**Why it appeared now.** The demo data had a handful of tag definitions. The live catalog has
**19**, so the menu went from comfortably short to taller than the viewport. Nothing about the
menu changed — the data did. Expect the same wherever a list is generated from the vocabulary.

**Three separate problems in one control** (`AddFilterMenu.razor`):

1. **Rows are not compact.** The `MudMenu` sets no `Dense`, so each entry takes full-height
   padding. Roughly a third more entries would fit on the same screen for free.
2. **No overflow mechanism.** There is no `MaxHeight`, no internal scroll and no type-to-filter, so
   entries past the viewport are simply unreachable — the page itself does not scroll while a
   popover is open. This is the part that makes it a defect rather than a nicety.
3. **It shows internal tag keys, not display names** — `AppInsightsFilter`, `ConfigPrefix`,
   `DbAppName`, `PortalUrl` — where the grid beside it shows "App Insights Filter", "Config Key
   Prefix", "Database Application Name", "Portal". That is **PL-48's exact defect**, which was
   fixed for the grid and never applied here: `ResourceService.GetTagFilterKeys` returns
   `GetAllTagKeysAsync`, a list of raw keys, with no `COALESCE(DisplayName, TagDefinitionKey)`
   anywhere in the path. Found while confirming the first two; Steve did not report it.

**Descriptive, not prescriptive.** The fix is not decided. Worth noting that item 3 changes a
contract (the filter menu would need key *and* label, since the key is what the filter and the
permalink token are built from — see the comment in `Resource_GetItems`), so it is not simply a
matter of swapping the string.

**Scope beyond this one menu.** Any select fed from the vocabulary has the same exposure: the
resource type picker, the tag pickers in the editor, and the domain-value lists. Worth fixing as a
pattern rather than one control at a time.
