# HT.Microsoft.IConfiguration.Extensions - Enhanced Configuration Extensions

## Overview
This library provides comprehensive extension methods for `IConfiguration` to simplify configuration management in .NET applications with type-safe, validated, and convenient access patterns.

## Key Features

### ? **Type-Safe Configuration Access**
- **String Methods**: `GetString()`, `GetRequiredString()`
- **Integer Methods**: `GetInt()` with validation and defaults
- **Boolean Methods**: `GetBool()`, `GetBoolOrDefault()`
- **Double/Decimal Methods**: `GetDouble()`, `GetDecimal()`
- **DateTime Methods**: `GetDateTime()` with parsing validation
- **TimeSpan Methods**: `GetTimeSpan()`, `GetTimeSpanFromSeconds()`, `GetTimeSpanFromMilliseconds()`
- **Enum Methods**: `GetEnum<T>()` with case-insensitive parsing
- **URI Methods**: `GetUri()` with validation

### ? **Array and Collection Support**
- **String Arrays**: `GetStringArray()` with customizable separators
- **Integer Arrays**: `GetIntArray()` with validation
- Support for comma-separated or custom delimiter values

### ? **Strongly-Typed Configuration Sections**
- **Generic Binding**: `GetTypedSection<T>()` for POCO binding
- **Default Values**: Fallback to default instances when sections are missing
- **Automatic Validation**: Section existence checking

### ? **Connection String Management**
- **Enhanced Access**: `GetRequiredConnectionString()` with validation
- **Validation Methods**: `ValidateRequiredConnectionStrings()`

### ? **Environment Detection**
- **Environment Helpers**: `GetEnvironmentName()`, `IsDevelopment()`, `IsProduction()`, `IsStaging()`
- **Custom Environment Checks**: `IsEnvironment()` with case sensitivity options

### ? **Validation and Error Handling**
- **Required Key Validation**: `ValidateRequiredKeys()` 
- **Descriptive Exceptions**: Detailed error messages with suggestions
- **Graceful Defaults**: Optional vs required value handling

### ? **Utility Methods**
- **Prefix Filtering**: `GetByPrefix()` for related configuration groups
- **Value Existence**: `HasValue()` for checking non-empty values
- **Suggestion Support**: `GetRequiredString()` with suggested alternatives

## Usage Examples

### Basic Type Access
```csharp
// String with default
var connectionTimeout = configuration.GetString("Database:Timeout", "30");

// Integer with validation
var maxRetries = configuration.GetInt("Api:MaxRetries", 3);

// Boolean with default
var debugMode = configuration.GetBoolOrDefault("Debug:Enabled", false);

// TimeSpan from seconds
var cacheExpiry = configuration.GetTimeSpanFromSeconds("Cache:ExpirySeconds", 300);
```

### Strongly-Typed Sections
```csharp
// Get entire configuration section as POCO
var apiConfig = configuration.GetTypedSection<ApiConfiguration>("Api");

// With default fallback
var clientConfig = configuration.GetTypedSection("Client", new ClientConfiguration());
```

### Environment Detection
```csharp
// Check environment
if (configuration.IsDevelopment())
{
    // Development-specific logic
}

// Get environment name
var env = configuration.GetEnvironmentName("Production");
```

### Validation
```csharp
// Validate required keys
configuration.ValidateRequiredKeys("Database:ConnectionString", "Api:Key");

// Validate connection strings
configuration.ValidateRequiredConnectionStrings("DefaultConnection", "LoggingConnection");
```

### Array Configuration
```csharp
// String array from comma-separated values
var allowedHosts = configuration.GetStringArray("Security:AllowedHosts");

// Integer array with custom separator
var ports = configuration.GetIntArray("Server:Ports", ";");
```

### Enum Configuration
```csharp
public enum LogLevel { Debug, Info, Warning, Error }

// Parse enum with validation
var logLevel = configuration.GetEnum<LogLevel>("Logging:Level", LogLevel.Info);
```

## Error Handling

### Descriptive Exceptions
All methods provide clear, actionable error messages:
```csharp
// KeyNotFoundException with context
"Configuration value for key 'Database:Timeout' is required."

// FormatException with suggestions
"Configuration value for key 'Api:MaxRetries' must be a valid integer. Got: 'abc'"

// Enum with valid options
"Configuration value for key 'LogLevel' must be a valid LogLevel. Got: 'INVALID'. Valid values: Debug, Info, Warning, Error"
```

### Suggestion Support
```csharp
// Provides helpful suggestions for typos
var value = configuration.GetRequiredString("DataBase:ConnectionString", 
    "Database:ConnectionString", "ConnectionStrings:DefaultConnection");
```

## Integration Examples

### Program.cs Setup
```csharp
// Validate configuration early
configuration.ValidateRequiredKeys("AllowedHosts");

// Load strongly-typed configuration
var appConfig = configuration.GetTypedSection<AppConfiguration>("App");
builder.Services.AddSingleton(appConfig);

// Environment-specific setup
if (configuration.IsDevelopment())
{
    builder.Services.AddSwagger();
}
```

### Service Configuration
```csharp
public class DatabaseService
{
    public DatabaseService(IConfiguration configuration)
    {
        ConnectionString = configuration.GetRequiredConnectionString("DefaultConnection");
        CommandTimeout = configuration.GetTimeSpanFromSeconds("Database:CommandTimeoutSeconds", 30);
        MaxRetries = configuration.GetInt("Database:MaxRetries", 3);
    }
}
```

## Best Practices

### 1. **Use Strongly-Typed Configuration**
Prefer `GetTypedSection<T>()` over individual property access for related settings.

### 2. **Validate Early**
Use `ValidateRequiredKeys()` in `Program.cs` to fail fast on missing configuration.

### 3. **Provide Sensible Defaults**
Always specify appropriate default values for non-critical settings.

### 4. **Use Environment Helpers**
Leverage `IsDevelopment()`, `IsProduction()` for environment-specific logic.

### 5. **Handle Arrays Consistently**
Use `GetStringArray()` and `GetIntArray()` for consistent parsing behavior.

## Dependencies
- `Microsoft.Extensions.Configuration.Abstractions` (9.0.5)
- `Microsoft.Extensions.Configuration.Binder` (9.0.5)

## Compatibility
- **.NET 9** target framework
- **C# 13** features supported
- **Nullable reference types** enabled