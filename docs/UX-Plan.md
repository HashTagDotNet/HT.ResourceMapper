# HT Resource Mapper – UX & Architecture Plan

> **Living document.** Update this file whenever a design decision is made. Sections marked ⚠️ are open for design discussion — do not implement until resolved.
>
> **Wireframes:** `docs/Wireframes/`
> - `Resource Editor.png` — resource editor wireframe
> - `HomePage-Grid-Annotations.png` — home page grid annotated review (2026-06-13)
> - `HomePage-Toolbar-BetterUX.png` — proposed toolbar layout (2026-06-13)
> - `MainLayout-AppBar-Navigation.png` — AppBar / nav drawer changes (2026-06-13)

---

## Table of Contents

1. [Application Shell](#application-shell)
2. [Home Page](#home-page)
3. [Resource Editor](#resource-editor)
4. [Import / Export](#import--export)
5. [Design System & Visual Styling](#design-system--visual-styling)
6. [Architecture: Authentication Flow](#architecture-authentication-flow)
7. [Architecture: WASM Startup Performance](#architecture-wasm-startup-performance)
8. [Architecture: Caching Strategy](#architecture-caching-strategy)

---

## Application Shell

**Status:** Decided — ready to implement.

### AppBar
- Spans **full width** across the entire page, sitting above the nav drawer (not beside it)
- **Left**: Hamburger menu (≡) — toggles the nav drawer open/closed
- **Center**: App title "ResourceMapper"
- **Right**: Dark mode toggle

### Nav Drawer
- Slides open/closed beneath the AppBar; does not push the AppBar aside
- Remove the **"Navigation"** label from the drawer header — redundant
- Nav items: Home, Resources, Import/Export, Settings (with sub-items)

### Per-Page Toolbar ⚠️ *Design discussion needed*
Each page may inject content into the AppBar (search box, actions). The mechanism for this — whether controls are hardcoded into `MainLayout` or injected per-page via a Blazor layout slot/portal pattern — needs to be decided before implementation.

See wireframe: `HomePage-Toolbar-BetterUX.png`

---

## Home Page (`/`)

**Status:** Partially decided. Items marked ⚠️ need further design discussion.

### Layout

```
┌─────────────────────────────────────────────────────────┐
│ ≡  [Import/Export]   [   🔍 Search...   ]  [Add Resource +] │  ← AppBar
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Resource Name ↓    │ Resource Type │ Tags    │ Updated │
│  ─────────────────────────────────────────────────────  │
│  Customer Portal    │ App Config    │ Env:... │ 2h ago  │
│  Production DB      │ Database      │ ...     │ 1d ago  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Toolbar ⚠️ *Design discussion needed before implementation*
Proposed: move primary actions out of the page content area and into the AppBar:
- **Left zone**: Import / Export link with icon
- **Center zone**: Search box — wide, prominent, with leading search icon
- **Right zone**: "Add Resource" label + `+` button

Current implementation (Bad UX): floating FAB + search box awkwardly centered in page content. See wireframe: `HomePage-Toolbar-BetterUX.png`.

Discussion needed: AppBar injection mechanism (layout slot vs. hardcoded), how AppBar changes per page, exact sizing of the three zones.

### Resource Grid

**Grid card header:** Removed — the "Resources" title and icon above the grid wastes vertical space and is redundant.

**Columns (4 total — Description column removed):**

| Column | Width | Sortable | Notes |
|--------|-------|----------|-------|
| Resource Name | 40% | ✅ A→Z default | Plain clickable link → detail page (same frame) |
| Resource Type | 25% | ✅ | Plain text — no chip or badge styling |
| Tags | 25% | ❌ | Plain text — see tag display rules below |
| Last Updated | 10% fixed | ✅ | Relative text ("2 hours ago") |

**Tag display rules:**
- Plain text style — no `MudChip` or button formatting
- Text tags: `Key: Value`
- Link tags (`ContentType = "Link"`):
  - Display as `Key: [clickable text]` — opens in a **new window/tab**
  - On hover: show copy icon (GitHub code-copy style, invisible until hover)
  - On copy icon click: copy URL to clipboard; show **"link copied"** feedback (exact wording and position TBD)
- Max 5 tags shown; overflow displays `(X more...)`
- Vertical spacing: as compact as feasible while remaining readable

**Empty state:** "No Resources Found" with "Add Your First Resource" CTA.

**Loading state:** `MudProgressCircular` centered in grid area.

### Search

- **Scope:** global — searches resource name, description, type, tag keys, tag values
- **Triggers:** Enter key or search icon click (saved to history); 400ms debounce for live filtering (not saved)
- **Clear:** X button or Escape key; empty search = no filter, grid returns to default (alphabetical by name)
- Grid columns (Name, Type) sort and filter in response to the active search query

### URL State Persistence

Every grid state change must update the URL so the view is fully bookmarkable and shareable:
- Parameters: search query, sort column, sort direction, active filters, page/offset
- **Load priority:** URL parameters → saved local state → application defaults (URL always wins)
- Goal: copy URL → paste in new browser → identical view

### Search History ⚠️ *Design discussion needed before implementation*

- Storage: **IndexedDB**
- Retention: rolling **10-day** window for non-favorited entries; auto-purge on rolloff
- A search is saved to history **only on Enter or search icon click** (not on debounce)
- Surfaced via a **dropdown** from the search box
- Each entry has a **favorite star** (filled / unfilled):
  - Favorites appear **first** in the dropdown, above MRU entries
  - Favorites are **exempt from 10-day rolloff** — persist until explicitly un-favorited
- Open questions: star interaction UX, dropdown layout, max favorites, duplicate search handling

---

## Resource Editor (`/resource/{uid}`)

**Status:** Initial wireframe reviewed. See `docs/Wireframes/Resource Editor.png`.

### Layout

- URL shows the resource GUID: `/resource/some-resource-guid`
- Page title: `Resource: {Resource Name}`

```
┌──────────────────────────────────────────────────────────┐
│  Resource: My Resource Name     [Apply] [Delete]  [✕]    │  ← Fixed header (pinned)
├──────────────┬───────────────────────────────────────────┤
│ Resource     │  *Resource Code                           │
│ Properties   │   Unique code for API/programmatic access │
│ Search Tags  │  ┌──────────────────────┐  📋            │
│ Depends On   │  │ my-resource-code     │                 │
│ Dependent On │  └──────────────────────┘                 │
│              │  Sample error message (red, below field)  │
│              │                                           │
│              │  Display Name                             │
│              │  ┌──────────────────────┐                 │
│              │  │ Visibility App Insig │                 │
│              │  └──────────────────────┘                 │
│              │                                           │
│              │  *Resource Type                           │
│              │  ┌──────────────────────┐                 │
│              │  │ Azure App Config  ▾  │                 │
│              │  └──────────────────────┘                 │
│              │                                           │
│              │  Notes                                    │
│              │  ┌──────────────────────┐                 │
│              │  │                      │                 │
│              │  └──────────────────────┘                 │
└──────────────┴───────────────────────────────────────────┘
```

### Fixed Header
- Stays **pinned** when the form body scrolls
- Contains: Apply (primary action), Delete (secondary), Close ✕
- Apply and Delete displayed as **links or minimal buttons** (per wireframe — not filled buttons)

### Left Sidebar Navigation
Sections (scroll the right panel independently):
1. Resource Properties
2. Search Tags
3. Depends On
4. Dependent On

### Field Anatomy
Each field follows this pattern:
```
**Field Label** (* = required)
Field help text, optional, italic, may be multi-line
┌─────────────────────────────────┐
│ user input                      │
└─────────────────────────────────┘
Error message in red (below field, only when invalid)
```

### Copy-to-Clipboard Icon
- Present on fields where copying is useful (e.g. Resource Code)
- **Invisible** until the field is hovered or focused
- On click: copies the **current window URL** to clipboard
- Shows a **"Copied"** tooltip briefly after click

### Resource Type
- Dropdown — values loaded dynamically from the server (currently hard-coded; to be replaced)
- Required field

---

## Import / Export (`/import`)

**Status:** Placeholder — not yet designed.

API design documented in `docs/ImportExportApiDesign.md`.

---

## Design System & Visual Styling

**Status:** ⚠️ *Needs design discussion before implementation.*

The current UI is visually underdeveloped. A focused design session is needed to establish a consistent visual language applied aggressively across all pages.

Topics to resolve:
- Color palette and MudBlazor theme customization (primary, secondary, surface, background)
- Typography scale — headings, body, captions, labels
- Spacing and density conventions (padding, margin, gutters)
- Card and surface treatment (elevation, border radius, dividers)
- Interactive element styling — links, buttons, hover/focus states
- Icon usage conventions
- Data grid visual style — row height, header, alternating rows vs. flat
- Application shell — AppBar height, drawer style, nav item treatment
- Empty and loading state visual design

**Goal:** define the look-and-feel once, apply it everywhere simultaneously.

---

## Architecture: Authentication Flow

**Status:** ⚠️ *Needs design discussion before implementation.*

Authentication is handled by an **off-system external provider**. The proposed entry flow:

```
Browser request
  → Headless MVC page or lightweight API endpoint (server-side, no WASM)
      → Check for auth cookie
          → Cookie valid    → Redirect to WASM home page
          → Cookie missing  → Redirect to external authentication system
External auth completes
  → Returns to app with cookie set
  → Resumes normal WASM flow
```

Design decisions needed:
- Headless MVC page vs. minimal API endpoint for the cookie-check gate
- Cookie format, lifetime, and validation
- External auth provider integration contract (redirect URL, token/cookie handoff)
- How WASM receives identity (claims, user object) after auth succeeds
- Token refresh and session expiry handling in the WASM client
- Security: CSRF protection, cookie flags (HttpOnly, Secure, SameSite)

---

## Architecture: WASM Startup Performance

**Status:** ⚠️ *Needs design discussion before implementation.*

**Target:** App ready to use in **< 100 ms** perceived load time. Unavoidable IT/network latency exists — every controllable millisecond must be saved.

Design decisions needed:
- Minimize WASM bundle size: lazy assembly loading, trimming, AOT trade-offs, pre-caching
- Lightweight non-WASM entry point: can a static HTML shell render while WASM downloads?
- Define "ready to use": grid visible with data, or just shell rendered?
- Blazor WASM pre-rendering: server-side pre-render of initial HTML before WASM hydrates
- PWA / service worker caching for near-instant repeat visits

---

## Architecture: Caching Strategy

**Status:** ⚠️ *Needs design discussion before implementation.*

**Goal:** Aggressive server-side caching to minimize database round-trips and support the < 100 ms load target.

Areas to design:
- **In-memory cache** (server process): reference data (resource types, tag definitions), short TTL
- **Distributed cache** (e.g. Redis): resource grid results, search results, user session data — shared across server instances
- Cache invalidation: which entries are evicted when a resource is created or edited
- Client-side caching (IndexedDB, in-memory): what WASM can safely cache and for how long
- Cache warm-up: pre-populate on server startup or on first request?
