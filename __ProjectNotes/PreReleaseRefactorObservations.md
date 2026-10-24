# Pre-Release Refactoring — Observations

Raw capture of Steve's observations ahead of the pre-release refactor.

**Status: collecting** — closed at fifteen on 2026-09-19 ("I think that is what I have"), reopened for a sixteenth.
Entries are recorded as stated. They may be incomplete, vague, or contradict each other — that is
expected. Nothing here is triaged or turned into a plan.

## How this file is used

- One entry per observation, in the order given, each with a short name used for cross-reference.
- Each entry keeps Steve's framing. Where an entry is ambiguous, the ambiguity is preserved
  rather than resolved.
- `Notes:` lines are mine (Claude) — clarifying context, a pointer to code, or a flagged
  conflict with another entry. They are never a decision.
- The reading of these observations as business analysis lives in
  `PreReleaseRefactorAnalysis.md`. This file stays raw.

## Observations

### No tier filter

There isn't a tier filter, so I cannot easily choose tier (e.g. named subscription) in this data.

### A more defined taxonomy

The tags-with-links model works, but I think I would like a different model. Tags will exist, but
a slightly more defined taxonomy is really necessary.

I think there's some more we need — different ways to sort, slice and group data.

Marked by Steve as incomplete: more thoughts coming.

Notes: possibly the same ground as *No tier filter* — that one names a missing way to slice; this
one questions the model the slicing would run on. Left as two entries; not merged.

### Systems / product lines

In our real life, we have systems — LITE, vNext, Telematics, Capacity. One might think of these
as "product lines".

Notes: "product lines" is offered tentatively ("one might think"), so the naming is not settled,
and whether this is a dimension, a taxonomy level, or something else is not stated.

### "Sub-system" is the wrong word

Systems contain "sub-systems", but I don't like the word sub-systems. In vNext a sub-system might
be UI, AI, Alerting, FraudDetection.

Notes: the concept is asserted; the name is explicitly rejected without a replacement given.
Extends *Systems / product lines* by one level down.

### Systems and sub-systems are virtual

Systems and sub-systems are generally virtual collections that don't map to a resource.

Notes: "generally" is Steve's — not stated as absolute. This sits against what the current model
does: the Tier seed describes System and Application as logical resources that carry the 'shared'
value and are deployed to neither tier, i.e. today they exist *as* resources.

### An application is 1..N resources

An application may be 1..N actual resources (web service, queue, app service).

Notes: introduces "application" as a level below sub-system, separate from the concrete resources
it comprises.

### Application is virtual; four-level hierarchy

Application, I'm thinking, is a virtual collection. For instance:

    s  = vNext
    ss = Alerting
    a  = Trigger Ingestor
    r  = usspi-my-cache-resource

...is the logical hierarchy I am thinking.

Notes: puts application on the virtual side with system and sub-system; only the leaf is an actual
resource. Still framed as thinking, not a decision. The second level remains
rejected-but-unreplaced per *"Sub-system" is the wrong word*.

### Environments and subscriptions

Systems function on multiple environments (localhost, develop, preprod, preview, production).

An Azure subscription defines the space where an environment lives. Sub1 contains all Azure
resources for all systems/apps etc. A system doesn't generally (not yet) reach across
subscriptions.

Notes: "generally (not yet)" is Steve's own double hedge — today's shape, not a rule. Two things
to hold: localhost appears in the environment list but has no Azure subscription; and this
environment axis is finer-grained than the Tier vocabulary (prod/non-prod/shared) behind
*No tier filter*. Whether environment replaces tier, sits under it, or is a separate dimension is
not stated.

### localhost consumes develop's shared resources

localhost uses shared Azure (e.g. configuration, logging, cache) that develop systems use.

Notes: speaks to the localhost gap in *Environments and subscriptions* — localhost isn't a
subscription of its own, it consumes resources living in develop's space. That makes a resource's
environment and the environment of the thing *using* it two different facts, which the four-level
hierarchy does not carry.

### Shared resources carry a selector

Shared resources often have a selector, or combined selector, for application and/or environment:

    AzureAppConfig:       Label = develop
    AzureAppInsights:     "alerting ti"  or  "AlertingTi(dev)"
    cache key:            my-app-{env}
    database application: ti(dev)

Notes: the mechanism behind *localhost consumes develop's shared resources* — one physical
resource partitioned by a selector, so it serves several applications and/or environments. The
selector's form differs per resource type (label, name convention, key template, app name), and
some encode application and environment together in one string. Whether the selector is data to be
stored, parsed, or just documented is not stated.

### Applications depend on applications

An application might depend on other applications — e.g. vNext Web Server depends on vNext Proxy
on LITE — and both share the same database server and database.

Notes: two claims in one sentence. The dependency crosses a system boundary (an application named
"vNext Proxy" living in the LITE system), so the four-level tree doesn't hold a resource's whole
story. And "both share the same database server and database" is sharing of a concrete resource
between applications — a different relationship from selector partitioning: here there is no
selector, it's the same database.

### Hierarchy, graph and tags coexist

Agreed — hierarchy, graph, and random tags etc.

Notes: answers the tree-vs-graph question directly: not a choice between them. Containment
hierarchy, cross-cutting relationships and unconstrained tags are three coexisting mechanisms.
"etc." is Steve's — the list is not stated as closed. Does not say which mechanism owns which fact
when more than one could carry it.

### Links are cumbersome and environment-dependent

Let's look at links. The links displaying in the tags list are nice but somewhat cumbersome. I am
thinking a links column in the grid.

An application might have links to the app service (or Service Fabric), to Azure App Insights with
custom queries, metrics, configuration with filter, DNS endpoint, Azure hosted URL, etc.

The links might change based on environment: dev-macropoint.com, localhost:333,
preprod-macropoint.com, etc.

Notes: three parts. (a) A problem named — links in the tags list are cumbersome — with "a links
column in the grid" offered as thinking, not settled. (b) Links are many per application and of
varying kinds, several parameterised (a query, a filter). (c) A link's value is a function of
environment, the same {env} templating shape as *Shared resources carry a selector*. Softens
*A more defined taxonomy*'s "tags with links works".

### Grid rows contain all four levels

Grid rows contain systems, sub-systems, applications, and native resources.

Notes: answers the question left open by *Links are cumbersome and environment-dependent* — the
grid is not a list of native resources, it lists every level, so an application row carries its own
links directly. Worth holding against *Systems and sub-systems are virtual*: "virtual collection
that doesn't map to a resource" and "appears as a grid row" are not the same claim. What
distinguishes the levels in the grid is not stated.

### Import tags from Azure

I might also import tags from Azure resources themselves.

Notes: hedged ("might"). Introduces a second source of truth for tags — authored here vs. pulled
from Azure. Says nothing about direction (read-only import, sync, round-trip) or what happens when
the two disagree.


### Two customers: project names vs Azure names

Two customers: app engineers think projects, IT engineers focus on Azure names.

Notes: corrects an overstatement in my analysis, which claimed nobody thinks in Azure resource
names. Both addressing schemes are primary — for different people. This bears on *Import tags from
Azure*: the IT engineer's source of truth lives in Azure, the app engineer's lives here.


### What the IT engineer actually asks

The IT engineer asks "what is impacted by this resource", "open resource so I can manage it",
"what logs are being created by…" — some questions.

Notes: corrects my guess at this persona's question, which was "what is it for and who owns it".
The real ones are operational, not descriptive. "Some questions" is Steve's — the list is
explicitly not exhaustive. Each one lands on a different mechanism: impact is the graph traversed
*backwards* (who depends on me), manage is the click-through link, logs is the App Insights
selector from *Shared resources carry a selector*.


### The wiki already does the handoff

On the catalog's job being to hand an engineer off to the tool that acts: this is largely what the
wiki provides.

Notes: ambiguous in a way worth preserving. It reads either as "so the catalog must beat the wiki
at this to be worth building" or as "so don't over-invest here, the differentiator is elsewhere".
Not resolved. Either way it puts the question to the whole wayfinding concern: if links are
hand-authored per resource, the catalog is a wiki with a worse editor.


### The dependency graph is as important

The dependency graph is as important.

Notes: responds to my ranking of wayfinding as the thing to settle first — corrects it to equal
standing rather than replacing it. "As important" is a statement about weight, not sequence.

## Collection note

Collection was closed after fifteen entries and reopened; nineteen and counting.

## Open conflicts

**Virtual levels vs. today's model.** *Systems and sub-systems are virtual* and *Application is
virtual* say the upper levels don't map to a resource. Today the Tier seed treats System and
Application as logical *resources* carrying 'shared'. *Grid rows contain all four levels* softens
this — the virtual levels do appear as rows, and a row is not the same thing as a resource. Not
reconciled.

**Naming of the upper levels.** *"Sub-system" is the wrong word* rejects that term with no
replacement; *Systems / product lines* offers "product lines" tentatively for the top level. No
terms settled.

**Tier vs. environment vs. subscription.** *No tier filter* asks for a tier filter and gives
"named subscription" as the example; *Environments and subscriptions* names environments
(localhost…production) and subscriptions as the space an environment lives in; the shipped Tier
vocabulary is prod/non-prod/shared. Three granularities under overlapping words. Not reconciled.

**Where a shared resource lives vs. who uses it.** *localhost consumes develop's shared resources*
makes these two different facts. Not resolved.

**Tree vs. graph.** RESOLVED by *Hierarchy, graph and tags coexist* — all three coexist.

**Links hang off applications; the grid lists resources.** RESOLVED by *Grid rows contain all four
levels* — the grid lists all levels, so an application row carries its own links.
