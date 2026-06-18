# ApiResponse Source Generation Migration Guide

## Overview

The `ApiResponse` class has been refactored to support **System.Text.Json source generation** for improved performance in Blazor WebAssembly applications and AOT compilation scenarios.

## Changes Made

### ? **What Changed**
- **Computed properties moved to extension methods** for source generation compatibility
- **Added ApiResponseExtensions class** with all the computed logic
- **Created JSON serialization contexts** for source generation
- **Maintained all existing functionality** through extension methods

### ?? **Migration Required**

#### **Before (Properties)**
```csharp
var response = new ApiResponse();
response.AddValidationError("Email", "Invalid format");

// Old property access
if (response.IsSuccess) { /* handle success */ }
if (response.HasFailures) { /* handle failures */ }
var errorCode = response.HighestPriorityError?.StatusCode;
var httpStatus = response.PrimaryHttpStatusCode;
var category = response.ResultCategory;
```

#### **After (Extension Methods)**
```csharp
using HT.Api.Client.Contracts.Extensions; // Add this using

var response = new ApiResponse();
response.AddValidationError("Email", "Invalid format");

// New extension method access
if (response.IsSuccess()) { /* handle success */ }
if (response.HasFailures()) { /* handle failures */ }
var errorCode = response.GetHighestPriorityError()?.StatusCode;
var httpStatus = response.GetPrimaryHttpStatusCode();
var category = response.GetResultCategory();
```

## Migration Steps

### 1. **Add Using Directive**
Add the extension namespace to files using ApiResponse:

```csharp
using HT.Api.Client.Contracts.Extensions;
```

### 2. **Update Property Calls to Method Calls**

| Old Property | New Extension Method |
|--------------|---------------------|
| `response.IsSuccess` | `response.IsSuccess()` |
| `response.HasFailures` | `response.HasFailures()` |
| `response.HasServerErrors` | `response.HasServerErrors()` |
| `response.HasClientErrors` | `response.HasClientErrors()` |
| `response.HasCancellations` | `response.HasCancellations()` |
| `response.HighestPriorityError` | `response.GetHighestPriorityError()` |
| `response.PrimaryHttpStatusCode` | `response.GetPrimaryHttpStatusCode()` |
| `response.ResultCategory` | `response.GetResultCategory()` |

### 3. **Update Blazor Components**

#### **Before**
```razor
@if (response.IsSuccess)
{
    <div class="@response.HighestPriorityError?.AlertClass">
        Success!
    </div>
}
else if (response.HasClientErrors)
{
    <div class="alert-danger">
        Client Error: @response.GetErrorSummary()
    </div>
}
```

#### **After**
```razor
@using HT.Api.Client.Contracts.Extensions

@if (response.IsSuccess())
{
    <div class="@response.GetHighestPriorityError()?.AlertClass">
        Success!
    </div>
}
else if (response.HasClientErrors())
{
    <div class="alert-danger">
        Client Error: @response.GetErrorSummary()
    </div>
}
```

### 4. **Enable Source Generation**

#### **Configure JSON Serialization**
```csharp
// In Program.cs for Blazor WebAssembly
using HT.Api.Client.Contracts.Serialization;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiContractsJsonContext.Default);
});

// For HttpClient JSON calls
var response = await httpClient.GetFromJsonAsync<ApiResponse>("api/endpoint", ApiContractsJsonContext.Default.ApiResponse);
```

#### **Blazor Optimized Context**
```csharp
// Use BlazorOptimizedJsonContext for WebAssembly scenarios
var json = JsonSerializer.Serialize(response, BlazorOptimizedJsonContext.Default.ApiResponse);
var deserialized = JsonSerializer.Deserialize<ApiResponse>(json, BlazorOptimizedJsonContext.Default.ApiResponse);
```

## New Features Available

### **Enhanced Error Analysis**
```csharp
// New extension methods for better error handling
var errorCount = response.GetErrorCount();
var clientErrors = response.GetClientErrorCount();
var serverErrors = response.GetServerErrorCount();

// Group errors by code
var errorGroups = response.GetErrorGroupsByCode();

// Get retryable errors
var retryableErrors = response.GetRetryableErrors();

// Check if only warnings
var onlyWarnings = response.HasOnlyWarnings();
```

### **Blazor UI Helpers**
```csharp
// CSS class helpers for UI
var severityClasses = response.GetCombinedSeverityClasses();
var primaryClass = response.GetPrimarySeverityClass();

// User-friendly messages
var userMessages = response.GetUserFriendlyMessages();

// Detailed logging
var logSummary = response.GetLoggingErrorSummary();
```

### **State Validation**
```csharp
// Validate response consistency
if (!response.IsValidState())
{
    var issues = response.GetStateValidationIssues();
    foreach (var issue in issues)
    {
        logger.LogWarning("Response state issue: {Issue}", issue);
    }
}
```

## Performance Benefits

### **Source Generation Advantages**
- ? **Faster serialization/deserialization**
- ? **Smaller bundle size** in Blazor WebAssembly
- ? **AOT compilation support**
- ? **No runtime reflection**
- ? **Better trimming support**

### **Memory Efficiency**
- Extension methods don't add memory overhead
- Computed values are calculated on-demand
- No caching means always current state

## Backward Compatibility

### **Maintained Functionality**
- ? All existing methods remain unchanged
- ? Error management APIs identical
- ? JSON serialization output unchanged
- ? Fluent APIs still work

### **Breaking Changes**
- ? Computed properties removed (replaced with extension methods)
- ? Must add `using HT.Api.Client.Contracts.Extensions;`
- ? Property syntax ? Method syntax

## Testing Updates

Update unit tests to use extension methods:

```csharp
// Before
Assert.True(response.IsSuccess);
Assert.False(response.HasFailures);

// After
Assert.True(response.IsSuccess());
Assert.False(response.HasFailures());
```

## Common Issues & Solutions

### **Issue: Extension methods not found**
```
CS1061: 'ApiResponse' does not contain a definition for 'IsSuccess'
```

**Solution:** Add the using directive:
```csharp
using HT.Api.Client.Contracts.Extensions;
```

### **Issue: JSON serialization not using source generation**
**Solution:** Configure the JSON context:
```csharp
options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiContractsJsonContext.Default);
```

## Summary

This migration enables:
- ?? **Better performance** in Blazor WebAssembly
- ?? **AOT compilation support**
- ?? **Smaller bundle sizes**
- ?? **Enhanced error handling capabilities**

The changes maintain full functionality while providing significant performance improvements for modern .NET applications.