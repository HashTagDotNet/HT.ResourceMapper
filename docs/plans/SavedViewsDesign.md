# Saved Views — design

Full CRUD for named grid queries, reached entirely from the application menu.

## The problem

A grid query today cannot be kept. `Home.razor` writes the current query string to a single
`ClientSetting` row (`home.gridView`) on every navigation, so that a later visit to a bare URL
resumes where you left off. The next filter change overwrites it. There is no way to name a
query, keep two of them, or return to one deliberately.

The only durable workaround is to bookmark the address bar, because the whole filter state
round-trips through the query string (`ResourceFilterState.ToQueryString` / `FromQueryString`).
That works, but it lives outside the application and tells you nothing about what a bookmark
contains.

## Decisions

| Decision | Choice |
|---|---|
| Identity | A single current owner behind an `ICurrentIdentity` seam — see below |
| Storage | A new `SavedView` table, modelled on `Diagram` |
| Scope | Grid queries only — the Explorer keeps its own diagram Save/Open |
| Sharing | None. The query string is already a shareable link |
| Placement | Entirely in the hamburger menu; no grid toolbar |
| Manage surface | A `/saved-views` page, alongside Resource Types / Domains / Tags |
| Manage features | Rename, delete, reorder, search, set default. No folders |
| Default view | One view per owner may be the default; it opens on a bare URL |
| Fallback | With no default set, resume the last view exactly as today |
| Open indicator | None on screen — the menu is the only place a view's state appears |

## Identity

Today there is no user. `ClientIdentity.GetOrCreateAsync` mints a random GUID into the browser's
`localStorage`, and everything personal hangs off that handle — which is why the Explorer surfaces
the id for copy-paste "recovery-key restore", a manual workaround for having no identity at all.

That is replaced with a seam:

```csharp
public interface ICurrentIdentity
{
    string OwnerId { get; }     // who owns saved views, diagrams, settings
}
```

The only implementation for now reads `ResourceMapper:Identity:OwnerId` from configuration and
falls back to the literal **`anonymous`** when unset. One deployment, one owner — which is
accurate: until an identity provider exists, every request really is the same person.

**All three personal surfaces move onto it** — saved views, Explorer diagrams, and the grid's
resume setting. Four consequences worth being explicit about:

- Diagrams and grid resume start following you between browsers and machines, which they never did.
- Diagrams already saved under a browser GUID become invisible, because they are keyed to an
  owner that no longer resolves. In a dev database that is a non-event; if any diagram ever matters,
  it is a one-line `UPDATE` to re-key it.
- **The Explorer's identity UI becomes dead and must be removed** — `ResourceExplorer.razor:71`
  renders `Client: xxxxxxxx...` with a copy button tooltipped "carry it to another browser". That
  affordance exists only to work around having no identity. `ClientIdentity.SetAsync` (the
  recovery-key restore, already caller-less) goes with it, and `GetOrCreateAsync` with it once both
  call sites move.
- **Prerender gets simpler.** `ClientIdentity` needed an interactive circuit because `localStorage`
  is unavailable during prerender, which is why `Home.razor` resolves it in `OnAfterRenderAsync`.
  A config-backed owner resolves server-side, so the default view can be applied during prerender
  rather than as a post-render correction.

**Column naming.** The new table uses `OwnerId`. `ClientSetting.ClientId` and `Diagram.ClientId`
should be renamed to match in the same pass — a column named `ClientId` that in fact holds an
owner is exactly the sort of stale name that misleads a year later. That touches the `Diagram_*`
and `ClientSetting_*` procedures and their repositories; contained, but real, and worth calling
out rather than discovering mid-implementation.

**What this does not do.** It is not authentication, authorisation or multi-tenancy. Anyone
reaching the app is the owner. It exists so that the *shape* of ownership is right now, and
swapping in a real provider later is one implementation rather than a schema migration across
three tables.

## Data model

```
[HTResourceMapper].[SavedView]
    SavedViewId   INT IDENTITY  PK
    SavedViewUid  VARCHAR(40)   UNIQUE        -- stable public id, as Diagram has
    OwnerId       VARCHAR(64)   NOT NULL      -- ICurrentIdentity.OwnerId ('anonymous' for now)
    Name          NVARCHAR(200) NOT NULL
    QueryString   NVARCHAR(MAX) NOT NULL      -- '?n=...&s=...&f=...', opaque to the server
    SortOrder     INT           NOT NULL DEFAULT 0
    IsDefault     BIT           NOT NULL DEFAULT 0
    CreatedOn     DateTime2(0)  NOT NULL DEFAULT SYSUTCDATETIME()
    UpdatedOn     DateTime2(0)  NULL

    UK_SavedView_Owner_Name   UNIQUE (OwnerId, Name)
    IX_SavedView_OwnerId      ON (OwnerId)                        -- the menu lists per owner
    UX_SavedView_Default      UNIQUE (OwnerId) WHERE IsDefault=1  -- at most one default
```

Two points carry weight here.

**`QueryString` is opaque.** The server stores and returns it without parsing, exactly as it
treats `Diagram.DiagramJson`. The filter serialisation can then change shape without a migration
or a server release; a view saved under an old format simply parses with
`FromQueryString`'s existing fail-safe behaviour (unknown tokens are skipped).

**At most one default is enforced by the database**, with the same filtered unique index that
`UX_ResourceTypeTag_DefaultPrimary` already uses. Setting a new default must clear the old one in
the same statement, or the index will reject it — which is the point: the constraint makes the
"two defaults" bug unrepresentable rather than relying on code remembering to clear.

## Stored procedures

Mirroring the `Diagram_*` set, plus the two the manage page needs:

| Procedure | Parameters |
|---|---|
| `SavedView_Upsert` | `@SavedViewUid, @OwnerId, @Name, @QueryString` — insert or update by uid; also serves rename |
| `SavedView_ListForOwner` | `@OwnerId` — ordered by `SortOrder`, then `Name` |
| `SavedView_Delete` | `@OwnerId, @SavedViewUid` |
| `SavedView_SetDefault` | `@OwnerId, @SavedViewUid` (nullable to clear) — clears then sets, in one transaction |
| `SavedView_Reorder` | `@OwnerId`, TVP of `(SavedViewUid, SortOrder)` — a drag commits in one round trip |

Every procedure takes `@OwnerId` and filters on it. With a single owner that is a no-op today,
but it means a real identity provider changes one implementation and nothing else.

## Server layer

Following the module pattern: `ISavedViewService` + `SavedViewSqlRepository` under
`Common.Server/SavedViews`, contracts in `Common.Shared/SavedViews/Contracts`, registered with
`TryAdd` semantics in `DependencyRegistration`. All results are `ApiServiceResponse<T>` built with
`ServiceResponseBuilder<T>`, with validation errors added through `builder.Validation`.

Validation the service owns:

- Name required, trimmed, 200 characters or fewer.
- Name unique per owner. The server returns a validation error on collision, but the UI should
  not rely on it for the overwrite flow: `Upsert` keys on uid, and the error carries no uid. The
  menu already holds the list, so `Save As` matches the name client-side and re-saves against the
  existing uid after confirming. Server validation stays as the backstop for a concurrent create.
- `QueryString` required and non-empty. Deliberately NOT length-capped: four filters over the
  current catalog's highest-cardinality tags already serialise to ~2100 characters, and that
  grows with the data. The column is NVARCHAR(MAX) for the same reason `Diagram.DiagramJson` is.

## UI

### The menu

The hamburger (`MainLayout.razor:16`) gains a `Saved Views` submenu:

```
Saved Views >
    Save                      disabled unless a view is open AND the query has changed
    Save As...
    -----------------
  * Development config        the default, starred; click opens it
    Prod queues
    LITE producers
    -----------------
    Manage saved views...
```

The listed views **are** the open affordance, so no Open dialog is needed — this is the main
simplification over the Explorer's version, which has a separate `OpenDiagramDialog`. Only
`Save As…` needs a dialog, for the name; `DiagramNameDialog.razor` is the existing pattern.

`Home.razor` tracks the open view's uid and its query string as-saved, so `Save` knows its target
and can tell changed from unchanged. Nothing about that state is shown on the page — by decision,
matching how Edge surfaces favourites.

### The manage page

`/saved-views`, built like the existing `ResourceTypes.razor` management page:

- A list ordered by `SortOrder`, drag to reorder, committing through `SavedView_Reorder`.
- Inline rename.
- Delete behind a confirm dialog, consistent with the rest of the app.
- A star toggle to set or clear the default.
- A search box, filtering client-side.

### Startup

On a bare URL, `Home.razor` resolves in this order:

1. A query string in the URL wins — a link or bookmark always shows what it says.
2. Otherwise, the owner's default saved view, if one is set.
3. Otherwise, the existing `home.gridView` resume behaviour, unchanged.

Step 3 means someone who never saves a view sees no difference from today.

**Stop writing the resume setting once a default exists.** With a default set, step 3 is
unreachable, yet `PersistViewAsync` would keep writing `home.gridView` on every navigation
forever — a write nothing ever reads. Gate it on there being no default. While in that code,
fold in the fire-and-forget `PersistViewAsync` call (PL-A10), which is in the same path.

**The startup gate gains a third branch.** `_resumeChecked` exists so the grid's default initial
load cannot overwrite a saved view before the restore has had its chance. Resolving a default view
adds a third state to that sequence, and it is the part of this feature most likely to produce a
subtle bug — a default that flashes then gets replaced by an unfiltered load, or a resume write
that lands before the restore. Treat it as its own step with its own test, not as a tweak.

## Testing

Unit tests for `SavedViewService` under `_Tests`, following `test-conventions.md`: xUnit + Moq
4.18.4 + FluentAssertions 7.1.0, `MethodName_StateUnderTest_ExpectedBehavior` naming,
`[Trait("Category", "Unit")]` at class level, a `because` clause on every assertion. Cover the
name validation rules, the duplicate-name path, and that `SetDefault` clears the previous default.

One e2e spec under `tools/e2e/tests`, covering the round trip: save a view, change filters, open
it again, rename it, reorder two, set a default, reload a bare URL and land on the default,
delete. Use the fixture pattern the existing specs share, with its own prefixed data and a
cleanup that touches nothing else.

## Constraints and things this does not do

**One owner, not real identity.** `ICurrentIdentity` returns a single configured `OwnerId`, so
everyone reaching this deployment is the same owner. There is no authentication, no authorisation
and no separation between people — the seam exists so ownership has the right *shape*, not so it
is enforced. Swapping in a real identity provider is on the roadmap, recorded as PL-63 in
`v1-punchlist.md`; because the rows already carry a stable owner, that becomes one implementation
rather than a migration across three tables.

**`ResourceFilterState.MaxFilters = 4` still applies.** Saving a view does not lift the cap on how
many filters a query may carry.

**No folders.** A flat, reorderable list. Folders would need a parent-child model and drag-between
-containers UI, and were explicitly deferred.

**No share tokens.** Unlike `Diagram`, a saved view has no `ShareId`; the query string is already
the shareable form.

## Parked

`Save`'s target is implicit, because nothing on screen names the open view. That is the accepted
consequence of putting everything in the menu, and it matches how Edge's favourites menu behaves.
Edge does have one signal this will not: a filled star in the address bar, saying the current page
is already a favourite without opening anything.

Parked as **PL-62** in `v1-punchlist.md` — deliberately not built now, so the menu-only shape gets
used as designed first. The smallest remedy, if it proves confusing, is a name chip on the filter
bar carrying the view's name and a changed marker.
