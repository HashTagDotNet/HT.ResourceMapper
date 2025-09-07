# Resource Management Application - Home Page Grid Design

## Overview
This document captures the finalized design decisions for the home page resource grid interface. This is a desktop-only application with no mobile responsive requirements.

## Grid Column Specifications

### **Column Layout**

| Column | Width | Resizable | Sortable | Filterable | Default Sort | Click Action |
|--------|-------|-----------|----------|------------|--------------|--------------|
| **Resource Name** | 40% | ✅ | ✅ | ✅ | A→Z ⭐ | → Detail page |
| **Type** | 25% | ✅ | ✅ | ✅ | None | None |
| **Tags** | 25% | ✅ | ❌ | ❌ | N/A | → View all tags |
| **Updated** | 10% | ❌ Fixed | ✅ | ✅ | None | None |

### **Column Details**

#### **Resource Name Column (40%)**
- **Content**: Full ResourceName from database (not ResourceKey)
- **Wrapping**: Text wraps if needed (multi-line cells)
- **Sort**: Alphabetical A→Z (default sort for entire grid)
- **Filter**: Text search within resource names
- **Click**: Navigate to resource detail page (URL TBD)
- **Resizable**: Yes

#### **Type Column (25%)**
- **Content**: ResourceType name only (no icons)
- **Sort**: Alphabetical by type name
- **Filter**: Dropdown list of available types
- **Click**: None
- **Resizable**: Yes

#### **Tags Column (25%)**
- **Content**: First 5 tags in key:value format
- **Format**: `environment: prod, portal: [Azure Portal], critical (... 3 more)`
- **Link Tags**: If TagContentType = "Link", display as clickable link
- **Text Tags**: If TagContentType = "Text", display as plain text
- **Overflow**: Show "(... X more)" when > 5 tags
- **Sort**: Not sortable
- **Filter**: Not filterable (filtering handled separately)
- **Click**: View all tags for resource (modal/popup)
- **Resizable**: Yes

#### **Updated Column (10%)**
- **Content**: Server-formatted display text (e.g., "2 hours ago")
- **Sort**: By LastUpdated DateTime field (not display text)
- **Filter**: Date range picker
- **Click**: None
- **Resizable**: No (fixed width)

## Default View Behavior

### **Initial State**
- **Empty grid** with helpful onboarding content
- **No authentication** or user tracking required
- **Default sort**: Resource Name A→Z

### **Empty State Content**
```
┌─────────────────────────────────────────────────────────────────┐
│                  📁 No Resources Found                          │
│                                                                 │
│          Start building your resource catalog                   │
│                                                                 │
│              [+ Add Your First Resource]                        │
│                                                                 │
│   Or try searching for existing resources in the search box     │
└─────────────────────────────────────────────────────────────────┘
```

## Visual Grid Layout
```
┌────────────────────────────────────────────────────────────────────────────────┐
│ Resource Name ↓        │ Type     │ Tags                  │ Updated            │
├────────────────────────────────────────────────────────────────────────────────┤
│ MyApp Production       │ Database │ environment: prod,    │ 2 hours ago        │
│ Database Server        │          │ portal: [Azure],      │                    │
│                        │          │ critical (... 3 more) │                    │
├────────────────────────────────────────────────────────────────────────────────┤
│ API Gateway            │ Gateway  │ environment: prod,    │ 1 day ago          │
│                        │          │ public                │                    │
└────────────────────────────────────────────────────────────────────────────────┘
```

## API Response Structure

### **Resource Grid Endpoint**
```
GET /api/resources?sortBy=ResourceName&sortOrder=asc&page=1&pageSize=25
```

### **Response Format**
```json
{
  "data": [
    {
      "resourceUid": "123e4567-e89b-12d3-a456-426614174000",
      "resourceName": "MyApp Production Database Server",
      "resourceType": "Database", 
      "updatedDisplayText": "2 hours ago",
      "lastUpdated": "2024-12-19T13:30:00Z",
      "tags": [
        {
          "key": "environment", 
          "value": "prod", 
          "contentType": "Text"
        },
        {
          "key": "portal", 
          "value": "https://portal.azure.com/resource/...",
          "displayText": "Azure Portal",
          "contentType": "Link"
        }
      ]
    }
  ],
  "links": [
    {"rel": "next", "href": "/api/resources?page=2"},
    {"rel": "prev", "href": "/api/resources?page=1"}
  ],
  "meta": {
    "responseId": "abc12345",
    "timestamp": "2024-12-19T15:45:32.123Z",
    "tags": {
      "totalCount": "157",
      "pageSize": "25", 
      "currentPage": "1"
    }
  }
}
```

## Sort Parameters
```
GET /api/resources?sortBy=ResourceName&sortOrder=asc    # Default (A→Z)
GET /api/resources?sortBy=ResourceName&sortOrder=desc   # Reverse (Z→A)
GET /api/resources?sortBy=LastUpdated&sortOrder=desc    # Most recent first
GET /api/resources?sortBy=LastUpdated&sortOrder=asc     # Oldest first
GET /api/resources?sortBy=ResourceType&sortOrder=asc    # Type alphabetical
```

## Tag Display Rules

### **Tag Content Type Handling**
- **Text Tags**: Display as `key: value`
- **Link Tags**: Display as `key: [display text]` where display text is clickable
- **Tag Limit**: Show first 5 tags, then "(... X more)" for overflow
- **Tag Order**: Server-determined (could be by importance, alphabetical, etc.)

### **Link Tag Behavior**
- **Display**: Use `displayText` if provided, otherwise show domain from URL
- **Click**: Open in new tab/window (standard web behavior)
- **Validation**: URLs validated server-side when creating resources

## Grid Interaction Behavior

### **Column Sorting**
- **Single Column Sort**: Click column header to sort
- **Sort Direction**: First click ascending, second click descending
- **Sort Indicator**: Visual arrow (↑↓) in column header
- **Default**: Resource Name A→Z on page load

### **Column Resizing**
- **Resizable Columns**: Resource Name, Type, Tags
- **Fixed Column**: Updated (always 10% width)
- **Resize Handle**: Drag column borders to adjust width
- **Persistence**: Column widths reset on page refresh (no persistence initially)

### **Row Interactions**
- **Resource Name Click**: Navigate to resource detail page
- **Tags Cell Click**: Show modal/popup with all resource tags
- **Link Tag Click**: Open URL in new tab (within tags cell)

## Technical Notes

### **Database Mapping**
- **Resource Name**: Maps to `Resource.ResourceName` field
- **Type**: Maps to `ResourceType.ResourceTypeName` 
- **Updated**: Maps to `Resource.LastUpdated` DateTime, formatted server-side
- **Tags**: Joined from `ResourceTag`, `TagDefinition`, `TagContentType` tables

### **Performance Considerations**
- **Pagination**: 25 resources per page default
- **Tag Loading**: Include tag data in main resource query (avoid N+1)
- **Sorting**: Perform server-side sorting by database fields
- **Indexing**: Ensure indexes on ResourceName, ResourceType, LastUpdated for sorting

## Future Enhancements (Not in Scope)
- Column width persistence per user
- Multi-column sorting
- Column visibility toggles
- Export grid data functionality
- Bulk selection and operations
- Real-time updates/WebSocket integration

---

**Document Status**: ✅ Finalized  
**Last Updated**: 2025-01-02  
**Next Phase**: Search behavior and filtering design