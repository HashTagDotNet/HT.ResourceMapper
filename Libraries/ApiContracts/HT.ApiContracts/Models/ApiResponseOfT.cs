using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Interfaces;

// ReSharper disable InconsistentNaming

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// Generic API response following JSON:API specification with ErrorCode-first approach.
    /// This class represents a response that can contain either data of type TData OR errors, but never both.
    /// ErrorCodes are the primary status indicators - HTTP status codes are derived automatically.
    /// 
    /// Usage Examples:
    /// <code>
    /// // Success with data
    /// var response = ApiResponse&lt;UserDto&gt;.Success(userData);
    /// 
    /// // Error response
    /// var errorResponse = ApiResponse&lt;UserDto&gt;.Error(ErrorCodes.NotFound, "User not found");
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
    public class ApiResponse<TData> : ApiResponse, IApiResponse<TData>, IApiResponse where TData : class, new()
    {
        /// <summary>
        /// The payload of the response. Data and Errors cannot be included in same document https://jsonapi.org/format/#document-top-level
        /// </summary>
        [JsonPropertyName("data")]
        [JsonPropertyOrder(0)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TData? Data { get; set; }

        #region JSON Serialization Helpers

        /// <summary>
        /// Only serialize data when there are no errors. Data and Errors cannot be included in same document.
        /// https://jsonapi.org/format/#document-top-level
        /// </summary>
        public bool ShouldSerializeData() => Data != null && !ShouldSerializeErrors();

        #endregion

        #region Data Management

        /// <summary>
        /// Sets data and ensures response is in valid JSON:API state (clears errors if data is set)
        /// </summary>
        public ApiResponse<TData> SetData(TData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            // JSON:API compliance - clear errors when data is present
            ClearErrors();
            return this;
        }

        /// <summary>
        /// Clears the data payload
        /// </summary>
        public ApiResponse<TData> ClearData()
        {
            Data = null;
            return this;
        }

        #endregion

        #region Error Management with JSON:API Compliance

        /// <summary>
        /// Adds an error using ErrorCode approach and clears data to maintain JSON:API compliance
        /// </summary>
        public new ApiResponse<TData> AddError(ErrorCodes errorCode, string? detail = null, string? property = null)
        {
            base.AddError(errorCode, detail, property);
            // JSON:API compliance - clear data when errors are added
            ClearData();
            return this;
        }

        /// <summary>
        /// Adds an error with custom user and developer action suggestions and clears data
        /// </summary>
        public new ApiResponse<TData> AddError(ErrorCodes errorCode, string? detail, string? property, 
            string? userActions, string? developerActions)
        {
            base.AddError(errorCode, detail, property, userActions, developerActions);
            // JSON:API compliance - clear data when errors are added
            ClearData();
            return this;
        }

        /// <summary>
        /// Adds validation error and clears data to maintain JSON:API compliance
        /// </summary>
        public new ApiResponse<TData> AddValidationError(string property, string detail)
        {
            base.AddValidationError(property, detail);
            // JSON:API compliance - clear data when errors are added
            ClearData();
            return this;
        }

        #endregion

        #region Fluent API (Generic Versions)

        /// <summary>
        /// Fluent API to add an error and return the response (generic version)
        /// </summary>
        public new ApiResponse<TData> WithError(ErrorCodes errorCode, string? detail = null, string? property = null)
        {
            AddError(errorCode, detail, property);
            return this;
        }

        /// <summary>
        /// Fluent API to add validation error and return the response (generic version)
        /// </summary>
        public new ApiResponse<TData> WithValidationError(string property, string detail)
        {
            AddValidationError(property, detail);
            return this;
        }

        /// <summary>
        /// Fluent API to set data and return the response
        /// </summary>
        public ApiResponse<TData> WithData(TData data)
        {
            SetData(data);
            return this;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        public static ApiResponse<TData> Success(TData data)
        {
            var response = new ApiResponse<TData>();
            response.SetData(data);
            return response;
        }

        /// <summary>
        /// Creates an error response (no data)
        /// </summary>
        public static ApiResponse<TData> Error(ErrorCodes errorCode, string? detail = null, string? property = null)
        {
            var response = new ApiResponse<TData>();
            response.AddError(errorCode, detail, property);
            return response;
        }

        /// <summary>
        /// Creates a validation error response
        /// </summary>
        public static ApiResponse<TData> ValidationError(string property, string detail)
        {
            var response = new ApiResponse<TData>();
            response.AddValidationError(property, detail);
            return response;
        }

        /// <summary>
        /// Creates a not found error response
        /// </summary>
        public static ApiResponse<TData> NotFound(string? detail = null, string? property = null)
        {
            var response = new ApiResponse<TData>();
            response.AddNotFoundError(detail, property);
            return response;
        }

        /// <summary>
        /// Creates an internal error response
        /// </summary>
        public static ApiResponse<TData> InternalError(string? detail = null)
        {
            var response = new ApiResponse<TData>();
            response.AddInternalError(detail);
            return response;
        }

        #endregion
    }
}
