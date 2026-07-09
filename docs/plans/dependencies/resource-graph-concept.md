# Resource Dependencies — Conceptual Model

> **Status:** Conceptual only. No dependency data is written or read by the running
> application yet. This document captures the *understanding* reached in discussion,
> not a design or an implementation plan. Nothing here has been decided or built.
>
> **Date:** 2026-07-09

---

## 1. The core idea: resources form a graph

The resource catalog is not a flat list — resources relate to one another, and those
relationships form a **directed, typed multigraph**:

- **Nodes** = resources (apps, services, queues, databases, APIs, …).
- **Edges** = relationships between resources, each carrying a **direction** and a **type**.

The graph is:

| Property | Why | Example |
|---|---|---|
| **Directed** | Relationships are asymmetric | `My-API` → `my-oauth-server` is not the reverse |
| **Typed / labeled** | "produces" ≠ "consumes" ≠ "depends on" | `my-app` **ProducesTo** `my-queue` |
| **Multigraph-capable** | One node pair can have >1 edge | A service both **produces to** and **consumes from** the same queue (retry logic) |
| **No self-loops** | "Dependency is external from itself" | `S → S` is a data error |

In storage terms this is an **adjacency-list / edge-table** representation: one row per
edge, traversable from either end. Correct for a sparse graph like infrastructure
dependencies.

---

## 2. Worked examples

**Dependency chain (transitive):**

```
My-API  --DependsOn-->  my-oauth-server  --DependsOn-->  my-identity-database
```

Each hop is stored once. The *chain* is not stored — it is derived by walking the edge
table (a recursive traversal). Reachability / impact analysis is computed, not persisted.

**Producer / consumer on a queue:**

```
my-app1     --ProducesTo-->   my-queue
my-app2     --ProducesTo-->   my-queue
my-service3 --ConsumesFrom--> my-queue
```

**Retry case (the multigraph driver):** a single service is *both* a producer and a
consumer on the same queue:

```
S --ProducesTo-->   Q
S --ConsumesFrom--> Q
```

`S` and `Q` are distinct nodes, so these are two edges between the same pair — a
multigraph, **not** a self-loop.

---

## 3. The central tension (and how it resolved)

The discussion surfaced an apparent conflict in what we want to *see* from each end of an
edge:

- **Browsing the queue `Q` casually** → we expect a single, coarse entry: "S is attached
  to Q." The produce/consume mechanics don't matter here.
- **Browsing the service `S`** → we expect role detail: "ProducesTo: Q, ConsumesFrom: Q."
- **During an incident on `Q`** (runaway queue counts) → we urgently need the *opposite* of
  the casual view: producers of Q as a group, **distinct from** consumers of Q. Role
  fidelity is demanded from **Q's** side.

### Resolution: the role belongs on the **edge**, not the node

Because the highest-value query (the incident) stands on the queue and needs the
producer/consumer split *from that side*, the relationship **type must be stored on the
edge** so it is queryable from either end. One stored fact then serves every viewpoint at
whatever fidelity the UI chooses:

| Viewpoint | Read | Rendering |
|---|---|---|
| Browsing `Q` | in-edges of Q | collapse types → "S is attached" |
| Incident on `Q` | in-edges of Q, grouped by type | "producers: … / consumers: …" |
| Browsing `S` | out-edges of S | "ProducesTo: Q, ConsumesFrom: Q" |

**Why not a tag on the node?** A tag like `ProducesTo: Q` on resource `S` renders fine on
S's page, but it is *not a real link*: the value `"Q"` is a free string (no foreign key, no
integrity, drifts on rename). Critically, it can only be read one direction — answering
"who produces to Q?" would require reverse-scanning every resource's tag values for `"Q"`.
That is the wrong tool for a 2am incident query. A typed edge answers it as a single
indexed read: `in-edges of Q where type = ProducesTo`.

**One-line takeaway:** *the connection is edge-shaped; the role is a property of that edge;
both viewpoints are renderings of one directed, typed edge.*

---

## 4. Real-world drivers behind this model

- **Retry logic** — services that both produce to and consume from the same queue. This is
  what makes the graph a *multigraph* rather than a simple graph.
- **Emergency DevOps / runaway queues** — under incident pressure, operators need to quickly
  find *which apps produce to Q* and *which consume from Q*. This is important enough that a
  hand-maintained wiki page already tracks it
  ([MacroPoint wiki 13215 — Queues/Services](https://dev.azure.com/dsgrandd/MacroPoint/_wiki/wikis/MacroPoint.wiki/13215/Queues-Services)).
  That page is effectively a **manually curated edge list** of the exact producer/consumer
  graph described here — strong evidence the relationship is genuinely typed and
  directional, and that the query off the *queue* end has real operational value.

---

## 5. Where the current model stands vs. the concept

The **node** half of the model is real and working. The **edge** half exists only as a
dormant skeleton — the right bone (a directed edge), but missing the type dimension the
concept requires, and under-constrained.

### What exists

- **Table** `Database/HTResourceMapperDb/Tables/ResourceDependency.sql` — a self-referencing
  junction:

  ```sql
  ResourceDependency(
      ResourceDependencyId,   -- PK
      ResourceId,             -- the dependent  ("from")
      DependencyResourceId,   -- the dependency ("to")
      CreatedOn, UpdatedOn )
  ```

  One row = "`ResourceId` depends on `DependencyResourceId`."

- **EF entity** `Modules/Common/ResourceMapper.Common.Server/Resources/Models/ResourceDependency.cs`
  with two navigation properties, so both traversal directions come from one row:
  - `Resource.Dependencies` → out-edges ("what this depends on")
  - `Resource.Dependents`   → in-edges ("what depends on this")

- **Import contract** `Modules/Common/ResourceMapper.Common.Shared/Import/Contracts/ImportContract.cs`
  accepts `List<string>? Dependencies` on each `ImportResourceItem` — a flat list of keys.

### What does NOT exist (dormant / unwired)

- `ImportService` **accepts but ignores** `Dependencies` — explicit "relationships are a
  later phase" comment. Nothing writes the table.
- No stored procedure reads or writes `ResourceDependency`; `Resource_GetItems` never
  touches it.
- No dependency data on the grid, editor, or any DTO (`ResourceDto` is an empty placeholder).
- No traversal (recursive CTE) for chains / impact analysis.

### The conceptual gaps (concept vs. current schema)

1. **No edge type.** Every edge means exactly "depends on." Cannot distinguish
   `ProducesTo` / `ConsumesFrom` / `DependsOn`. → This is the main gap; the type is what the
   incident query needs.
2. **No uniqueness constraint** on `(ResourceId, DependencyResourceId)` → duplicate edges
   possible, which would break the "single entry" expectation when browsing.
3. **No self-loop guard** → the schema allows `ResourceId = DependencyResourceId`, which the
   domain says should never happen.
4. **No edge attributes** beyond audit timestamps → no room for environment, message type,
   criticality, "hard vs. soft" dependency, etc., should those ever be needed.

**In one sentence:** the model is already a graph, but a *single-relation* one, whereas the
world it describes is *typed* — and the type is a property of the edge. On the point where
reality wants more (edge types) the schema is too thin; on the point where reality wants
less (no self-loops, no dupes) it is too loose.

---

## 6. Open questions (deliberately unanswered here)

These are noted for a future design conversation, not decided:

- Is each relationship type its own directed edge, or is "consumes" the stored inverse of
  "produces"? (Affects one-row-per-direction vs. derive-the-inverse.)
- Is the edge fundamentally **dependency-direction** ("needs to be healthy") or
  **data-flow-direction** ("records move this way")? Produce/consume answer "who points at
  whom" differently under each.
- Should node *type* constrain which edge types are legal between two nodes (e.g. a database
  doesn't "consume from" a service)?
- Do edges need attributes (environment, criticality), making this a *property* graph?
