using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Interfaces;

// ReSharper disable InconsistentNaming

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// Generic API response following JSON:API specification with CallStatusCode-first approach.
    /// This class represents a response that can contain either data of type TData OR errors, but never both.
    /// CallStatusCodes are the primary status indicators - HTTP status codes are derived automatically.
    /// 
    /// Usage Examples:
    /// <code>
    /// // Success with data
    /// var response = ApiResponse&lt;UserDto&gt;.Success(userData);
    /// 
    /// // Error response
    /// var errorResponse = ApiResponse&lt;UserDto&gt;.Error(CallStatusCode.NotFound, "User not found");
    /// 
    /// // Fluent API
    /// var validationResponse = new ApiResponse&lt;UserDto&gt;()
    ///     .WithValidationError("email", "Email is required");
    /// </code>
    /// 
    /// NOTE: Computed properties and Blazor-specific methods have been moved to ApiResponseExtensions 
    /// for source generation compatibility. Use extension methods for enhanced functionality.
    /// 
    /// JSON:API Specification: https://jsonapi.org/format/#document-top-level
    /// </summary>
    /// <typeparam name="TData">The type of data payload. Must be a reference type with parameterless constructor.</typeparam>
    public class ApiResponse<TData> : IApiResponse<TData> where TData : class, new()
    {
        /// <summary>
        /// The payload of the response. Data and Errors cannot be included in same document https://jsonapi.org/format/#document-top-level
        /// </summary>
        [JsonPropertyName("data")]
        [JsonPropertyOrder(int.MaxValue)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TData? Data { get; set; }

        /// <summary>
        /// List of errors on this message.  Not included on successful response. https://jsonapi.org/format/#errors
        /// </summary>
        [JsonPropertyName("errors")]
        [JsonPropertyOrder(int.MaxValue-1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Message>? Errors { get; set; }

        /// <summary>
        /// List of links (e.g. to object, log entries, etc.) https://jsonapi.org/format/#document-top-level
        /// </summary>
        [JsonPropertyName("links")]
        [JsonPropertyOrder(int.MaxValue - 2)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Link>? Links { get; set; }

        /// <summary>
        /// Included in each response. https://jsonapi.org/format/#document-meta
        /// </summary>
        [JsonPropertyName("meta")]
        [JsonPropertyOrder(int.MaxValue)]
        public MetaData MetaData { get; set; } = new();

        /// <summary>
        /// Only serialize data when there are no errors. Data and Errors cannot be included in same document.
        /// https://jsonapi.org/format/#document-top-level
        /// </summary>
        public bool ShouldSerializeData() => Data != null && !ShouldSerializeErrors(); // never serialize data when there are errors on the response (https://jsonapi.org/format/#document-top-level)
        public bool ShouldSerializeLinks() => Links is { Count: > 0 };
        public bool ShouldSerializeErrors() => Errors is { Count: > 0 };

        #region Static Factory Methods for Common Scenarios

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        /// <param name="data">The data to include in the response</param>
        /// <returns>A successful API response</returns>
        public static ApiResponse<TData> Success(TData data)
        {
            return new ApiResponse<TData>
            {
                Data = data,
                MetaData = new MetaData
                {
                    CallStatus = CallStatusCode.Ok,
                    CallStatusId = (int)CallStatusCode.Ok
                }
            };
        }

        /// <summary>
        /// Creates a resource not found response (HTTP 422 - Unprocessable Entity)
        /// Use this when a specific resource ID/identifier is not found in the database
        /// </summary>
        /// <param name="resourceType">The type of resource that was not found (e.g., "Resource", "User")</param>
        /// <param name="resourceId">The identifier that was searched for</param>
        /// <param name="additionalDetails">Additional context about the search</param>
        /// <returns>A resource not found API response</returns>
        public static ApiResponse<TData> ResourceNotFound(string resourceType, string resourceId, string? additionalDetails = null)
        {
            var detail = $"{resourceType} with identifier '{resourceId}' was not found.";
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                detail += $" {additionalDetails}";
            }

            return new ApiResponse<TData>
            {
                MetaData = new MetaData
                {
                    CallStatus = CallStatusCode.NotFound,
                    CallStatusId = (int)CallStatusCode.NotFound
                },
                Errors = new List<Message>
                {
                    new Message
                    {
                        CallStatus = CallStatusCode.NotFound,
                        CallStatusId = (int)CallStatusCode.NotFound,
                        Title = $"{resourceType} Not Found",
                        Details = detail,
                        MessageCode = "RESOURCE_NOT_FOUND",
                        SeverityCode = MessageSeverity.Error,
                        SeverityId = (int)MessageSeverity.Error,
                        PropertyLocation = PropertyLocation.Path,
                        PropertyName = resourceId,
                        Tags = new Dictionary<string, string?>
                        {
                            ["ResourceType"] = resourceType,
                            ["ResourceId"] = resourceId,
                            ["ErrorType"] = "ResourceNotFound"
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates a validation error response (HTTP 400 - Bad Request)
        /// </summary>
        /// <param name="fieldName">The field that failed validation</param>
        /// <param name="errorMessage">The validation error message</param>
        /// <returns>A validation error API response</returns>
        public static ApiResponse<TData> ValidationError(string fieldName, string errorMessage)
        {
            return new ApiResponse<TData>
            {
                MetaData = new MetaData
                {
                    CallStatus = CallStatusCode.InvalidArgument,
                    CallStatusId = (int)CallStatusCode.InvalidArgument
                },
                Errors = new List<Message>
                {
                    new Message
                    {
                        CallStatus = CallStatusCode.InvalidArgument,
                        CallStatusId = (int)CallStatusCode.InvalidArgument,
                        Title = "Validation Error",
                        Details = errorMessage,
                        MessageCode = "VALIDATION_ERROR",
                        SeverityCode = MessageSeverity.Error,
                        SeverityId = (int)MessageSeverity.Error,
                        PropertyLocation = PropertyLocation.Body,
                        PropertyName = fieldName,
                        Tags = new Dictionary<string, string?>
                        {
                            ["ErrorType"] = "ValidationError",
                            ["Field"] = fieldName
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates an internal server error response (HTTP 500)
        /// </summary>
        /// <param name="details">Error details (avoid exposing sensitive information)</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>An internal server error API response</returns>
        public static ApiResponse<TData> InternalError(string? details = null, string? correlationId = null)
        {
            return new ApiResponse<TData>
            {
                MetaData = new MetaData
                {
                    CallStatus = CallStatusCode.InternalError,
                    CallStatusId = (int)CallStatusCode.InternalError
                },
                Errors = new List<Message>
                {
                    new Message
                    {
                        CallStatus = CallStatusCode.InternalError,
                        CallStatusId = (int)CallStatusCode.InternalError,
                        Title = "Internal Server Error",
                        Details = details ?? "An unexpected error occurred while processing your request.",
                        MessageCode = "INTERNAL_ERROR",
                        SeverityCode = MessageSeverity.Error,
                        SeverityId = (int)MessageSeverity.Error,
                        Tags = new Dictionary<string, string?>
                        {
                            ["ErrorType"] = "InternalError",
                            ["CorrelationId"] = correlationId
                        }
                    }
                }
            };
        }

        #endregion
    }
}
