# Rename HT.ResourceMapper to MacroPoint.Azure.ResourceExplorer

**Date:** 2026-09-14
**Branch:** `rename-to-macropoint-azure-resourceexplorer` (cut from `home-page`)

## Goal

Rebrand the entire solution from `HT.ResourceMapper` / `ResourceMapper` to
`MacroPoint.Azure.ResourceExplorer`. Projects, namespaces, assemblies, the solution file, the SQL
schema, the database, configuration keys and all documentation.

**The bar:** when this is done, the strings `HT.ResourceMapper`, `HTResourceMapper`, `ResourceMapper`
and the `HT.` namespace prefix appear nowhere in the working tree. Three exceptions are out of
scope and stay as they are:

- the GitHub repository name (`HashTagDotNet/HT.ResourceMapper`)
- git branch names and history
- the filesystem folder the repo is cloned into (`C:\src\HT.ResourceMapper`)

This is a pure rename. No behaviour changes, no refactoring, no bug fixes.

## Decisions

These were settled before work began and are not open for re-litigation during execution.

| Question | Decision |
|---|---|
| Prefix for the generic `HT.*` libraries | `MacroPoint.Azure.ResourceExplorer.*` — one root, no exceptions |
| SQL schema | `[HTResourceMapper]` becomes `[ResourceExplorer]` |
| Database | New `ResourceExplorer`; old `ResourceMapper` left untouched |
| Existing data | Not migrated — rebuilt from the live catalog seed (see Reseed) |
| Configuration root | `MacroPoint:Azure:ResourceExplorer:` |
| Documentation | Rewritten everywhere, including `docs/archive/` and `__ProjectNotes/` |
| Folder names | Match the full project name |
| `HT.Api.Contracts.csproj` | Deleted (orphan; see Cleanup) |

### Why the full root on the utility libraries

`MacroPoint.Azure.ResourceExplorer.Collections` names a general-purpose collection helper after one
application. That is a real cost, accepted deliberately: a single root means there is exactly one
rule to apply and no judgement call about which library counts as "generic". Should these libraries
ever be extracted for reuse, renaming them again is a smaller job than the one being done here.

### Path length

Windows `LongPathsEnabled` is `1` on the development machine, and this was verified empirically
rather than assumed: a throwaway project built at the longest name the rename produces generated 18
files over 260 characters (longest 314) and compiled without error under the .NET SDK. Folder names
therefore carry the full project name. The `.sqlproj` — the one project that builds under
full-framework MSBuild rather than the SDK — sits at a much shorter path and is unaffected.

## Name map

### Projects, assemblies, namespaces

Every name below is simultaneously the directory name, the `.csproj` filename, the assembly name and
the root namespace. Sub-namespaces within each project (`.Explorer`, `.SavedViews`, `.HomePage`,
`.Editor`, `.Resources`, `.Settings`, `.Tags`, `.Domains`, `.Cascade`, `.Import`, `.Export`,
`.Identity`, `.Config`, `.Utils`, `.Interfaces`, `.Models`, `.Contracts`) are unchanged.

For brevity below, `MAR.` stands for the literal prefix `MacroPoint.Azure.ResourceExplorer.` — it is
never abbreviated in the code itself.

| Area | Old | New |
|---|---|---|
| ApiContracts | `HT.Api.Client.Contracts` | `MAR.Api.Client.Contracts` |
| ApiContracts | `HT.Api.Service.Contracts` | `MAR.Api.Service.Contracts` |
| Collections | `HT.Collections` | `MAR.Collections` |
| Configuration | `HT.Microsoft.IConfiguration.Extensions` | `MAR.Configuration.Extensions` |
| Logging | `HT.Microsoft.ILogger.Extensions` | `MAR.Logging.Extensions` |
| SqlClient | `HT.Microsoft.SqlClient.Extensions` | `MAR.SqlClient.Extensions` |
| SqlClient | `HT.Microsoft.SqlClient.Extensions.Abstractions` | `MAR.SqlClient.Extensions.Abstractions` |

Those four are the one place the mechanical transformation could not be applied, and the reason is
worth recording because it is a direct consequence of the single-root decision.

**`Microsoft` cannot be a segment under the shared root.** C# resolves the first segment of a
qualified name against enclosing namespaces before the global one. Once the libraries became
`MAR.Microsoft.*`, every file under `MacroPoint.Azure.ResourceExplorer` that wrote
`using Microsoft.JSInterop` resolved `Microsoft` to *our* namespace, found no `JSInterop` in it, and
stopped looking — shadowing the entire `Microsoft.*` framework. This produced 228 compiler errors
from one cause. It never happened before the rename because the libraries (`HT.*`) and the
application (`ResourceMapper.*`) had different roots; unifying them created the collision. The
segment is dropped.

**`IConfiguration` and `ILogger` cannot be segments either.** They are type names. As namespace
segments beneath the shared root they shadow the types themselves, so `IConfiguration config` fails
to compile with *"'IConfiguration' is a namespace but is used like a type"*. They become
`Configuration` and `Logging`, matching the folders those libraries already live in.

The alternative in both cases was `global::` qualification at several hundred call sites. Renaming
four namespace segments is the smaller and more durable change, and it leaves the root — the part
that was actually chosen — untouched.
| Modules | `ResourceMapper.Common.Server` | `MAR.Common.Server` |
| Modules | `ResourceMapper.Common.Shared` | `MAR.Common.Shared` |
| UI | `ResourceMapper.UI.Web` | `MAR.UI.Web` |
| Database | `HTResourceMapperDb` | `MAR.Db` |

Test projects take the same transformation with their `.Tests` suffix intact, and their namespaces
follow (`HT.Api.Service.Contracts.Tests.BuildersOfTTests` becomes
`MAR.Api.Service.Contracts.Tests.BuildersOfTTests`).

Two directories currently disagree with the project they contain; both are corrected so that folder
and project name match:

- `Libraries/ApiContracts/HT.ApiContracts/` holds `HT.Api.Client.Contracts.csproj`
- `Libraries/SqlClient/HT.Microsoft.SqlClient.Abstractions/` holds
  `HT.Microsoft.SqlClient.Extensions.Abstractions.csproj`

### Solution and build

| Item | Old | New |
|---|---|---|
| Solution file | `HT.ResourceMapper.slnx` | `MacroPoint.Azure.ResourceExplorer.slnx` |
| MSBuild TFM property | `$(HTTargetFramework)` | `$(ResourceExplorerTargetFramework)` |
| User secrets id | `1a3b74ec-…` | unchanged |

The secrets id is deliberately preserved so the existing secrets file is still discovered.

### Database

| Item | Old | New |
|---|---|---|
| Database | `ResourceMapper` | `ResourceExplorer` |
| SQL schema | `[HTResourceMapper]` | `[ResourceExplorer]` |
| Schema script | `Security/HTResourceMapper.sql` | `Security/ResourceExplorer.sql` |
| Deploy script name | `HTResourceMapperDb.sql` | `MacroPoint.Azure.ResourceExplorer.Db.sql` |

Stored procedure, table, view and user-defined-type names are unchanged — only their schema
qualifier moves.

`[HT.Services]` names a database that does not exist in this solution. Every occurrence is already
commented out — four `GRANT` lines in the security script and a `--USE [HT.Services]` header in
three seed scripts. All are deleted rather than renamed; pointing them at `ResourceExplorer` would
manufacture a claim that was never true. One further mention, of a long-removed
`Database\HT.Services.sqlproj`, survives in `docs/plans/editor/01-data-foundation.md` as a record of
files that were deleted. That sentence is reworded so it no longer carries the dead name, since
naming the removed project precisely adds nothing to the history.

### Configuration

| Old key | New key |
|---|---|
| `ResourceMapper:ConnectionStrings:Database:RO` | `MacroPoint:Azure:ResourceExplorer:ConnectionStrings:Database:RO` |
| `ResourceMapper:ConnectionStrings:Database:RW` | `MacroPoint:Azure:ResourceExplorer:ConnectionStrings:Database:RW` |
| `ResourceMapper:Identity:OwnerId` | `MacroPoint:Azure:ResourceExplorer:Identity:OwnerId` |

This lands in `appsettings.json`, `appsettings.Development.json`,
`appsettings.localhost.json`, the strings passed to `GetString` in server code, and the developer's
user-secrets file.

The user-secrets file lives outside the repository at
`%APPDATA%\Microsoft\UserSecrets\1a3b74ec-…\secrets.json`. Because the secrets id does not change,
the file is still loaded — but its two keys carry the old prefix and its values carry
`Database=ResourceMapper`, so both the key and the value need rewriting or the application starts
with no connection string. The original is copied to the scratchpad first; restoring that one file
returns the machine to its current state.

### Tooling and ancillary

| Item | Old | New |
|---|---|---|
| e2e npm package | `ht-resourcemapper-e2e` | `macropoint-azure-resourceexplorer-e2e` |
| e2e database constant | `ResourceMapper` | `ResourceExplorer` |
| e2e cleanup SQL schema refs | `HTResourceMapper.` | `ResourceExplorer.` |
| Bruno collection | `ResourceMapper` | `MacroPoint Azure Resource Explorer` |
| VS Code launch/tasks | `ResourceMapper.UI.Server` | `MAR.UI.Web` |

The VS Code configuration currently points at `ResourceMapper.UI.Server`, a project that no longer
exists; the rename corrects it to the real host as a side effect.

### Documentation

All markdown is rewritten: `README.md`, `CLAUDE.md`, `test-conventions.md`, `docs/plans/**`,
`docs/archive/**`, `__ProjectNotes/**`, `tools/e2e/README.md` and
`Database/**/Live_Seed_README.md`. `CLAUDE.md` additionally needs its stale reference to
`UI/ResourceMapper.UI.Server` corrected to the real host project, and its `$(HTTargetFramework)`
mention updated.

## Cleanup

`Libraries/ApiContracts/HT.ApiContracts/` contains two project files:
`HT.Api.Client.Contracts.csproj`, which is in the solution and referenced throughout, and
`HT.Api.Contracts.csproj`, which is tracked by git, belongs to no solution and is referenced by
nothing. Two projects in one directory contend for the same `obj/` and `bin/`. The orphan is
deleted rather than carried forward as a renamed duplicate.

## Reseed

The rename creates an empty database, and the DACPAC post-deployment script seeds only reference
data — two `TagContentType` rows and the `Domain` tag definition. That is not enough to resume
development on, so repopulating the catalog is part of this work, not a follow-up.

The catalog is rebuilt with the existing live-seed pipeline documented in
`Database/**/Live_Seed_README.md`:

1. Publish the `.dacpac` to the new database. Creates the schema and runs the reference seed.
2. `Live_Seed_Vocabulary.sql` — resource types, tag definitions, per-type tag templates.
3. `LiveCatalog.json` POSTed to `/api/resources/import` on the running application. The endpoint
   accepts the import document directly, so this needs no browser automation.
4. `Live_Seed_PostImport.sql` — cross-domain dependency edges and primary-link wiring.

`Live_Purge_Catalog.sql` (step 1 of the documented pipeline) is skipped; the new database is
already empty.

Two tables are not reproducible from any script and are copied across from the old database, which
sits on the same localdb instance:

- `SavedView` — the developer's saved view
- `ClientSetting` — accumulated UI state

`Diagram` is empty and needs nothing.

## Execution

Ordering matters in two places: paths move before content is rewritten, and the content rewrite
processes tokens longest-first so that no replacement corrupts a longer token containing it.

1. **Delete the orphan project file.**
2. **Move paths** with `git mv` — directories, `.csproj`, `.sqlproj`, the `.slnx`, and the SQL
   security script.
3. **Rewrite content** across all tracked text files, applying these substitutions in exactly this
   order — longest token first, so that no replacement corrupts a longer token containing it:

   | # | Find | Replace with |
   |--:|---|---|
   | 1 | `HTResourceMapperDb` | `MacroPoint.Azure.ResourceExplorer.Db` |
   | 2 | `HT.ResourceMapper` | `MacroPoint.Azure.ResourceExplorer` |
   | 3 | `HTResourceMapper` | `ResourceExplorer` |
   | 4 | `HTTargetFramework` | `ResourceExplorerTargetFramework` |
   | 5 | `HT.ApiContracts` | `MacroPoint.Azure.ResourceExplorer.Api.Client.Contracts` |
   | 6 | `HT.Microsoft.SqlClient.Abstractions` | `MacroPoint.Azure.ResourceExplorer.Microsoft.SqlClient.Extensions.Abstractions` |
   | 7 | `HT.Api.` | `MacroPoint.Azure.ResourceExplorer.Api.` |
   | 8 | `HT.Microsoft.` | `MacroPoint.Azure.ResourceExplorer.Microsoft.` |
   | 9 | `HT.Collections` | `MacroPoint.Azure.ResourceExplorer.Collections` |
   | 10 | `ResourceMapper.` | `MacroPoint.Azure.ResourceExplorer.` |
   | 11 | `ResourceMapper` | context-dependent — see below |

   Substitution 3 yields the SQL schema name, which is deliberately the short `ResourceExplorer`
   rather than the full root. Reversing any pair corrupts the result: rule 3 before rule 1 leaves a
   mangled `ResourceExplorerDb`, and rule 10 before rule 2 leaves a stranded `HT` prefix.

   Rules 5 and 6 exist because two directory names disagree with the project they contain, and the
   generic rules would not repair the difference. `HT.Microsoft.SqlClient.Abstractions` is the
   directory holding `HT.Microsoft.SqlClient.Extensions.Abstractions.csproj`, and it is named in the
   `.slnx` and in two `ProjectReference` paths. Rule 8 alone would rewrite those to a directory that
   does not exist and every reference would dangle, so rule 6 must precede it.

   Rule 10 has no single answer and is applied by inspection, not by script. The remaining bare
   occurrences are the database name (`ResourceExplorer`), the configuration root
   (`MacroPoint:Azure:ResourceExplorer`), the Bruno collection title
   (`MacroPoint Azure Resource Explorer`), the npm package name
   (`macropoint-azure-resourceexplorer-e2e`) and prose in documentation (`Resource Explorer`).
   Each is listed in the name map above; the string sweep gate proves none was missed.
4. **Rewrite the `.slnx`** project paths and folder names.
5. **Rewrite the database publish profile** — target database name and deploy script name.
6. **Rewrite the user-secrets file**, after copying it to the scratchpad.
7. **Drop the colliding namespace segments** — `Microsoft`, then `IConfiguration` and `ILogger` —
   in a second and third pass, renaming their directories and project files to match. See the note
   under the name map for why these cannot survive under the shared root.
8. **Re-nest the JSON configuration by hand.** The colon-delimited rule rewrites
   `ResourceMapper:ConnectionStrings` in code and in user-secrets, but `appsettings.*.json`
   expresses the same key as *nested objects* — `"ResourceMapper": { "ConnectionStrings": ... }` —
   where no colon appears next to the token. The bulk rule therefore leaves a structurally wrong
   file that still passes a string sweep: no old token remains, yet the key the code reads no
   longer exists. The block must be re-nested as
   `"MacroPoint": { "Azure": { "ResourceExplorer": { ... } } }`. Only `appsettings.localhost.json`
   carries such a block today; check the others whenever this is repeated.

Two traps are worth calling out because neither is visible to the compiler or the string sweep.
**The worktree's `.git` is a file, not a directory**, holding a `gitdir:` path that contains the
repository folder name — a `--exclude-dir=.git` does not protect it, and rewriting it detaches the
worktree from its repository. Exclude it explicitly. And **`ResourceUid` is regenerated by the
import**, so it cannot be used to compare the old and new databases, nor to carry rows across
between them.

`HTTP`, `HTML`, `HTMLElement` and `HTMLClient` must survive untouched. Every token in the list above
is either dot-qualified or a longer word, so none of them matches these — but the verification step
below checks explicitly rather than trusting that argument.

## Verification

Each gate must pass before the next is attempted. Failures are reported, not worked around.

| Gate | Criterion |
|---|---|
| Solution builds | `dotnet build` on the UI host: 0 errors |
| Database builds | full MSBuild on the `.sqlproj`: `.dacpac` produced |
| Tests | 555 total, 553 pass, the same 2 pre-existing failures |
| Publish | `.dacpac` deploys to the new database via SqlPackage |
| Reseed | row counts match the table below |
| Application | starts, home page lists resources, editor and explorer open |
| String sweep | no matches for the old tokens; `HTTP`/`HTML` intact |

### Baseline

Captured on `home-page` at `e483b11` before any change, so that post-rename results are
attributable.

| Test project | Result |
|---|---|
| `Api.Client.Contracts.Tests` | 25 pass |
| `Api.Service.Contracts.Tests` | 210 pass, **2 fail** |
| `Common.Server.Tests` | 244 pass |
| `Common.Shared.Tests` | 74 pass |

The two failures assert that `CallStatusCode.NotFound` maps to HTTP 404; it currently maps to 422.
The defect predates this work and is out of scope — fixing it inside a rename would both obscure the
diff and hide exactly the kind of regression this rename could introduce. The rename must leave
these two failing for the same reason, and everything else passing.

### Reseed row counts

Taken from the current database as the shape to reproduce. These are a comparison target for this
one migration, not a specification of correct catalog size.

| Table | Expected |
|---|---:|
| Resource | 147 |
| ResourceRelationship | 232 |
| ResourceTag | 773 |
| ResourceType | 11 |
| TagDefinition | 19 |
| ResourceTypeTag | 25 |
| SavedView | 1 |
| ClientSetting | 37 |

## Risks

**A partial rename that still compiles.** The danger is not a build break — that is loud and easy to
fix. It is a string in a place the compiler never checks: a sproc name in a repository call, a
configuration key, a tag `DisplayName`, a Blazor route. The string sweep gate exists for exactly
this, and the reseed exercises the full stack through a real 147-resource import rather than trusting
a green build.

**The old database is the only copy of the developer's data.** Nothing in this plan writes to
`ResourceMapper`. The two script-less tables are read out of it, never modified. If the reseed
produces the wrong shape, the old database and the original secrets file together restore the
current working setup.

**Work in flight.** This rename touches essentially every file, so it conflicts with anything else
on `home-page`. It should land as a single commit and be merged promptly rather than left to age.
