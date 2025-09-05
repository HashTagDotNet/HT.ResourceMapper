using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Client.Contracts.Serialization
{
    /// <summary>
    /// Comprehensive JSON serialization context for System.Text.Json source generation.
    /// This enables AOT compilation and improved performance for both API servers and Blazor WebAssembly clients.
    /// Supports all API contract types for universal compatibility.
    /// </summary>
    [JsonSerializable(typeof(ApiResponse))]
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(ApiResponse<Dictionary<string, object>>))]
    [JsonSerializable(typeof(ErrorMessage))]
    [JsonSerializable(typeof(MetaData))]
    [JsonSerializable(typeof(Link))]
    [JsonSerializable(typeof(Message))]
    [JsonSerializable(typeof(MessageBase))]
    [JsonSerializable(typeof(CallStatusCode))]
    [JsonSerializable(typeof(ResultCategory))]
    [JsonSerializable(typeof(MessageSeverity))]
    [JsonSerializable(typeof(RequestLocation))]
    [JsonSerializable(typeof(List<ErrorMessage>))]
    [JsonSerializable(typeof(List<Link>))]
    [JsonSerializable(typeof(List<Message>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(List<object>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, string?>))]
    [JsonSerializable(typeof(SortedDictionary<string, string>))]
    [JsonSerializable(typeof(SortedDictionary<string, object>))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(object[]))]
    [JsonSerializable(typeof(int?))]
    [JsonSerializable(typeof(RequestLocation?))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        GenerationMode = JsonSourceGenerationMode.Default,
        UseStringEnumConverter = true,
        IncludeFields = false,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true)]
    public partial class ApiContractsJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// API server-optimized JSON context for high-performance serialization scenarios.
    /// Designed for ASP.NET Core APIs that need fast response serialization.
    /// </summary>
    [JsonSerializable(typeof(ApiResponse))]
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(ApiResponse<Dictionary<string, object>>))]
    [JsonSerializable(typeof(ErrorMessage))]
    [JsonSerializable(typeof(MetaData))]
    [JsonSerializable(typeof(Link))]
    [JsonSerializable(typeof(Message))]
    [JsonSerializable(typeof(MessageBase))]
    [JsonSerializable(typeof(CallStatusCode))]
    [JsonSerializable(typeof(ResultCategory))]
    [JsonSerializable(typeof(MessageSeverity))]
    [JsonSerializable(typeof(RequestLocation))]
    [JsonSerializable(typeof(List<ErrorMessage>))]
    [JsonSerializable(typeof(List<Link>))]
    [JsonSerializable(typeof(List<Message>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, string?>))]
    [JsonSerializable(typeof(SortedDictionary<string, string>))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        GenerationMode = JsonSourceGenerationMode.Serialization,
        UseStringEnumConverter = true,
        IncludeFields = false)]
    public partial class ApiServerJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Blazor WebAssembly-optimized JSON context with minimal features for client-side performance.
    /// Optimized for smaller bundle size and faster deserialization in WebAssembly scenarios.
    /// </summary>
    [JsonSerializable(typeof(ApiResponse))]
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(ErrorMessage))]
    [JsonSerializable(typeof(MetaData))]
    [JsonSerializable(typeof(Link))]
    [JsonSerializable(typeof(CallStatusCode))]
    [JsonSerializable(typeof(ResultCategory))]
    [JsonSerializable(typeof(RequestLocation))]
    [JsonSerializable(typeof(List<ErrorMessage>))]
    [JsonSerializable(typeof(List<Link>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(SortedDictionary<string, string>))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        GenerationMode = JsonSourceGenerationMode.Default,
        UseStringEnumConverter = true,
        IncludeFields = false,
        PropertyNameCaseInsensitive = true)]
    public partial class BlazorOptimizedJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Minimal JSON context for scenarios where only basic API response types are needed.
    /// Useful for microservices or specialized clients that only handle specific response types.
    /// </summary>
    [JsonSerializable(typeof(ApiResponse))]
    [JsonSerializable(typeof(ErrorMessage))]
    [JsonSerializable(typeof(CallStatusCode))]
    [JsonSerializable(typeof(ResultCategory))]
    [JsonSerializable(typeof(List<ErrorMessage>))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        GenerationMode = JsonSourceGenerationMode.Default,
        UseStringEnumConverter = true)]
    public partial class MinimalApiJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Development-friendly JSON context with additional options for debugging and development scenarios.
    /// Includes indented output and better developer experience.
    /// </summary>
    [JsonSerializable(typeof(ApiResponse))]
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(ErrorMessage))]
    [JsonSerializable(typeof(MetaData))]
    [JsonSerializable(typeof(Link))]
    [JsonSerializable(typeof(Message))]
    [JsonSerializable(typeof(MessageBase))]
    [JsonSerializable(typeof(CallStatusCode))]
    [JsonSerializable(typeof(ResultCategory))]
    [JsonSerializable(typeof(MessageSeverity))]
    [JsonSerializable(typeof(RequestLocation))]
    [JsonSerializable(typeof(List<ErrorMessage>))]
    [JsonSerializable(typeof(List<Link>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, string?>))]
    [JsonSerializable(typeof(SortedDictionary<string, string>))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true, // For development readability
        GenerationMode = JsonSourceGenerationMode.Default,
        UseStringEnumConverter = true,
        IncludeFields = false,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true)]
    public partial class DevelopmentJsonContext : JsonSerializerContext
    {
    }
}