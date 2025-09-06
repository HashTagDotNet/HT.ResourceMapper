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
        public TData? Data { get; set; }

        /// <summary>
        /// List of errors on this message.  Not included on successful response. https://jsonapi.org/format/#errors
        /// </summary>
        public List<Message>? Errors { get; set; }

        /// <summary>
        /// List of links (e.g. to object, log entries, etc.) https://jsonapi.org/format/#document-top-level
        /// </summary>
        public List<Link>? Links { get; set; }

        /// <summary>
        /// Included in each response. https://jsonapi.org/format/#document-meta
        /// </summary>
        public MetaData MetaData { get; set; } = new();

        /// <summary>
        /// Only serialize data when there are no errors. Data and Errors cannot be included in same document.
        /// https://jsonapi.org/format/#document-top-level
        /// </summary>
        public bool ShouldSerializeData() => Data != null && !ShouldSerializeErrors(); // never serialize data when there are errors on the response (https://jsonapi.org/format/#document-top-level)
        public bool ShouldSerializeLinks() => Links is { Count: > 0 };
        public bool ShouldSerializeErrors() => Errors is { Count: > 0 };

    }
}


//#region Data Management

///// <summary>
///// Sets data and ensures response is in valid JSON:API state (clears errors if data is set)
///// </summary>
//public ApiResponse<TData> SetData(TData data)
//{
//    Data = data ?? throw new ArgumentNullException(nameof(data));
//    // JSON:API compliance - clear errors when data is present
//    ClearErrors();
//    return this;
//}

///// <summary>
///// Clears the data payload without affecting call status
///// </summary>
//public ApiResponse<TData> ClearData()
//{
//    Data = null;
//    return this;
//}

//#endregion

//#region Error Management with JSON:API Compliance

///// <summary>
///// Adds an error using CallStatusCode approach and clears data to maintain JSON:API compliance
///// </summary>
//public new ApiResponse<TData> AddError(CallStatusCode statusCode, string? detail = null, string? property = null)
//{
//    base.AddError(statusCode, detail, property);
//    // JSON:API compliance - clear data when errors are added
//    ClearData();
//    return this;
//}

///// <summary>
///// Adds an error with custom user and developer action suggestions and clears data
///// </summary>
//public new ApiResponse<TData> AddError(CallStatusCode statusCode, string? detail, string? property, 
//    string? userActions, string? developerActions)
//{
//    base.AddError(statusCode, detail, property, userActions, developerActions);
//    // JSON:API compliance - clear data when errors are added
//    ClearData();
//    return this;
//}

///// <summary>
///// Adds validation error and clears data to maintain JSON:API compliance
///// </summary>
//public new ApiResponse<TData> AddValidationError(string property, string detail)
//{
//    base.AddValidationError(property, detail);
//    // JSON:API compliance - clear data when errors are added
//    ClearData();
//    return this;
//}

///// <summary>
///// Convenience method to add common errors (generic version)
///// </summary>
//public new ApiResponse<TData> AddNotFoundError(string? detail = null, string? property = null)
//{
//    base.AddNotFoundError(detail, property);
//    ClearData();
//    return this;
//}

///// <summary>
///// Convenience method to add permission errors (generic version)
///// </summary>
//public new ApiResponse<TData> AddPermissionError(string? detail = null, string? property = null)
//{
//    base.AddPermissionError(detail, property);
//    ClearData();
//    return this;
//}

///// <summary>
///// Convenience method to add authentication errors (generic version)
///// </summary>
//public new ApiResponse<TData> AddAuthenticationError(string? detail = null)
//{
//    base.AddAuthenticationError(detail);
//    ClearData();
//    return this;
//}

///// <summary>
///// Convenience method to add internal server errors (generic version)
///// </summary>
//public new ApiResponse<TData> AddInternalError(string? detail = null)
//{
//    base.AddInternalError(detail);
//    ClearData();
//    return this;
//}

///// <summary>
///// Convenience method to add cancellation errors (generic version)
///// </summary>
//public new ApiResponse<TData> AddCancellationError(string? detail = null)
//{
//    base.AddCancellationError(detail);
//    ClearData();
//    return this;
//}

//#endregion

//#region Fluent API (Generic Versions)

///// <summary>
///// Fluent API to add an error and return the response (generic version)
///// </summary>
//public new ApiResponse<TData> WithError(CallStatusCode statusCode, string? detail = null, string? property = null)
//{
//    AddError(statusCode, detail, property);
//    return this;
//}

///// <summary>
///// Fluent API to add validation error and return the response (generic version)
///// </summary>
//public new ApiResponse<TData> WithValidationError(string property, string detail)
//{
//    AddValidationError(property, detail);
//    return this;
//}

///// <summary>
///// Fluent API to set data and return the response
///// </summary>
//public ApiResponse<TData> WithData(TData data)
//{
//    SetData(data);
//    return this;
//}

//#endregion

//#region Static Factory Methods

///// <summary>
///// Creates a successful response with data
///// </summary>
//public static ApiResponse<TData> Success(TData data)
//{
//    var response = new ApiResponse<TData>();
//    response.SetData(data);
//    response.MetaData.CallStatus = CallStatusCode.Ok;
//    return response;
//}

///// <summary>
///// Creates an error response (no data)
///// </summary>
//public static ApiResponse<TData> Error(CallStatusCode statusCode, string? detail = null, string? property = null)
//{
//    var response = new ApiResponse<TData>();
//    response.AddError(statusCode, detail, property);
//    return response;
//}

///// <summary>
///// Creates a validation error response
///// </summary>
//public static ApiResponse<TData> ValidationError(string property, string detail)
//{
//    var response = new ApiResponse<TData>();
//    response.AddValidationError(property, detail);
//    return response;
//}

///// <summary>
///// Creates a not found error response
///// </summary>
//public static ApiResponse<TData> NotFound(string? detail = null, string? property = null)
//{
//    var response = new ApiResponse<TData>();
//    response.AddNotFoundError(detail, property);
//    return response;
//}

///// <summary>
///// Creates an internal error response
///// </summary>
//public static ApiResponse<TData> InternalError(string? detail = null)
//{
//    var response = new ApiResponse<TData>();
//    response.AddInternalError(detail);
//    return response;
//}

//#endregion
