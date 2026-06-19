# HT Resource Mapper – UX & Architecture Plan

> **Status / resume:** see [`Phase1-Status.md`](./Phase1-Status.md) — Phase 1 is functionally complete and
> live-verified (import pipeline + new SSR UI + wiki "Visibility Resources" imported). Run via
> [`UI/ResourceMapper.UI.Web/README.md`](../../UI/ResourceMapper.UI.Web/README.md).

# File Locations

**Research Files:** docs/research-files
* Miscellaneous documents that can inform our planning, design, or implmentation but should NOT be considered authoritative.

**Research Images:** docs/research-files/images
* Miscellaneous image and image fragements that can inform our planning, design, and implementation but should NOT be considered authoritative.

**Active planning documents:** docs/plans
* Currently active plans
* There may be one or more plan files depending on scope

**Active planning artifacts:** docs/plans/artifacts
* Images, and other related documents that support the current plan(s)
* AI can place pasted images, files, snippets here

**Archive:** docs/archive
* Planning and other documents that are no longer active from obsolete sessions but might be useful for current planning and implementation

# Project Summary
## Objectives
This project has these primary objectives
1. Demonstrate best practices using AI to generate a non-trivial applicaiton.
1. Replace the static wiki with more comprehensive capabilities

## AI Assistance
1. I expect the AI planning agent(s) to guide me first in agentic coding best practices such as loops, rubber-ducks, planning, sub-agents and use those where indicated; but only if it makes sense.  DO NOT use agentic concepts for illustrative purposes but only if it moves the project forward in least time and least cost.
1. The AI should calculate realistive wall-clock effort. Ideally I would like to complete this project in a week-end (20-30 hours of human time). This is an important design consideration.
1. The AI should guide and shape my expectations
1. The AI must not make assumptions such a declare a task done until it can veriy the task is done. (This has been a problem in my other AI related projects)
1. The AI must help me create a strong plan from my loose ideas.  It must seek to understand my concepts before considering implementation.
1. The AI must take these high level ideas and work with me to create master plan with sub tasks (design, discuss compoent 1, ask about security, etc.) even before reading code.  It must not run ahead and if it needs access to existing codes/docs then it can ask. "It would be helpful in this stage of design if I could see what your are thinking"
1. The AI must constantly evaluate my prompts and suggest better prompts
1. I expect the AI to be a partner in design, planning, and implmentation. The AI must be more than code-completion tool.  It must bring real intelligence to the process
1. The AI is expected to maintain accurate and up-to-date status, plans, checklist, todo items, such that this project can start/stop without loss.

## Wiki Replacement/High Level Capabilities
### Existing Wiki
1. The existing wiki functions as a robust static bookmarking system. See ResearchImages::wiki-annotated-top.  Users use this section most often as a fast gateway into some common applications
1. This wiki is limited to a single team (we have 4 teams)
1. This wiki is large enough that is has become more difficult to maintain
1. Wiki also maintains meta data related to some resources. This meta needs to be easily viewed and accessed. See ResearchIMages::wiki-annotated-meta-data
1. There are parts of the wiki that aren't related to resource mapping and are out of scope for this project (e.g. logging, tooling)
1. The complete wiki is at: ResearchFiles::azure-wiki.md

## High Level Capabilities and Considerations

**Note:** This is not a comprehensive list.  I expect our design conversations will expand/remove and further refine the capability list before any detailed design is attempted.

**Note:** These ideas are a brain dump are not comprehensive or prescriptive.  The AI is expected to take these into consideration but must not consider these to be complete or accurate.  The AI must first work with developer to flesh out and document the capabilites

1. The app should start extremely fast.  First load can be a little slower but if I use the site on Monday, then next week I use the site, it should snap open.  It should be as available as a wiki page. Maybe <1 or <2 seconds? Actual numbers are for design later.
1. UX is going to strongly inspred by Azure DevOps resource list. See ResourceImages::azure-home-example and ResourceImages::azure-home-annotated.
1. Tags provide a mechanism for associating meta data with a resource.  A resource may have more than one tag-key: enviornment:dev, environment:localhost
1. Tags can have links
1. The AI is responsible for working with developer building a complete capability list.
1. The focus will be on building the landing/grid page and importing so we can import existing resource links/metadata from wiki.
1. I created a course grained suggested mockup for landing page in PlanArtifacts::home-page-course-wireframe
1. Navigation needs consideration.  In the grid I want to be able to easly copy/click a link and go to the Azure resource.  I also want to copy/clik link that will go to the resource details in this application.  So there are two similar distinct navigation paths. 

## Other Notes
### UX Consdierations
1. The application MUST start extremely fast.  It should be equivilant to going to the Wiki.  This is a consideration when deciding between between Razor pages, MVC, or Blazor
1. The UX should be visually light weight and compact.  
1. The UX should make it so easy to access primary use-case data that users will go here instead of custom book marks or this page.
1. The UX should use native components of the chosen technology stack where possible.  AVOID style or behavior customization.  Prefer to change project requirements if technology stack does not support that requirement in some way.
1. The UX design must be hyper usable such as:
   1. Copy link on hover like git hub and other apps
   1. Generally responsive
   1. Visually appealing yet very simple
   1. State persistance
   1. Visual focus on data and not operations (e.g. keep buttons, links, etc. secondary brightness.)

## Development Phases
1. Phase #1a - UI/Home Page with Wiki Data
1. Phase #1b - Filter management/search
1. Phase #2 - Add/edit/delete Resources
1. Phase #3 - Resource relationships (this resource uses that resource and is consumed by another resource)

## Phase #1a Home Page - Suggested Focus Plan/Roadmap
AI - please review and challenge me on this before continuing

1. Review/refine business capabities.  
1. Select technology stack (or reuse stack we have)
1. Build landing page with place holders including filer text box, menu icon, grid, filter slugs, and (+) filter
1. Focus on ETL of wiki resources
   1. Import menu
   1. Define import json.  Seen plans::ImportExportApiDesign.md for prior work.
   1. Extract data from wiki into json file
   1. Import data into database
1. Display of resources in grid
   1. Linking to azure resources
   1. Tags link to resources
1. Filtering - no filter management, maybe save filter to browser storage?

### Technical Notes
1. Deep linking is important, both in summary grid and to the resource itself.
1. Security is going to be provided by a cookie.  Code the logic but don't challenge
1. You can use LocalDb to store state and user prefernces, etc.

### Existing Work
1. The solution MUST use the existing database/backend scope where possible building and extending as necesary.
1. The UI/UX layer should be entirely replaced with new code

---

> **Living document.** Update this file whenever a design decision is made. Sections marked ⚠️ are open for design discussion — do not implement until resolved.

