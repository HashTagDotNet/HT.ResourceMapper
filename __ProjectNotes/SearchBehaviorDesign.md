# Resource Management Application - Search Behavior Design

## Overview
This document defines the search and filtering behavior for the home page resource grid. The search system supports comprehensive global search combined with FluentGrid's built-in column filtering capabilities.

## Search Interface Layout

### **Visual Layout**
```
┌─────────────────────────────────────────────────────────────────────────┐
│ [🔍] Search resources, types, tags...                      [×] [+ Add]   │
├─────────────────────────────────────────────────────────────────────────┤
│ ┌─ FluentGrid with Built-in Column Filters ────────────────────────────┐ │
│ │ Resource Name ↓ [🔍] │ Type [▼] │ Tags │ Updated [📅] │              │ │
│ │ MyApp Prod DB        │ Database │ ...  │ 2 hours ago  │              │ │
│ │ API Gateway          │ Gateway  │ ...  │ 1 day ago    │              │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### **Auto-complete Search History**
```
┌────────────────────────────────────────────────────────────┐
│ [🔍] database▌                                      [×]    │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🕒 database production     (recent search - 12 results) │ │
│ │ 🕒 database staging        (recent search - 8 results)  │ │  
│ │ 📁 Database               (resource type - 45 total)    │ │
│ │ 🔍 MyApp Database Server   (resource name match)        │ │
│ └─────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
```

## Global Search Behavior

### **Search Scope - Comprehensive**
The global search box searches across **all** relevant resource fields:

| Field | Search Behavior | Example |
|-------|-----------------|---------|
| **Resource Name** | Contains match (case-insensitive) | "prod" matches "MyApp **Prod**uction DB" |
| **Resource Description** | Full-text search | "primary database" matches description text |
| **Resource Type** | Contains match | "data" matches "**Data**base" type |
| **Tag Keys** | Exact or contains match | "environment" matches tag key |
| **Tag Values** | Contains match (case-insensitive) | "prod" matches tag value "**prod**uction" |

### **Search Triggers - Hybrid Approach**
```javascript
// Debounced typing (400ms delay)
onSearchInput(debounce: 400ms) → triggerSearch()

// Immediate UI interactions  
onEnterKey() → triggerSearch()
onSearchButton() → triggerSearch()
onClearButton() → clearSearch()
onEscapeKey() → clearSearch()
```

### **Search Box Features**
- **Placeholder**: "Search resources, types, tags..."
- **Clear Button**: X appears when search has content
- **Search Icon**: 🔍 indicates search functionality
- **Loading State**: Visual indicator during search execution

### **Clear Search Behavior**
- **Clear Triggers**: Click X button, press Escape key
- **Clear Action**: Empties search box and refreshes grid to show all resources
- **Combined Filters**: Clearing search preserves column filters
- **Visual State**: X button disappears when search is empty

## FluentGrid Column Filtering

### **Built-in Column Filters**
FluentGrid provides standard filtering for each applicable column:

| Column | Filter Type | Behavior |
|--------|-------------|----------|
| **Resource Name** | Text Input | Contains/starts with filtering |
| **Type** | Dropdown Multi-select | Select multiple resource types |
| **Tags** | None | Handled by global search only |
| **Updated** | Date Range Picker | Filter by date range |

### **Filter Interaction**
- **Independent Operation**: Column filters work independently of global search
- **Combined Results**: Global search + column filters = AND operation
- **Filter Persistence**: Column filters remain when global search is cleared
- **Visual Indicators**: FluentGrid shows active filter states

## Virtual Data Source Integration

### **FluentGrid Virtual Data Source**
- **Performance**: Only loads visible data + buffer
- **Lazy Loading**: Data fetched as user scrolls/filters
- **Real-time**: Search and filter results update grid immediately
- **Pagination**: Handled automatically by virtual scrolling

### **Data Loading Behavior**
```
User types → Debounce → API call → Virtual grid updates → Smooth UX
User filters → Immediate → API call → Virtual grid updates → Responsive
```

## API Requirements

### **Search and Filter Endpoint**
```
GET /api/resources/grid?
    search={globalSearchTerm}&           // Global search across all fields
    nameFilter={nameFilter}&             // Resource name column filter
    typeFilter={type1,type2}&            // Resource type column filter  
    updatedFrom={date}&updatedTo={date}& // Updated date column filter
    sortBy={column}&sortOrder={asc|desc}&// Sorting parameters
    skip={offset}&take={pageSize}        // Virtual scrolling pagination
```

### **Example API Calls**

#### **Global Search Only**
```
GET /api/resources/grid?search=production&skip=0&take=50
```
Searches for "production" in resource name, description, type, and all tag keys/values.

#### **Global Search + Column Filters**
```
GET /api/resources/grid?search=database&typeFilter=Database&nameFilter=prod&skip=0&take=50
```
Global search for "database" AND resource type is "Database" AND resource name contains "prod".

#### **Column Filters Only**
```
GET /api/resources/grid?typeFilter=Database,Gateway&updatedFrom=2024-12-01&skip=0&take=50
```
No global search, just filter by specific types and recent updates.

### **Response Structure**
```json
{
  "data": [
    {
      "resourceUid": "123e4567-...",
      "resourceName": "MyApp Production Database",
      "resourceType": "Database",
      "resourceDescription": "Primary database for MyApp production environment",
      "updatedDisplayText": "2 hours ago",
      "lastUpdated": "2024-12-19T13:30:00Z",
      "tags": [
        {"key": "environment", "value": "production", "contentType": "Text"},
        {"key": "portal", "value": "https://portal.azure.com/...", "displayText": "Azure Portal", "contentType": "Link"}
      ]
    }
  ],
  "totalCount": 157,
  "meta": {
    "responseId": "abc12345",
    "timestamp": "2024-12-19T15:45:32.123Z",
    "tags": {
      "searchTerm": "production",
      "appliedFilters": "type:Database,Gateway",
      "executionTimeMs": "45"
    }
  }
}
```

## Search Performance Considerations

### **Database Query Optimization**
- **Full-Text Indexing**: On ResourceName, ResourceDescription fields
- **Tag Search Optimization**: Efficient joins across ResourceTag, TagDefinition tables
- **Query Execution**: Complex searches should execute in < 200ms
- **Result Limiting**: Virtual grid requests only needed data (typically 50-100 rows)

### **Client-Side Performance**
- **Debouncing**: Prevents excessive API calls during typing
- **Request Cancellation**: Cancel previous search when new search initiated
- **Loading States**: Visual feedback during search execution
- **Error Handling**: Graceful degradation for failed searches

## User Experience Flow

### **Typical Search Workflow**
1. **User lands on page**: Empty grid with onboarding content
2. **User types search**: Debounced search triggers, loading indicator shows
3. **Results appear**: Grid populates with matching resources
4. **User refines**: Applies column filters for more specific results
5. **User clears**: X button or Escape key returns to full resource list

### **Error States**
- **No Results**: "No resources found matching your search" with suggestions
- **Search Error**: "Search temporarily unavailable" with retry option
- **Network Error**: Offline indicator with cached results if available

### **Search Result Context**
- **Result Count**: Show "Showing X of Y resources" with search context
- **Search Highlight**: Consider highlighting search terms in results (future enhancement)
- **Search History**: Browser history supports back/forward through searches

## Search History & State Persistence

### **Search History Storage**
```javascript
// localStorage structure
SearchHistory = {
  searches: [
    {
      term: "database production",
      timestamp: "2024-12-19T15:30:00Z",
      resultCount: 12,
      filters: { typeFilter: ["Database"] } // Optional: associated filters
    }
  ],
  maxItems: 10
}
```

### **Auto-complete Behavior**
- **Trigger**: Shows dropdown when user types (2+ characters)
- **Mixed Results**: Recent searches (🕒) + live suggestions (📁🔍)
- **Selection**: 
  - **Recent searches**: Immediate search execution with stored filters
  - **Live suggestions**: Populate search box and execute
- **Keyboard Navigation**: Arrow keys + Enter support
- **Visual Distinction**: Icons differentiate search types

### **Grid State Persistence**
```javascript
// Complete grid state stored in localStorage
GridState = {
  // Search state
  searchTerm: "database production",
  
  // Column filters (FluentGrid)
  nameFilter: "prod",
  typeFilter: ["Database", "Gateway"], 
  updatedFrom: "2024-12-01",
  updatedTo: "2024-12-31",
  
  // Sort state
  sortBy: "LastUpdated",
  sortOrder: "desc",
  
  // Grid layout
  columnWidths: {
    resourceName: "45%",
    type: "20%", 
    tags: "25%",
    updated: "10%" // fixed, but tracked for consistency
  },
  
  // Pagination/position
  scrollPosition: 0,
  selectedRowId: null
}
```

### **State Loading Priority (on app startup)**
1. **URL parameters** (highest priority)
2. **Saved localStorage state**  
3. **Application defaults** (lowest priority)

**Example URL override:**
```
/resources?search=production&type=Database
→ Overrides saved search state, preserves other state (column widths, etc.)
```

### **Storage Implementation**
- **localStorage**: Persistent across browser sessions
- **Automatic cleanup**: Trim search history to 10 most recent
- **Debounced writes**: Save grid state changes after 500ms delay
- **Error handling**: Graceful fallback if localStorage unavailable

## Integration Points

### **Related User Stories**
- **US-001**: Basic Resource Search - Fully supported by global search
- **US-002**: Tag-Based Resource Filtering - Supported by global search + future tag-specific filters
- **Future US-003**: Advanced Boolean Search - Can extend current API with query parser

### **Database Schema Integration**
- **Resource Table**: ResourceName, ResourceDescription, ResourceType searchable
- **Tag System**: ResourceTag → TagDefinition joins for tag key/value search
- **Content Types**: TagContentType used for link vs text rendering

### **FluentGrid Configuration**
- **Virtual Scrolling**: Enabled for performance with large datasets
- **Column Filters**: Enabled on Name, Type, Updated columns
- **Sorting**: Enabled on all applicable columns
- **Resizing**: Enabled per HomePageGridDesign.md specifications
- **State Persistence**: Grid state automatically saved and restored

---

**Document Status**: ✅ Enhanced with History & Persistence  
**Last Updated**: 2025-01-02  
**Integration**: Complements HomePageGridDesign.md  
**Next Phase**: Resource detail page design or API implementation