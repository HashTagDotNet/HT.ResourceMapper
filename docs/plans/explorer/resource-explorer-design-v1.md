# Resource Explorer — V1 Design

> **Status:** Design agreed via brainstorming. Not yet an implementation plan.
> Capability/feature level only — engine choice and build sequencing are deferred to
> implementation planning.
>
> **Date:** 2026-07-19

---

## 1. Purpose

A graphical explorer for the resource dependency graph — conceptually the **SQL Server
Management Studio database-diagram** equivalent for cloud resources. A resource is placed on
a canvas; acting on it opens/closes the resources that depend on it or that it depends on.
The diagram can be arranged, saved, shared, and exported as a picture.

**Co-primary jobs (V1):**

1. **Explore / document** — understand and communicate how resources relate.
2. **Directional impact** — read "what depends on this" and "what this depends on" from any node.

The canvas is **read-only over the catalog**. All catalog editing happens in the existing
resource editor; the explorer links out to it.

---

## 2. What this builds on (existing foundation)

- **The data is already a directed graph.** `HTResourceMapper.ResourceRelationship` stores one
  row per edge (`FromResourceId` = dependent → `ToResourceId` = dependency), unique, with a
  no-self-loop check.
- **Single-hop reads exist.** `ResourceRelationship_GetForResource` returns a resource's edges
  in both directions; `ResourceRelationshipItem` carries `Direction` (`DependsOn` out-edge /
  `DependentOn` in-edge), plus the other node's `Key`, `Name`, `Type`, `Domain`.
- **The editor already has Dependencies / "Dependent On" tabs** (text/grid). The explorer is the
  graphical counterpart to those tabs.
- **Resources carry tags** (`ResourceTagModel`): each tag has a `ContentType` (URL is one) and an
  `IsPrimary` flag. The primary URL-type tag is the resource's canonical deep-link.
- **The editor has a dirty-guard** (slice #9) — pattern available if ever needed.

**Deliberately NOT relied on:** edges are currently **untyped** (every edge means "depends-on");
there is **no transitive traversal** (no recursive CTE). V1 needs neither.

---

## 3. Scope

### In scope (V1)

- Enter rooted on a single resource; grow by one-hop expansion.
- Deduped nodes; one-hop expand/collapse; collapse-all; remove-node with reachability cleanup.
- Pan / zoom / fit-to-screen; directed-arrow edges.
- Node display presets; auto-layout on first appearance then drag-to-move with persisted positions.
- Per-node link affordances: hover tooltip, external primary URL (new tab), open in editor (new tab).
- Floating toolbar (global actions) + inline expand control + right-click node menu.
- Server-persisted diagrams owned by an anonymous durable client identifier.
- Save / Save-As / Open-Recent / Delete (scoped to the client identifier).
- Share via short link (read-only) + "Save a copy to mine".
- Export: copy PNG to clipboard, save PNG, save SVG, browser Print → PDF.

### Out of scope (deferred / future)

- Typed edges and the producer/consumer split.
- Transitive expansion / "expand all" / computed blast-radius.
- On-canvas relationship editing or catalog CRUD (drawing edges, creating resources).
- Multi-root / unconnected canvases (dropping unrelated resources on one canvas).
- Real per-user identity / authentication.
- Undo/redo, minimap, on-canvas find, secondary (non-primary) URL tags.
- Live refresh of a node after it is edited in the editor tab.

---

## 4. Interaction model

### 4.1 Entry & seeding

- The explorer opens **rooted on one resource**, launched via an "Explore" action from the home
  grid or the editor. There is **no blank canvas and no on-canvas resource picker** in V1.
- Consequence (accepted): a diagram is always a **connected subgraph** reachable from its seed;
  you cannot place two unrelated resources on one canvas in V1.

### 4.2 Nodes and expansion

- **One node per resource (deduped).** If a resource is reachable by multiple paths it appears
  once, with edges converging. This is also the **cycle-safety** mechanism: expanding into an
  already-present node just draws the edge — it never re-expands infinitely.
- **Click / inline control on a node toggles its immediate neighbors** open or closed:
  - out-edges (`DependsOn`) = "what this depends on"
  - in-edges (`DependentOn`) = "what depends on this"
- **One-hop only.** To go deeper, expand the next node. No transitive/expand-all in V1.
- **Collapse all** — a global action that collapses every expanded node back toward the seed
  (client-side; no server call).
- **Edges render as directed arrows** indicating dependency direction.

### 4.3 Remove node (reachability cleanup)

- Removing a node removes it and then **drops any node no longer reachable from the seed root**,
  keeping the rest. A node still reachable via another surviving path stays.
  - Worked example: seed `A` → `B`,`C`; `B` → `D`; `C` → `D`,`E`. Remove `B` ⇒ `B` disappears,
    `D` stays (still reachable via `C`), `E` stays.
- The reachability pass runs on the client-held graph (inexpensive).
- **Removing the seed root clears the canvas** (treated as a deliberate reset — nothing anchors
  reachability once the seed is gone).

### 4.4 Canvas navigation

- Pan (drag), zoom in/out, fit-to-screen.

---

## 5. Node rendering

### 5.1 Display presets

- Node content is chosen from a small set of **presets**, applied to the whole diagram and saved
  with it:
  - **Name only**
  - **Name + Type**
  - **Detailed** — Key/Code + Name + Type + Domain
- (Per-field toggles and per-node overrides are out of scope for V1.)

### 5.2 Layout memory

- **Auto-layout on first appearance:** when a node first appears (seed or newly expanded), the
  layout algorithm places it.
- **Then manual + persisted:** the user can drag nodes; positions are saved with the diagram.
- **Reopening a saved diagram looks identical** to how it was left (SMS-diagram behavior).

### 5.3 Per-node link affordances

- **Hover tooltip** — quick metadata (Name, Type, Domain, Key) without navigating.
- **External-link icon → primary URL** — opens the resource's **primary URL-type tag** in a
  **new tab** (the "jump to Azure resource" action). **V1 surfaces the primary URL only**;
  other URL tags are ignored.
- **Open in Resource Mapper** — opens the resource in the editor/detail view in a **new tab**
  (see §8).

> **Read-model dependency:** the neighbor read currently returns only Key/Name/Type/Domain. To
> render the external-link icon, the read path must **also return each node's primary URL**.

---

## 6. Actions UI

Two kinds of action, two homes:

- **Global / canvas actions — floating toolbar:** zoom in/out, fit-to-screen, collapse-all,
  display preset, save / save-as, share, export (copy-PNG / PNG / SVG / print), open-recent,
  delete. Globals live in a persistent toolbar because they *must* be discoverable — export and
  share are the payoff features.
- **Per-node expand/collapse — inline control on the node** (e.g. a chevron/±). Used constantly,
  so it is visible without a right-click.
- **Other per-node actions — right-click context menu:** remove node, open external (primary URL),
  open in Resource Mapper. Keeps nodes visually clean and scales to more actions.

**Accepted limitation:** right-click has no clean touch equivalent. V1 is desktop-oriented (an
internal infrastructure tool); touch is out of scope and revisited only if it matters later.

---

## 7. Persistence, identity & sharing

### 7.1 Anonymous durable client identity

- There is **no per-user login** yet. Ownership is an **anonymous durable `clientId`** (a GUID)
  stored on the client in **localStorage + a cookie**, with an optional "copy recovery key" so a
  user can carry their identity to another browser/machine.
- **Accepted trade-off:** clearing storage / a new browser / a new machine yields a **new
  `clientId`**, so the user no longer sees their own diagrams in Open-Recent. The data is **not
  lost** — it stays on the server and any shared links still resolve; only the ownership handle
  is gone. Orphaned diagrams simply accumulate (acceptable for an internal tool).

### 7.2 Server-side diagram store

- Diagrams are **persisted server-side** (this re-adds a backend workstream: **diagram tables +
  a CRUD/resolve API** — create, read, update, delete, resolve-by-shareId).
- Each diagram carries:
  - a **private `clientId`** — the owner handle, and
  - a **separate public `shareId`** — an unguessable token used only for sharing, so sharing one
    diagram never exposes the owner's whole library.
- **A saved diagram = { seed resource, the set of on-canvas nodes, their expansion state, their
  positions, the display-preset setting }.**
- **Save / Save-As / Open-Recent / Delete** operate on diagrams scoped to the current `clientId`.

### 7.3 Sharing

- A **share link resolves a `shareId`** and opens the diagram **read-only**.
- A recipient (different `clientId`) can **"Save a copy to mine"** — clones the diagram into a new
  diagram under their own `clientId`. There is no anyone-with-link editing (avoids bearer-token
  edit collisions with no identity).

---

## 8. Editor handoff

- "Open in Resource Mapper" (and the external primary-URL link) **open in a new browser tab**;
  the diagram tab stays exactly as-is. No save, no prompt, no navigation away — so an arranged
  diagram is never at risk, and "return to diagram" is simply switching back to the untouched tab.
- **Accepted limitation:** because the editor is a separate tab, edits do **not** live-refresh the
  node. On returning, the node shows its previous name/type until the user re-expands or refreshes.
  Live sync is a future nicety.

---

## 9. Export

- **Copy PNG to clipboard** (primary — paste straight into Teams/Outlook).
- **Save PNG** (raster, for quick sharing).
- **Save SVG** (true vector — crisp at any zoom, editable downstream).
- **Browser Print → Save-as-PDF** covers the PDF need by vectorizing the SVG; **no custom PDF
  pipeline** is built (client-side PDF would only wrap a raster image — no quality gain).

---

## 10. Rendering engine — recorded constraint (decision deferred)

The V1 interaction requirements — **drag-to-position, incremental one-hop expand/collapse, stable
persisted positions, PNG/SVG export** — **rule out Mermaid** (it re-renders whole static diagrams
and cannot do interactive expand/collapse or manual positioning) and **MudBlazor** (no graph/
diagram component). The realistic path is **JS-interop from Blazor to a mature interactive graph
library**. The specific library is an **implementation-planning decision**, not a V1 capability
decision, and is deliberately left open here.

---

## 11. Accepted trade-offs (chosen, not stumbled into)

1. **Per-machine "Recent."** Without identity, Open-Recent is per-browser; the share link is the
   only bridge between people/machines.
2. **Orphaned diagrams on storage clear.** Data survives server-side; ownership handle does not.
3. **Connected-only canvases.** No unrelated resources on one canvas in V1 (entry is
   explore-from-resource only).
4. **Untyped edges.** Impact is directional ("depends-on" / "depended-by"); no producer/consumer
   split until typed edges arrive.
5. **No live refresh after editor edits.** Re-expand/refresh to pick up changes.
6. **Desktop-oriented.** Right-click node menu has no V1 touch equivalent.

---

## 12. Implications for implementation planning (not decided here)

These are the concrete workstreams the design implies; sequencing/design happens in the plan:

- **Read-model extension:** add each node's **primary URL** to the neighbor read used by the canvas.
- **Diagram persistence:** diagram store schema (owner `clientId`, public `shareId`, seed, node
  set + expansion state + positions, display preset) + CRUD/resolve API.
- **Anonymous client identity:** durable `clientId` bootstrap (localStorage + cookie) + recovery-key.
- **Canvas/engine integration:** JS-interop graph library selection and the Blazor wrapper.
- **Explore entry point:** "Explore" launch from grid/editor rooted on a resource.
- **Export/clipboard:** copy-PNG, PNG, SVG, print wiring from the chosen engine.
