using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Client.Contracts.Tests
{
    /// <summary>
    /// Test-specific JSON serialization context for System.Text.Json source generation.
    /// Includes test models and API response types for comprehensive testing.
    /// </summary>
    [JsonSerializable(typeof(ApiResponse<TestModel>))]
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(ApiResponse<Dictionary<string, object>>))]
    [JsonSerializable(typeof(TestModel))]
    [JsonSerializable(typeof(MetaData))]
    [JsonSerializable(typeof(Link))]
    [JsonSerializable(typeof(Message))]
    [JsonSerializable(typeof(CallStatusCode))]
    [JsonSerializable(typeof(List<Link>))]
    [JsonSerializable(typeof(List<Message>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        GenerationMode = JsonSourceGenerationMode.Default,
        UseStringEnumConverter = true,
        IncludeFields = false,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true)]
    public partial class TestApiContractsJsonContext : JsonSerializerContext
    {
    }
}