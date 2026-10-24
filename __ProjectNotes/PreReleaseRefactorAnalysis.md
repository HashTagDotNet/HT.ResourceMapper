# Pre-Release Refactoring — Business Analysis

A reading of `PreReleaseRefactorObservations.md` as business analysis and UX, plus a review of
what we actually built against it. The observations file stays raw; this is the interpretation,
and it is mine, not Steve's.

Scope note: this is analysis, not a plan. It names what the refactor has to decide. It does not
sequence work or propose an implementation.

---

## 1. What the observations are actually describing

Read as a data-model wishlist, the fifteen observations look like a list of missing columns. Read
as business analysis, they are all one thing:

> **The catalog's job is not to list resources. It is to answer "something is wrong with X in
> environment Y — where do I look?" in as few moves as possible.**

Every observation is a place where the current design makes that question take too many moves.
The tier filter is missing, so you cannot narrow by where. The taxonomy is flat, so you cannot
narrow by what-it-is-part-of. The links are buried in a tag list, so the last move — leaving the
catalog for the real tool — is the clumsiest one.

### Two customers, two addressing schemes

**App engineers think in projects. IT engineers think in Azure names.** Both are primary; neither
is a translation layer over the other, and the catalog is the only place they meet.

This is the correction to an earlier draft of this document, which claimed nobody thinks in Azure
resource names and treated the logical hierarchy as the one true address. That was wrong, and it
would have produced a catalog that served half its audience. `usspi-my-cache-resource` is not
noise an app engineer has to tolerate — it is an IT engineer's primary key.

It also raises the stakes on the hierarchy question. If the logical path were merely a nicer
label, it could stay a presentation concern. Because it is one of two genuine addressing schemes,
the catalog has to hold **both** and let each audience search, sort and group in its own idiom —
and, crucially, translate between them. Translation is close to being the product. A wiki can
translate too, but only along the paths someone thought to write down; see §1a for why that
distinction is the whole argument for building this at all.

That reframing matters because it changes what "done" looks like. A richer tag model that still
takes six interactions to reach App Insights for the right application in preprod has not moved.

### The personas this implies

Steve is not the only user. The observations mix five points of view, which split across the two
customers above.

_Widen the terminal if this renders as stacked Key: value records._

| Persona | The question they arrive with | What they need |
|---|---|---|
| **Incident responder** (app) | "Alerting is failing in preprod — where do I look?" | Narrow by system + environment, then one click out with the query already applied |
| **New developer** (app) | "What *is* Trigger Ingestor made of?" | Containment: the application and its 1..N real resources |
| **Change impact** (either) | "If I touch this database, who breaks?" | The graph, including across system boundaries |
| **IT engineer** (infra) | "What is impacted by this resource?" · "Open it so I can manage it" · "What logs is it producing?" | Arrive by Azure name, leave into the tool that acts — impact, portal, telemetry |
| **Estate owner** (either) | "Is this catalog still true?" | Vocabulary curation, drift detection, import |

The incident responder is the one the observations keep circling back to, and the one the current
UI serves worst. The estate owner is the only persona the current UI serves well; the tag
definition, resource type and domain-value admin screens are the most complete part of the app.

The IT engineer's questions are worth dwelling on, because they are **operational, not
descriptive**. This persona does not arrive wanting to know what a resource *is*. They arrive
wanting to act on it, and the catalog's only job is to hand them off to the tool that acts — with
enough context that the handoff lands in the right place. The three questions Steve gave are not
variations on one need; they are three different mechanisms, at three different levels of
readiness:

_Widen the terminal if this renders as stacked Key: value records._

| The question | The mechanism | State today |
|---|---|---|
| "Open it so I can manage it" | Click-through link | **Served** — `PrimaryTagDefinitionId`, seeded per type from the `IsDefaultPrimary` template, Portal everywhere it exists |
| "What is impacted by this?" | The graph, traversed backwards | **Partial** — both directions exist, but one hop only, and only inside the editor |
| "What logs is it producing?" | App Insights + its filter selector | **Blocked** — the selector can't say which application or environment it belongs to |

Note that the list is explicitly open ("some questions"), so this is the shape of the need, not
its extent.

That is the central UX imbalance: **we built the curation tool thoroughly and the consumption tool
thinly.** Pre-release, the consumption side is what everyone else will judge it by.

---

## 1a. The wiki question

Steve's response to the framing above: *handing an engineer off to the tool that acts is largely
what the wiki already provides.*

That is the right challenge to put to this product, and it should be answered before the refactor
sets its scope, because a large part of what the catalog does today is a wiki page in a database.
Resource, description, a handful of hand-typed links — a wiki page has all of that, and a better
editor for it.

The honest version of the distinction is not "the catalog has links too". It is:

**A wiki gives you one hand-authored path through the estate. A catalog gives you every path, and
can tell when a path has gone stale.**

Concretely, the things a wiki structurally cannot do:

- **Arrive from any direction.** A wiki page is reached the way its author imagined. The same fact
  in the catalog is reachable by Azure name, by system, by environment, by type, by tag — which is
  exactly Steve's "different ways to sort, slice and group".
- **Answer a question nobody wrote down.** "Every non-prod Redis with no owner" is a query here
  and a re-read of forty pages there.
- **Traverse.** Impact analysis is a graph walk. A wiki can list dependencies; it cannot close
  over them.
- **Be checked against reality.** An import from Azure can prove the catalog wrong. Nothing can
  prove a wiki page wrong except a human noticing.
- **Derive rather than store.** A templated link is correct for every resource of its type
  forever. A pasted link is correct until something moves.

That last point is the sharp one, and it cuts directly at the current design: **if links stay
hand-authored per resource as tag values, the catalog is a wiki with a worse editor.** Per-type
link templates parameterised by placement can't drift, can't be forgotten on a new resource, and
stay right when an environment is added. Pasted URLs do none of that.

**Wayfinding and the graph carry equal weight here, and the graph clears the wiki bar more
cleanly.** Derived links beat a wiki; a traversable graph is something a wiki has no version of at
all. A wiki page can list what a thing depends on — a sentence a human typed — but it cannot
close over those edges, cannot be asked the reverse question, and cannot answer "if this dies,
what else dies" for anything more than one hop. Between the two, the graph is also in the worse
state today (§3.2): links are at least authored through the UI, whereas an edge that crosses the
identity boundary can only be added by editing a post-deploy script.

So the test for the rest of the refactor is: **for each thing we are considering, would a wiki
page do it as well?** If yes, it is not where the effort belongs. Templated wayfinding and a real
graph both pass it. A links column filled by hand does not.

## 2. The one structural finding

Three kinds of fact are currently stored in one mechanism, and they have different lifecycles,
different owners, and different jobs.

_Widen the terminal if this renders as stacked Key: value records._

| Kind of fact | Examples | Why tags are wrong for it |
|---|---|---|
| **Placement** | system, sub-system, application, environment, subscription, tier | Closed vocabulary, drives grouping and filtering, and the values relate to *each other* — a string can't say Alerting is inside vNext |
| **Wayfinding** | portal links, App Insights filters, config prefixes, cache keys, DB app names | Belongs to a resource *type*, parameterised by placement; the value is derived, not authored per resource |
| **Description** | everything ad-hoc, owner, notes, cost centre, imported Azure tags | Genuinely open-ended. Tags are exactly right here |

Tags are the correct home for the third kind and a poor home for the first two. That is what "a
slightly more defined taxonomy is really necessary" means when you unpack it — not *more* tags,
and not *stricter* tags, but **lifting placement and wayfinding out of tags entirely**, leaving
tags to do the one job they are good at.

Steve's own "hierarchy, graph, and random tags etc." is the same conclusion reached from the other
direction. The three mechanisms map onto the three kinds of fact almost exactly:

- hierarchy → placement
- graph → dependency and sharing
- tags → description

Wayfinding is the one that does not have a home in that list, and it is the one generating the
most friction in the observations (links cumbersome, selectors varying per resource type,
environment-dependent URLs). **It needs to be named as its own concern, or it will silently stay
in tags.**

---

## 3. How we built it — review against the above

### 3.1 The shape we have

There is one `Resource` table. Everything structural is a `TagDefinition` plus a string value in
`ResourceTag`. There is no parent, no path, no containment column anywhere in the schema. System
and Application are *resource types* (`SYS`, `APL`), and membership in them is expressed by a text
tag whose value is matched by spelling.

This is a defensible thing to have built — it made the vocabulary fully user-editable without
schema changes, which is what the earlier design was optimising for. It is also precisely the
thing the observations are pushing back on.

### 3.2 Findings

**The `System` tag already carries two of Steve's levels at once.** Its allowed values are
`["Visibility","Alerting","LITE"]`. In Steve's account Alerting is a *sub-system* of vNext, and
LITE is a *system*. One flat tag is holding two levels of a hierarchy that has not been modelled,
so the values silently span tiers of meaning. Anything that groups by `System` today produces a
list that mixes the two.

**Tier is not missing from the filter bar — it is deliberately suppressed.**
`ResourceSqlRepository.GetAllTagKeysAsync` excludes any tag with `IsSystemTag` or `IsDomainTag`
before handing the list to the filter picker. `Domain` (display name "Tier") is the only domain
tag, so it is the only thing that exclusion removes. The comment explains the intent — internal
plumbing should not leak into the UI — and the intent is right. The problem is that Tier is not
only plumbing.

**Tier is doing two incompatible jobs.** It is the identity boundary (resource uniqueness is
`Domain + Type + Key`, code-enforced) *and* a user-facing facet named "Tier". Those jobs pull
opposite ways: a boundary wants to be invisible and immutable; a facet wants to be filterable and
browsable. The suppression above is the first job winning. The first observation is the second job
losing. **Splitting these two roles is the single highest-leverage change in the whole list**, and
it is upstream of several other findings.

**The boundary actively blocks the graph.** `Live_Seed_PostImport.sql` states it plainly: import
resolves every dependency key inside the dependent's own domain, so an Application (domain
`shared`) depending on an App Service (`prod`) "cannot be expressed in the JSON at all — it is not
a warning, it fails the whole import." The workaround is a hand-maintained `VALUES` list of edges
in a post-deploy script. Steve's *Applications depend on applications* is exactly this shape, and
today the only way to record it is to edit SQL. That is not a gap in the import format; it is the
identity boundary and the dependency graph disagreeing about what a resource is.

**The graph is partly carried in strings.** Producer/Consumer are text tags holding application
*names*, joined onto `Resource.ResourceName` to synthesise queue edges. The seed script's own
comment warns that spelling drift makes edges "silently stop appearing", and ships a diagnostic
query to detect it. A real edge table exists (`ResourceRelationship`) — untyped by deliberate
deferral — so the catalog has both a graph and a shadow graph made of strings.

**Selectors are already there, and already unstructured.** `ConfigPrefix`, `AppInsightsFilter`,
`DbAppName`, `ServiceFabricApp`, `Container`, `Search` are all multi-valued free-text tags. So
*Shared resources carry a selector* is, mechanically, already built. What is missing is the
association: a shared App Insights resource carrying four filter values cannot say which
application or environment each one belongs to. The values are a list, and the relationship they
encode is lost.

**Links cannot vary by environment, because links hang off resources and applications span
environments.** Links are tags of content type `Link`, stored flat per resource, with one
`PrimaryTagDefinitionId` as the click-through. The post-import seed shows the application `vnext`
with edges to three App Services — dev, preprod and prod. So an application row genuinely cannot
hold one portal link, and *Links are cumbersome and environment-dependent* is a real structural
limit, not a display preference.

**The Tags column is a junk drawer, and that is why links feel cumbersome.** The home grid's
columns are Name, Type, Description, Tags, actions. Placement, wayfinding and description all
render inside that one cell, differentiated only by whether a value is a chip or `key: value`
text. A links column would help, but the durable problem is that one cell is showing three
unrelated kinds of fact. (There is already a links menu in the actions column, so links are
rendered in two places at once.)

**Blast radius is one hop deep, and lives in the editor.**
`ResourceRelationship_GetForResource` returns both directions in one result set — `DependsOn`
out-edges and `DependentOn` in-edges — so the reverse traversal the IT engineer needs is there.
Two limits. It is a single hop: "what is impacted by this resource" is transitive in the asking
(if the cache goes, the ingestor goes, and whatever the ingestor feeds goes with it) and
non-transitive in the answer. And it surfaces in the resource editor's Dependent On tab plus the
delete-confirmation list, so **you have to open a thing for editing to find out what it would
break** — an operational read-only question routed through an authoring surface. The edges are
also untyped by deliberate deferral, so "impacted" cannot distinguish *breaks* from *degrades*.

**The Azure name is already a taxonomy, encoded positionally.** The App Service keys in the
post-import seed —
`usspi-aps-dt-mplt-dev-visibility-macropoint-com`, `usspi-aps-pp-mplt-preprod-vnextproxy-…`,
`usspi-aps-pd-mplt-vnextproxy-…` — vary in a regular way across a subscription prefix, a
type segment (`aps`), a tier segment (`dt` / `pp` / `pd`) and an environment segment. I am reading
that off three examples, so treat the exact decoding as a hypothesis rather than a spec; the shape
is not in doubt. The significance is that **the IT engineer's naming convention encodes most of
the placement facts the taxonomy is trying to introduce.** That makes it both a validation source
(a resource whose tags disagree with its own name is suspect) and the natural bridge between the
two customers — which is the translation job named in §1.

Worth noting the catalog has half a decoder ring already: `ResourceType.ShortCode` holds `APP`,
`AI`, `APC`, `SB`, `SF` and so on. These are *close to* the Azure name segments without matching
them (`APP` vs `aps`, `SBQ` vs a queue's segment). If the name is ever parsed, that near-miss is
where the bugs will live.

**Two seed scripts disagree about resource type names.** `05_Seed_ResourceTypeCodes.sql` joins on
`TypeName` using `'Azure App Service'`, `'Azure App Config'`, `'Azure ServcieFabric'` (sic);
`Live_Seed_Vocabulary.sql` creates those types as `'App Service'`, `'App Configuration'`,
`'Service Fabric'`. The join matches nothing for the live catalog, so that script's `ShortCode`
and `IconKey` updates silently no-op — an `UPDATE … FROM … INNER JOIN` affecting zero rows
succeeds. The live vocabulary sets `ShortCode` itself, so nothing is visibly broken today, which
is exactly why it has gone unnoticed. `IconKey` is not set by the live seed at all, and is
deliberately left NULL there. Worth deciding whether the older script is dead and should go.

### 3.3 Where the build is genuinely good

Worth stating, because a refactor should not throw these away:

- The facet counts respect the other active filters but never constrain themselves — that is the
  correct and non-obvious behaviour, and it is implemented in both the sproc and the service.
- Saved views, URL round-tripping of filter state, and per-client resume are all done, and they
  are exactly the mechanism a "different ways to sort, slice and group" story needs to build on.
- `ResourceTypeTag` already models "which tags does this type expect, and which is its default
  click-through" — a per-type template. That is the right shape for wayfinding, one level away
  from being parameterised.
- The seed scripts are honest. Every workaround above is documented in a comment that explains
  why. That is why this review could be written quickly.

---

## 4. What the refactor has to decide

Not a plan — the questions whose answers determine everything else.

**Does placement become structure, or stay tags?** Everything else follows from this. If system /
sub-system / application become real containment, the `System` tag's level-conflation, the grid's
grouping story, and "what is an application row" all resolve together. If they stay tags, each is
solved separately and the conflation stays.

**Does the identity boundary stop being Tier?** Tier cannot be both an invisible uniqueness scope
and a user-facing facet. Until this splits, the filter cannot be exposed and cross-boundary edges
keep needing hand-written SQL.

**What is the environment axis?** Tier, Environment and Subscription currently overlap. Steve's
own example — "tier (e.g. named subscription)" — uses one word for what the seed treats as three
things. Pick which is the primary axis a human filters on; the others become attributes of it.

**Where does wayfinding live?** If links and selectors stay as per-resource tag values, they
cannot vary by environment and cannot be associated with an application. If they become per-type
templates parameterised by placement (`https://{env}-macropoint.com`), the environment-dependent
link problem and the shared-selector problem are the same problem with one answer.

**Does an application row show its own links or its resources' links?** Answered indirectly —
applications are rows, so they carry links. But an application spanning three environments has
three of everything, which is the same parameterisation question again.

**What does the graph have to become?** Four sub-decisions, all currently answered by deferral:
can an edge be authored across the identity boundary without editing SQL; are edges typed (so
"impacted" can separate *breaks* from *degrades*); is traversal transitive; and does impact get a
read-only surface of its own rather than living in the editor. The producer/consumer shadow graph
— application names string-joined onto `ResourceName` — is a fifth, and it either becomes real
edges or stays a documented drift risk.

**Is the Azure name a first-class address, or a field?** Both customers' addressing schemes have
to be searchable, sortable and groupable in their own idiom. Today the Azure name is
`ResourceKey`, one searchable string among several. If the naming convention is treated as
structure — parsed, or at least validated against the placement tags — the catalog can translate
between the two audiences and flag drift. If it stays an opaque string, translation stays manual
and the IT engineer keeps using the portal.

**Which direction does the Azure tag import flow?** Read-only mirror, one-time seed, or two-way
sync. Each implies a different answer to "what happens when the catalog and Azure disagree", and
imported Azure tags are the description-kind of fact, so they should not be allowed to land in the
placement vocabulary.

---

## 5. The thing I would push back on

Steve said the tags-with-links model "works" and then, later, that links in the tag list are
"cumbersome". Both are true, and the gap between them is the finding: the model works for the
person who *built* the catalog and is cumbersome for the person *using* it. The curation tooling
is complete and the consumption tooling is a single grid with a junk-drawer column.

If the refactor only makes the taxonomy more defined, it improves the experience of the persona
already best served. The observations that matter most for release are the ones about getting out
of the catalog quickly — the tier filter, the links, the environment-dependent URLs — because
those are the whole value to everyone who did not build it.

With one qualification, from §1a: getting out quickly is only worth building if it beats the
wiki, and it only beats the wiki when the exit is *derived* rather than typed. A links column
filled with hand-pasted URLs would answer the surface complaint and leave the catalog doing, more
laboriously, what a wiki page already does. The same feature built on per-type templates
parameterised by placement is something a wiki cannot be. **Same screen, same column, opposite
verdict on whether the product is worth shipping**. The graph decision in §4 carries the same
weight for the same reason, and starts from further back.
