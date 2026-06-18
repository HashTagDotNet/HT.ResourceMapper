# API Service Patterns and Validation Guidelines

## Overview

This document outlines the established patterns for handling API requests with testable validation in the ResourceMapper project. The patterns emphasize simplicity, testability, and maintainability without requiring injectable validation classes.

## Core Principles

1. **No Injectable Validation Classes**: Validation logic is embedded directly in service methods as static functions, making it easier to develop and debug.
2. **Testable Validation**: All validation methods are public static methods that can be unit tested independently.
3. **Consistent Error Handling**: All services follow the same pattern for handling validation errors and exceptions.
4. **Separation of Concerns**: Business logic, validation, and response building are clearly separated.

## Architecture Components

### 1. BaseApiService Class

**Location**: `Modules\Common\ResourceMapper.Common.Server\Base\BaseApiService.cs`

The base class provides common patterns for all API services:

```csharp
public abstract class BaseApiService
{
    protected async Task<ApiServiceResponse<TResponse>> ExecuteApiOperationAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, ValidationResult> validateRequest,
        Func<TRequest, CancellationToken, Task<TResponse>> executeOperation,
        CancellationToken cancellationToken,
        string operationName = "API Operation")
```

**Key Features**:
- Standardized request/response handling
- Built-in validation integration
- Consistent error handling and logging
- Support for both async and sync operations

### 2. Validation Framework

**Components**:
- `ValidationResult`: Contains validation errors and success state
- `ValidationError`: Represents a single validation error with property name and message
- `ValidationHelpers`: Common validation utility methods

**Example Usage**:
```csharp
public static ValidationResult ValidateGetDaysAgoRequest(SampleGetDateTimeRequest request)
{
    return ValidationHelpers.Combine(
        ValidationHelpers.ValidateRequired(request, nameof(request)),
        ValidateDateOffset(request?.DateOffsetToGet ?? 0)
    );
}
```

## Service Implementation Pattern

### 1. Service Structure

```csharp
public class SampleService : BaseApiService, ISampleService
{
    private readonly ISampleRepository _repo;

    public SampleService(ILogger<SampleService> logger, ISampleRepository repo) : base(logger)
    {
        _repo = repo;
    }

    public async Task<ApiServiceResponse<TResponse>> MethodAsync(TRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteApiOperationAsync(
            request,
            ValidateRequest,        // Validation function
            ExecuteBusinessLogic,   // Business logic function
            cancellationToken,
            "OperationName");
    }

    // Public static validation methods for testing
    public static ValidationResult ValidateRequest(TRequest request)
    {
        // Validation logic here
    }

    private static ValidationResult ValidateSpecificField(...)
    {
        // Specific field validation
    }
}
```

### 2. Validation Method Guidelines

**Requirements**:
- Must be `public static` for easy unit testing
- Should return `ValidationResult`
- Should use `ValidationHelpers.Combine()` for multiple validations
- Should break complex validation into smaller private static methods

**Example**:
```csharp
public static ValidationResult ValidateUserRegistrationRequest(UserRegistrationRequest request)
{
    if (request == null)
    {
        var result = new ValidationResult();
        result.AddError(nameof(request), "Request cannot be null");
        return result;
    }

    return ValidationHelpers.Combine(
        ValidateEmail(request.Email),
        ValidateName(request.FirstName, nameof(request.FirstName)),
        ValidateName(request.LastName, nameof(request.LastName)),
        ValidateAge(request.Age),
        ValidatePhoneNumber(request.PhoneNumber)
    );
}
```

## Testing Patterns

### 1. Validation Testing

Test validation methods directly without mocking:

```csharp
[Fact]
public void ValidateGetDaysAgoRequest_WithValidRequest_ReturnsValid()
{
    // Arrange
    var request = new SampleGetDateTimeRequest { DateOffsetToGet = 5 };

    // Act
    var result = SampleService.ValidateGetDaysAgoRequest(request);

    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
}

[Theory]
[InlineData(-11)]
[InlineData(-10)]
public void ValidateGetDaysAgoRequest_WithTooLowOffset_ReturnsInvalid(int offset)
{
    // Arrange
    var request = new SampleGetDateTimeRequest { DateOffsetToGet = offset };

    // Act
    var result = SampleService.ValidateGetDaysAgoRequest(request);

    // Assert
    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal(nameof(request.DateOffsetToGet), result.Errors.First().PropertyName);
    Assert.Equal("Date offset must be greater than -10", result.Errors.First().ErrorMessage);
}
```

### 2. Integration Testing

Test the full service flow with mocked dependencies:

```csharp
[Fact]
public async Task GetDaysAgoAsync_WithValidRequest_ReturnsSuccessResponse()
{
    // Arrange
    var request = new SampleGetDateTimeRequest { DateOffsetToGet = 5 };
    var expectedDate = DateTime.Now.AddDays(-5);
    _mockRepository.Setup(r => r.GetDaysAgoAysnc(5, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expectedDate);

    // Act
    var result = await _sampleService.GetDaysAgoAsync(request, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(HttpStatusCode.OK, result.HttpStatusCode);
    Assert.NotNull(result.Data);
    Assert.Equal(expectedDate, result.Data.FoundDate);
}
```

## Validation Helpers

### Available Helper Methods

```csharp
// Required field validation
ValidationHelpers.ValidateRequired(value, propertyName, customMessage)

// Range validation
ValidationHelpers.ValidateRange(value, propertyName, min, max, customMessage)

// String length validation
ValidationHelpers.ValidateStringLength(value, propertyName, maxLength, minLength, customMessage)

// Combine multiple validation results
ValidationHelpers.Combine(result1, result2, result3, ...)
```

### Custom Validation Example

```csharp
private static ValidationResult ValidateEmail(string? email)
{
    var result = ValidationHelpers.ValidateRequired(email, nameof(UserRegistrationRequest.Email));
    
    if (result.IsValid && !string.IsNullOrWhiteSpace(email))
    {
        if (!email.Contains('@') || !email.Contains('.'))
        {
            result.AddError(nameof(UserRegistrationRequest.Email), "Email must be in valid format");
        }
        
        if (email.Length > 255)
        {
            result.AddError(nameof(UserRegistrationRequest.Email), "Email cannot be longer than 255 characters");
        }
    }
    
    return result;
}
```

## Benefits of This Approach

### 1. Testability
- Validation methods are pure functions (static, no dependencies)
- Easy to unit test with various input scenarios
- No need to mock validation dependencies

### 2. Simplicity
- No complex validation frameworks to learn
- Validation logic is co-located with business logic
- Easy debugging and maintenance

### 3. Consistency
- All services follow the same pattern
- Standardized error responses
- Consistent logging and exception handling

### 4. Flexibility
- Easy to add complex validation rules
- Can combine simple validations into complex ones
- Custom validation messages and error codes

## Examples in Codebase

1. **Simple Validation**: `SampleService.ValidateGetDaysAgoRequest()`
2. **Complex Validation**: `UserService.ValidateUserRegistrationRequest()`
3. **Unit Tests**: `SampleServiceTests` and `UserServiceValidationTests`
4. **Base Pattern**: `BaseApiService.ExecuteApiOperationAsync()`

## Best Practices

1. Always validate null requests first
2. Use `ValidationHelpers.Combine()` for multiple validations
3. Make validation methods `public static` for testing
4. Break complex validations into smaller methods
5. Use meaningful error messages
6. Include property names in validation errors
7. Test both valid and invalid scenarios
8. Use Theory tests for multiple similar test cases