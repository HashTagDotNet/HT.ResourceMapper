# ErrorCode-First API Response Pattern

## Overview

This document outlines the ErrorCode-first approach for determining success/failure in API responses. In this hybrid system:

- **Developers set ErrorCodes** (primary mechanism)
- **HTTP Status Codes are derived automatically** from ErrorCodes
- **Flexibility** - Developers can still override HTTP status codes when needed

## Core Philosophy

### ErrorCode = Primary Success/Failure Determinant
```csharp
// Developer focuses on business logic error codes
response.AddError(ErrorCodes.NotFound, "User not found");
response.AddError(ErrorCodes.InvalidArgument, "Email format is invalid", "Email");

// HTTP status codes are automatically derived:
// ErrorCodes.NotFound ? HttpStatusCode.NotFound (404)
// ErrorCodes.InvalidArgument ? HttpStatusCode.BadRequest (400)
```

### HTTP Status Code = Secondary/Transport Layer
```csharp
// Automatic derivation (preferred)
var httpStatus = errorCode.ToHttpStatusCode();

// Manual override (when needed)
response.SetStatusCode(HttpStatusCode.BadRequest, "Custom message");
```

## Success/Failure Logic

### Success Determination
```csharp
// ErrorCode-based success checking
if (errorCode.IsSuccess()) // Only ErrorCodes.Ok returns true
{
    // Handle success
}

// Response-level success checking
if (response.ApiResponse.IsSuccess) // No errors or all errors are success codes
{
    // Handle success
}
```

### Failure Categories
```csharp
// Client Errors (4xx equivalent)
errorCode.IsClientError() // InvalidArgument, NotFound, PermissionDenied, etc.

// Server Errors (5xx equivalent)  
errorCode.IsServerError() // InternalError, Unavailable, NotImplemented, etc.

// Cancellation (neutral)
errorCode.IsCancelled() // Cancelled operations
```

## ErrorCode Hierarchy

### Success States
- `ErrorCodes.Ok = 0` - Operation successful

### Special States  
- `ErrorCodes.Cancelled = 1` - Operation cancelled (not failure)

### Error States (All have Error bit flag = 8)
- `ErrorCodes.Error = 8` - Generic error
- All other errors are `Error | specific_bit`

## Developer Workflow Examples

### 1. Service Implementation (ErrorCode-First)
```csharp
public class UserService : BaseApiService
{
    public async Task<ApiServiceResponse<UserResponse>> GetUserAsync(int userId)
    {
        return await ExecuteApiOperationAsync(
            new { UserId = userId },
            ValidateGetUserRequest,
            async (req, ct) => 
            {
                var user = await _repo.GetUserAsync(req.UserId, ct);
                if (user == null)
                {
                    // Developer sets ErrorCode - HTTP status derived automatically
                    throw new BusinessException(ErrorCodes.NotFound, "User not found");
                }
                return new UserResponse { User = user };
            },
            cancellationToken,
            "GetUser");
    }

    // Validation using ErrorCode approach
    private ValidationResult ValidateGetUserRequest(dynamic request)
    {
        var result = new ValidationResult();
        if (request.UserId <= 0)
        {
            result.AddError("UserId", "User ID must be greater than 0");
        }
        return result;
    }
}
```

### 2. Manual Error Handling
```csharp
public async Task<ApiServiceResponse<UserResponse>> CreateUserAsync(CreateUserRequest request)
{
    var response = new ApiServiceResponse<UserResponse>();
    
    // Validation errors
    if (await _repo.UserExistsAsync(request.Email))
    {
        response.AddErrorCode(ErrorCodes.AlreadyExists, 
            "A user with this email already exists", "Email");
        return response;
    }
    
    // Business logic errors
    if (!_permissionService.CanCreateUser(request.Role))
    {
        response.AddErrorCode(ErrorCodes.PermissionDenied, 
            "Insufficient permissions to create user with this role");
        return response;
    }
    
    // Success case
    var user = await _repo.CreateUserAsync(request);
    return response.SetDataWithErrorCode(user, ErrorCodes.Ok, "User created successfully");
}
```

### 3. Complex Error Scenarios
```csharp
public async Task<ApiServiceResponse<BulkUpdateResponse>> BulkUpdateUsersAsync(BulkUpdateRequest request)
{
    var response = new ApiServiceResponse<BulkUpdateResponse>();
    var results = new List<UserUpdateResult>();
    
    foreach (var userUpdate in request.Users)
    {
        try
        {
            await _repo.UpdateUserAsync(userUpdate);
            results.Add(new UserUpdateResult { Success = true, UserId = userUpdate.Id });
        }
        catch (UserNotFoundException)
        {
            // Add error but continue processing
            response.AddErrorCode(ErrorCodes.NotFound, 
                $"User {userUpdate.Id} not found", $"Users[{userUpdate.Id}]");
            results.Add(new UserUpdateResult { Success = false, UserId = userUpdate.Id });
        }
        catch (PermissionException)
        {
            response.AddErrorCode(ErrorCodes.PermissionDenied, 
                $"No permission to update user {userUpdate.Id}", $"Users[{userUpdate.Id}]");
            results.Add(new UserUpdateResult { Success = false, UserId = userUpdate.Id });
        }
    }
    
    return response.SetDataWithErrorCode(new BulkUpdateResponse { Results = results }, 
        response.HasFailures ? ErrorCodes.Error : ErrorCodes.Ok);
}
```

## Blazor Client Usage

### 1. Component Error Handling
```razor
@code {
    private async Task LoadUser(int userId)
    {
        var response = await UserService.GetUserAsync(userId);
        
        // ErrorCode-based checking
        if (response.ApiResponse.IsSuccess)
        {
            user = response.Data;
            errorMessage = null;
        }
        else if (response.ApiResponse.HasErrorCode(ErrorCodes.NotFound))
        {
            errorMessage = "User not found";
        }
        else if (response.ApiResponse.HasClientErrors)
        {
            errorMessage = "Please check your input and try again";
        }
        else if (response.ApiResponse.HasServerErrors)
        {
            errorMessage = "Server error. Please try again later";
        }
    }
}
```

### 2. UI Error Display
```razor
@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="@GetAlertClass()">
        <i class="@GetIconClass()"></i>
        @errorMessage
        
        @if (response?.ApiResponse.HighestPriorityError?.IsRetryable == true)
        {
            <button @onclick="RetryOperation" class="btn btn-sm btn-outline-primary">
                Retry
            </button>
        }
    </div>
}

@code {
    private string GetAlertClass()
    {
        var errorCode = response?.ApiResponse.HighestPriorityError?.StatusCode ?? ErrorCodes.Error;
        return errorCode.GetAlertClass();
    }
    
    private string GetIconClass()
    {
        var errorCode = response?.ApiResponse.HighestPriorityError?.StatusCode ?? ErrorCodes.Error;
        return errorCode.GetIconClass();
    }
}
```

### 3. Form Validation Display
```razor
<EditForm Model="userModel" OnValidSubmit="SubmitUser">
    <div class="form-group">
        <label>Email</label>
        <InputText @bind-Value="userModel.Email" class="form-control" />
        
        @if (response?.ApiResponse.HasErrorsForProperty("Email") == true)
        {
            var emailErrors = response.ApiResponse.GetErrorsForProperty("Email");
            foreach (var error in emailErrors)
            {
                <div class="@error.AlertClass">
                    <i class="@error.IconClass"></i>
                    @error.Detail
                </div>
            }
        }
    </div>
</EditForm>
```

## ErrorCode Extension Methods

### Success/Failure Checking
```csharp
errorCode.IsSuccess()           // Only ErrorCodes.Ok
errorCode.IsFailure()           // Any error except Cancelled
errorCode.IsCancelled()         // Cancelled operations
```

### Error Classification
```csharp
errorCode.IsClientError()       // 4xx equivalent errors
errorCode.IsServerError()       // 5xx equivalent errors
errorCode.IsRetryable()         // Errors that can be retried
```

### UI Support
```csharp
errorCode.GetSeverityClass()    // "success", "warning", "danger", "info"
errorCode.GetAlertClass()       // "alert-success", "alert-warning", etc.
errorCode.GetIconClass()        // "fa-check-circle", "fa-exclamation-triangle", etc.
```

### Business Logic Support
```csharp
errorCode.GetPriority()         // 1-5 (1 = highest priority)
errorCode.GetResultCategory()   // Success, ClientError, ServerError, etc.
errorCode.ToHttpStatusCode()    // Convert to HTTP status
```

### User Guidance
```csharp
errorCode.GetDescription()              // Technical description
errorCode.GetUserActionSuggestion()     // User-friendly action
errorCode.GetDeveloperActionSuggestion() // Developer troubleshooting
```

## Benefits

### 1. Developer Experience
- **Focus on business logic** - Set ErrorCodes, HTTP status derived automatically
- **Consistent error handling** - Same patterns across all services  
- **Rich error information** - Built-in categorization and suggestions
- **Easy testing** - Test ErrorCodes directly without HTTP concerns

### 2. Blazor UI Experience
- **Rich error display** - CSS classes, icons, priorities automatically available
- **Property-specific errors** - Easy field-level validation display
- **User guidance** - Built-in action suggestions for different error types
- **Retry logic** - Built-in retry indicators for appropriate errors

### 3. API Consistency
- **Predictable HTTP status codes** - Automatic derivation ensures consistency
- **Detailed error information** - ErrorCodes provide more context than HTTP status alone
- **Flexible overrides** - Can still set custom HTTP status when needed
- **Transport agnostic** - ErrorCodes work regardless of transport protocol

## Migration Strategy

### Phase 1: Add ErrorCode Support (Non-Breaking)
- Add ErrorCode-based methods alongside existing HTTP status methods
- Existing code continues to work unchanged
- New code can adopt ErrorCode-first approach

### Phase 2: Gradual Adoption
- Update new services to use ErrorCode-first approach
- Migrate existing services over time
- Both approaches coexist during transition

### Phase 3: Optimization
- Eventually deprecate direct HTTP status setting where appropriate
- Standardize on ErrorCode-first for all new development
- Maintain compatibility for legacy scenarios

This approach provides a smooth transition path while immediately enabling the benefits of ErrorCode-first development.