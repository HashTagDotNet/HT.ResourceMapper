# Resource Management Application - User Stories

## Epic 1: Resource Discovery and Search

### US-001: Basic Resource Search
**As a** DevOps engineer  
**I want to** search for resources by name or description  
**So that** I can quickly locate specific resources across our cloud environments

**Acceptance Criteria:**
- [ ] Search box accepts free text input
- [ ] Results display in grid format with key resource information
- [ ] Search includes resource name, description, and resource key
- [ ] Results are returned in < 2 seconds for typical queries
- [ ] No results state is clearly communicated

**Database Impact:** 
- Uses existing `Resource` table
- May need full-text indexing on Name/Description fields

**API Requirements:**
```
GET /api/resources/search?q={searchTerm}&limit={limit}&offset={offset}
```

---

### US-002: Tag-Based Resource Filtering
**As a** DevOps engineer  
**I want to** filter resources by tags (key, value, or key-value pairs)  
**So that** I can find all resources related to a specific application or environment

**Acceptance Criteria:**
- [ ] Filter by tag key only (e.g., all resources with "environment" tag)
- [ ] Filter by tag value only (e.g., all resources tagged with "production")
- [ ] Filter by key-value pair (e.g., environment="production")
- [ ] Support multiple tag filters with AND/OR logic
- [ ] Tag suggestions appear as user types

**Database Impact:**
- Uses `ResourceTag`, `TagDefinition` tables
- Complex joins required for multi-tag searches
- Consider materialized views for performance

**API Requirements:**
```
GET /api/resources?tags[environment]=production&tags[application]=MyApp
```

---

### US-003: Advanced Boolean Search
**As a** System architect  
**I want to** perform complex searches using boolean logic  
**So that** I can find resources matching sophisticated criteria

**Acceptance Criteria:**
- [ ] Support AND, OR, NOT operators
- [ ] Group conditions with parentheses
- [ ] Combine text search with tag filters
- [ ] Save complex search queries for reuse
- [ ] Export search results

**Example Query:** `(environment="production" OR environment="staging") AND application="MyApp" NOT deprecated="true"`

---

## Epic 2: Resource Management

### US-004: Create New Resource
**As a** Developer  
**I want to** add a new resource to the catalog  
**So that** my team can discover and understand the resource I've deployed

**Acceptance Criteria:**
- [ ] Select resource type from predefined list
- [ ] Enter required fields (ResourceKey, Name, Description)
- [ ] System generates unique ResourceUid automatically
- [ ] Apply required tags based on resource type
- [ ] Add optional custom tags
- [ ] Validate ResourceKey uniqueness
- [ ] Success confirmation with link to view resource

**Database Impact:**
- Uses existing `Resource_Create` stored procedure
- Need validation for ResourceKey uniqueness
- Auto-generation of ResourceUid

**API Requirements:**
```
POST /api/resources
{
  "resourceKey": "myapp-prod-db",
  "resourceTypeCode": "database",
  "resourceName": "MyApp Production Database",
  "description": "Primary database for MyApp production environment",
  "tags": [
    {"key": "environment", "value": "production"},
    {"key": "application", "value": "MyApp"}
  ]
}
```

---

### US-005: Bulk Tag Application
**As a** DevOps engineer  
**I want to** apply tags to multiple resources simultaneously  
**So that** I can efficiently maintain consistent tagging across related resources

**Acceptance Criteria:**
- [ ] Select multiple resources from search results
- [ ] Choose tags to add or remove
- [ ] Preview changes before applying
- [ ] Bulk operation progress indicator
- [ ] Success/error summary after completion
- [ ] Audit trail of bulk changes

---

## Epic 3: Dependency Management

### US-006: View Resource Dependencies
**As a** System architect  
**I want to** see what resources a specific resource depends on and what depends on it  
**So that** I can understand the impact of changes

**Acceptance Criteria:**
- [ ] Display "Depends On" list (upstream dependencies)
- [ ] Display "Depended By" list (downstream dependencies)
- [ ] Show dependency relationship creation date
- [ ] Link to dependent resource details
- [ ] Indicate circular dependencies if present

**Database Impact:**
- Uses `ResourceDependency` table
- Need efficient queries for bidirectional relationships
- Consider recursive CTEs for dependency chains

---

### US-007: Add Resource Dependencies
**As a** Developer  
**I want to** define dependencies between resources  
**So that** others understand the relationships between system components

**Acceptance Criteria:**
- [ ] Search and select dependent resources
- [ ] Prevent self-dependencies
- [ ] Warn about potential circular dependencies
- [ ] Bulk dependency creation
- [ ] Audit trail of dependency changes

---

### US-008: Dependency Impact Analysis
**As a** DevOps engineer  
**I want to** see the full chain of dependencies for a resource  
**So that** I can assess the impact of changes or outages

**Acceptance Criteria:**
- [ ] Visualize dependency graph (2-3 levels deep)
- [ ] Export dependency report
- [ ] Filter by dependency type if implemented
- [ ] Show impact scope (number of affected resources)
- [ ] Highlight critical paths

---

## Epic 4: Resource Types and Metadata

### US-009: Resource Type Management
**As a** System administrator  
**I want to** manage resource types and their required tags  
**So that** we maintain consistent resource categorization

**Acceptance Criteria:**
- [ ] Create new resource types
- [ ] Define required tags per resource type
- [ ] Set allowed tag values where applicable
- [ ] Enable/disable custom tags per resource type
- [ ] Archive obsolete resource types

**Database Impact:**
- Uses `ResourceType`, `ResourceTypeTag` tables
- Need admin interface for type management

---

### US-010: Link-Type Tag Management
**As a** Developer  
**I want to** add links as tag values (URLs to monitoring, documentation, etc.)  
**So that** resources serve as a comprehensive directory with quick access to related tools

**Acceptance Criteria:**
- [ ] Tag content type validation (Link vs Text)
- [ ] URL format validation for link-type tags
- [ ] Click-through functionality in UI
- [ ] Support for multiple links per tag definition
- [ ] Link health checking (optional)

**Database Impact:**
- Uses `TagContentType` table
- Leverage existing "Link" content type

---

## Technical Stories (Infrastructure)

### TS-001: Search Performance Optimization
**As a** System  
**I want to** provide fast search results even with large resource catalogs  
**So that** user experience remains responsive

**Technical Requirements:**
- [ ] Full-text indexing on searchable fields
- [ ] Materialized views for complex tag queries
- [ ] Query execution plans under 100ms for typical searches
- [ ] Caching strategy for frequent searches
- [ ] Database connection pooling

---

### TS-002: API Response Standards
**As a** API consumer  
**I want to** consistent response formats across all endpoints  
**So that** integration is predictable and reliable

**Technical Requirements:**
- [ ] Leverage existing `ApiResponse`, `MetaData`, `Link` patterns
- [ ] Standardized error codes and messages
- [ ] HATEOAS links for related resources
- [ ] Pagination for list responses
- [ ] Response time tracking in metadata

---

## Epic 5: Navigation Preferences

### US-011: Configurable Resource-Name Click Behavior
**As a** user of the resource catalog
**I want to** choose what happens when I click a resource's name in the grid
**So that** the click takes me straight to the tool I use most — either the resource's primary external reference or the ResourceMapper details page

**Acceptance Criteria:**
- [ ] A **User Preferences** page exposes a setting: *"When I click a resource name, open…"* with two choices — **Primary external link** (default) or **ResourceMapper details page**
- [ ] The preference is persisted client-side in `localStorage` (same mechanism as the home-page "resume last view" state — see `ViewStorageKey` in `Home.razor`), under a distinct key
- [ ] **Default behavior** (no preference set, or set to "Primary external link"): clicking the resource name opens that resource's *primary* external reference link
- [ ] When the preference is "ResourceMapper details page": clicking the name opens the details page instead
- [ ] **Missing primary link fallback:** if a resource has no primary reference link defined, the name is not a dead link. Show a tooltip (or equivalent affordance) offering **"Open details to define primary resource link"**, which navigates to the details page where the user can set one
- [ ] Behavior is consistent across the home grid and any other place resource names are rendered as clickable

**Prerequisite / Database Impact:**
- Requires a **"primary reference link"** concept that does not yet exist. Today the grid renders *all* `Link`-content-type tags with no primary designation (`Home.razor`, external-links menu). Options to evaluate:
  - Add an `IsPrimary` flag on the link-type resource tag (e.g. on `ResourceTag`), or
  - A dedicated primary-reference field on `Resource`
- The details page (US-010 link-type tag management) must let the user designate which link is primary

**Notes:**
- Preference is per-browser (localStorage), not per-user-in-DB, matching the current last-used-URL/resume storage approach. Revisit if cross-device sync is needed later.
- Relates to US-010 (link-type tag management) and the home-page row quick-actions already shipped (open/copy external + details).

---

## Story Template for Future Use

### US-XXX: [Story Title]
**As a** [persona]  
**I want to** [capability]  
**So that** [business value]

**Acceptance Criteria:**
- [ ] [Specific, testable criteria]

**Database Impact:**
- [Tables/procedures affected]

**API Requirements:**
```
[Sample API calls]
```

**Notes:**
- [Additional context, dependencies, or considerations]

---

## Story Status Tracking

| Story ID | Status | Priority | Sprint | Assignee | Notes |
|----------|--------|----------|---------|----------|--------|
| US-001 | Planned | High | 1 | - | Foundation for all search features |
| US-002 | Planned | High | 1 | - | Core tag functionality |
| US-004 | Planned | High | 1 | - | Basic CRUD operations |
| US-006 | Planned | Medium | 2 | - | Dependency visualization |
| US-007 | Planned | Medium | 2 | - | Dependency management |
| US-003 | Planned | Low | 3 | - | Advanced search features |
| US-005 | Planned | Low | 3 | - | Bulk operations |
| US-008 | Planned | Low | 3 | - | Advanced dependency analysis |
| US-011 | Planned | Low | - | - | Name-click behavior pref; needs "primary link" concept first |
