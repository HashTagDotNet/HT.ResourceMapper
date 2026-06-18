# API Contracts JSON Serialization Configuration Guide

## Overview

The API contracts library provides multiple JSON serialization contexts optimized for different scenarios while maintaining **universal compatibility**. This ensures that source generation works for both API servers and various client types, including **non-Blazor clients**.

## Universal API Compatibility Strategy

### ?? **Design Principles**
- **API Server Independence**: JSON contexts work regardless of server technology
- **Client Agnostic**: Supports Blazor WASM, desktop apps, mobile apps, and server-to-server communication
- **Source Generation First**: All contexts use compile-time code generation for optimal performance
- **AOT Compatible**: Designed for Native AOT scenarios including Blazor WebAssembly

## Available JSON Contexts

### 1. **ApiContractsJsonContext** - Universal Default ?
- **Use Case**: Default choice for most scenarios
- **Features**: Complete type coverage, both serialization and deserialization
- **Best For**: General-purpose APIs, client libraries, testing, non-Blazor clients

```csharp
// Universal configuration for any .NET client
options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiContractsJsonContext.Default);

// Works with any HTTP client
var response = await httpClient.GetFromJsonAsync<ApiResponse>("api/endpoint", ApiContractsJsonContext.Default.ApiResponse);
```

### 2. **ApiServerJsonContext** - Server Optimized ??
- **Use Case**: ASP.NET Core APIs that primarily serialize responses
- **Features**: Serialization-focused, high performance, minimal footprint
- **Best For**: Web APIs, REST endpoints, microservices

```csharp
// Program.cs for ASP.NET Core API (any hosting model)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiServerJsonContext.Default);
});

// Minimal API
app.MapGet("/api/data", () => new ApiResponse<DataDto> { Data = data });

// Controller API
[HttpGet]
public ApiResponse<UserDto> GetUser(int id) => new() { Data = userData };
```

### 3. **BlazorOptimizedJsonContext** - WebAssembly Client ??
- **Use Case**: Blazor WebAssembly applications
- **Features**: Minimal bundle size, both read/write, case-insensitive
- **Best For**: Blazor WASM, PWAs, client-side applications

```csharp
// Program.cs for Blazor WebAssembly
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, BlazorOptimizedJsonContext.Default);
});

// Blazor component usage
var response = await Http.GetFromJsonAsync<ApiResponse>("api/data", BlazorOptimizedJsonContext.Default.ApiResponse);
```

### 4. **MinimalApiJsonContext** - Lightweight ?
- **Use Case**: Microservices, health checks, simple endpoints
- **Features**: Only core types, smallest footprint
- **Best For**: Health endpoints, simple APIs, constrained environments

```csharp
// Health check endpoint
app.MapGet("/health", () => 
{
    var response = new ApiResponse();
    return Results.Json(response, MinimalApiJsonContext.Default.ApiResponse);
});
```

### 5. **DevelopmentJsonContext** - Development/Debug ??
- **Use Case**: Development, debugging, API exploration
- **Features**: Indented output, human-readable, complete types
- **Best For**: Development environments, debugging, API documentation

```csharp
#if DEBUG
var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
{ 
    TypeInfoResolver = DevelopmentJsonContext.Default,
    WriteIndented = true 
});
logger.LogDebug("API Response: {Response}", json);
#endif
```

## Universal Client Examples

### ??? **Console Application / Worker Service**
```csharp
using HT.Api.Client.Contracts.Serialization;
using HT.Api.Client.Contracts.Extensions;

// Works with any .NET application
class ApiClient
{
    private readonly HttpClient _httpClient;
    
    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<T?> GetDataAsync<T>(string endpoint) where T : class, new()
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<T>>(
            endpoint, 
            ApiContractsJsonContext.Default.ApiResponse);
            
        return response?.IsSuccess() == true ? response.Data : null;
    }
}
```

### ?? **Mobile App (MAUI)**
```csharp
// Optimized for mobile
public class MobileApiService
{
    private readonly HttpClient _httpClient;
    
    public async Task<ApiResponse<UserProfile>> GetProfileAsync(int userId)
    {
        try
        {
            // Use Blazor context for small footprint
            return await _httpClient.GetFromJsonAsync<ApiResponse<UserProfile>>(
                $"api/users/{userId}", 
                BlazorOptimizedJsonContext.Default.ApiResponse) 
                ?? new ApiResponse<UserProfile>();
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<UserProfile>();
            errorResponse.AddInternalError($"Failed to get profile: {ex.Message}");
            return errorResponse;
        }
    }
}
```

### ?? **Server-to-Server Communication**
```csharp
// Microservice communication
public class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    
    public async Task<bool> NotifyExternalSystemAsync(NotificationData data)
    {
        var request = new ApiResponse<NotificationData> { Data = data };
        
        // Serialize for external API
        var json = JsonSerializer.Serialize(request, ApiContractsJsonContext.Default.ApiResponse);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("api/notifications", content);
        
        if (!response.IsSuccessStatusCode) return false;
        
        // Deserialize response
        var responseStream = await response.Content.ReadAsStreamAsync();
        var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponse>(
            responseStream, 
            ApiContractsJsonContext.Default.ApiResponse);
            
        return apiResponse?.IsSuccess() ?? false;
    }
}
```

### ?? **Desktop Application (WPF/WinUI)**
```csharp
public class DesktopApiService
{
    private readonly HttpClient _httpClient;
    
    public async Task<List<T>> GetPagedDataAsync<T>(int page, int size) where T : class, new()
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<T>>>(
            $"api/data?page={page}&size={size}",
            ApiContractsJsonContext.Default.ApiResponse);
            
        if (response?.IsSuccess() == true)
        {
            return response.Data?.Items ?? new List<T>();
        }
        
        // Handle errors appropriately for desktop UI
        if (response?.HasClientErrors() == true)
        {
            throw new InvalidOperationException(response.GetErrorSummary());
        }
        
        return new List<T>();
    }
}
```

## API Server Implementation Examples

### ?? **ASP.NET Core Minimal API**
```csharp
using HT.Api.Client.Contracts.Serialization;
using HT.Api.Client.Contracts.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure for optimal server performance
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiServerJsonContext.Default);
});

var app = builder.Build();

// Example endpoint - works for ALL client types
app.MapGet("/api/users/{id}", async (int id, UserService userService) =>
{
    var response = new ApiResponse<UserDto>();
    
    if (id <= 0)
    {
        response.AddValidationError("id", "User ID must be greater than 0");
        return Results.BadRequest(response);
    }
    
    var user = await userService.GetUserAsync(id);
    if (user == null)
    {
        response.AddNotFoundError("User not found");
        return Results.NotFound(response);
    }
    
    response.Data = user;
    return Results.Ok(response); // Source generation handles serialization
});

// Health check with minimal context
app.MapGet("/health", () =>
{
    var response = new ApiResponse();
    return Results.Json(response, MinimalApiJsonContext.Default.ApiResponse);
});
```

### ?? **ASP.NET Core Controller API**
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType<ApiResponse<UserDto>>(200)]
    [ProducesResponseType<ApiResponse>(400)]
    [ProducesResponseType<ApiResponse>(404)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        var response = new ApiResponse<UserDto>();
        
        // Validation
        if (id <= 0)
        {
            response.AddValidationError("id", "User ID must be greater than 0");
            return BadRequest(response); // Works for ALL clients
        }
        
        // Business logic
        var user = await _userService.GetUserAsync(id);
        if (user == null)
        {
            response.AddNotFoundError("User not found");
            return NotFound(response); // Works for ALL clients
        }
        
        // Success
        response.Data = user;
        return Ok(response); // Works for ALL clients
    }
}
```

## Performance Characteristics

### Bundle Size Impact (WebAssembly/AOT)
- **MinimalApiJsonContext**: ~12KB
- **BlazorOptimizedJsonContext**: ~18KB  
- **ApiContractsJsonContext**: ~25KB
- **ApiServerJsonContext**: ~20KB (serialization only)
- **DevelopmentJsonContext**: ~28KB

### Serialization Performance
| Context | Serialization | Deserialization | Use Case |
|---------|---------------|-----------------|----------|
| **ApiServerJsonContext** | ????? | ??? | API servers |
| **BlazorOptimizedJsonContext** | ???? | ????? | WebAssembly clients |
| **ApiContractsJsonContext** | ???? | ???? | Universal/Default |
| **MinimalApiJsonContext** | ????? | ????? | Simple scenarios |

## Context Selection Guide

| Client Type | Recommended Context | Reason |
|-------------|-------------------|---------|
| **ASP.NET Core API** | `ApiServerJsonContext` | Optimized for response serialization |
| **Blazor WebAssembly** | `BlazorOptimizedJsonContext` | Minimal bundle, WASM optimized |
| **Console App** | `ApiContractsJsonContext` | Complete feature set |
| **Desktop App (WPF/WinUI)** | `ApiContractsJsonContext` | Full compatibility |
| **Mobile App (MAUI)** | `BlazorOptimizedJsonContext` | Small footprint |
| **Microservice** | `MinimalApiJsonContext` | Lightweight |
| **Server-to-Server** | `ApiContractsJsonContext` | Robust feature set |
| **Development/Debug** | `DevelopmentJsonContext` | Human-readable output |

## Best Practices for Universal Compatibility

### 1. **API Design**
```csharp
// ? Good: Always return ApiResponse<T> for consistency
[HttpGet]
public ActionResult<ApiResponse<UserDto>> GetUser(int id)

// ? Avoid: Direct return types that vary by endpoint
[HttpGet]
public ActionResult<UserDto> GetUser(int id)
```

### 2. **Error Handling**
```csharp
// ? Good: Use ErrorCode-first approach
response.AddValidationError("email", "Invalid format");
var httpStatus = response.GetPrimaryHttpStatusCode(); // Derived automatically

// ? Avoid: HTTP status code first
return BadRequest("Invalid email"); // Loses error structure
```

### 3. **Client Configuration**
```csharp
// ? Good: Configure at startup
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiContractsJsonContext.Default);
});

// ? Avoid: Per-request configuration
var options = new JsonSerializerOptions { TypeInfoResolver = SomeContext.Default };
```

### 4. **Backward Compatibility**
```csharp
// ? Good: Graceful fallback for unknown error codes
var errorMessage = errorCode.GetDescription(); // Returns "Unknown error" for undefined codes

// ? Good: Extension methods maintain functionality
if (response.IsSuccess()) { /* handle success */ }
```

## Migration Path

### From Reflection-Based JSON
```csharp
// Before (reflection at runtime)
var response = await httpClient.GetFromJsonAsync<ApiResponse>("api/data");

// After (compile-time source generation)
var response = await httpClient.GetFromJsonAsync<ApiResponse>(
    "api/data", 
    ApiContractsJsonContext.Default.ApiResponse);
```

### Context Migration Strategy
1. **Start with ApiContractsJsonContext** for universal compatibility
2. **Optimize specific scenarios** with specialized contexts
3. **Use DevelopmentJsonContext** during development/debugging
4. **Switch to production contexts** for deployment

## Testing Across Client Types

### Unit Test Example
```csharp
[Test]
public void ApiResponse_SerializesCorrectly_ForAllContexts()
{
    var response = new ApiResponse<UserDto>
    {
        Data = new UserDto { Id = 1, Name = "Test User" }
    };
    
    // Test with different contexts
    var contexts = new JsonSerializerContext[]
    {
        ApiContractsJsonContext.Default,
        ApiServerJsonContext.Default,
        BlazorOptimizedJsonContext.Default,
        DevelopmentJsonContext.Default
    };
    
    foreach (var context in contexts)
    {
        var json = JsonSerializer.Serialize(response, context.ApiResponse);
        var deserialized = JsonSerializer.Deserialize<ApiResponse<UserDto>>(json, context.ApiResponse);
        
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(response.Data.Id, deserialized.Data?.Id);
    }
}
```

This configuration ensures your API contracts work efficiently across **all client types** while maintaining the benefits of source generation for both servers and clients, regardless of whether they use Blazor or not.