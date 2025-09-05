using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Interfaces;

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// Base API response following JSON:API specification with CallStatusCode-first approach.
    /// CallStatusCodes are the primary status indicators - HTTP status codes are derived automatically.
    /// Use AddError() methods with CallStatusCodes rather than setting HTTP status directly.
    /// This design is based on the JSON:API specification (https://jsonapi.org/) with some customizations.
    /// 
    /// NOTE: Computed properties have been moved to ApiResponseExtensions for source generation compatibility.
    /// Use extension methods like response.IsSuccess() instead of response.IsSuccess property.
    /// </summary>
    public partial class ApiResponse : IApiResponse 
    {
        /// <summary>
        /// List of errors on this message.  Not included on successful response. https://jsonapi.org/format/#errors
        /// </summary>
        [JsonPropertyOrder(int.MinValue)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ErrorMessage>? Errors { get; set; }

        /// <summary>
        /// List of links (e.g. to object, log entries, etc.) https://jsonapi.org/format/#document-top-level
        /// </summary>
        [JsonPropertyOrder(int.MaxValue-1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Link>? Links { get; set; }

        /// <summary>
        /// Included in each response. https://jsonapi.org/format/#document-meta
        /// </summary>
        [JsonPropertyName("meta")]
        [JsonPropertyOrder(int.MaxValue)]
        public MetaData MetaData { get; set; } = new MetaData();

        #region Error Management Methods

        /// <summary>
        /// Adds a validation error using CallStatusCode approach (developer sets CallStatusCode, HTTP status derived)
        /// </summary>
        public ApiResponse AddValidation(string propertyName, string message, CallStatusCode statusCode = CallStatusCode.InvalidArgument)
        {
            Errors ??= new List<ErrorMessage>();
            var errorMessage = new ErrorMessage
            {
                Property = propertyName,
                Detail = message,
                CallStatus = statusCode
            };
            Errors.Add(errorMessage);
            UpdateCallStatus();
            return this;
        }

        /// <summary>
        /// Adds an error using CallStatusCode approach
        /// </summary>
        public ApiResponse AddError(CallStatusCode statusCode, string? detail = null, string? property = null)
        {
            Errors ??= new List<ErrorMessage>();
            Errors.Add(ErrorMessage.Create(statusCode, detail, property));
            UpdateCallStatus();
            return this;
        }

        /// <summary>
        /// Adds an error with custom user and developer action suggestions
        /// </summary>
        public ApiResponse AddError(CallStatusCode statusCode, string? detail, string? property, 
            string? userActions, string? developerActions)
        {
            Errors ??= new List<ErrorMessage>();
            var error = ErrorMessage.Create(statusCode, detail, property);
            error.SuggestedUserActions = userActions;
            error.SuggestedApplicationActions = developerActions;
            Errors.Add(error);
            UpdateCallStatus();
            return this;
        }

        #endregion

        #region Convenience Error Methods

        /// <summary>
        /// Convenience method to add common errors
        /// </summary>
        public ApiResponse AddNotFoundError(string? detail = null, string? property = null)
        {
            AddError(CallStatusCode.NotFound, detail, property);
            return this;
        }

        /// <summary>
        /// Convenience method to add validation errors
        /// </summary>
        public ApiResponse AddValidationError(string property, string detail)
        {
            AddError(CallStatusCode.InvalidArgument, detail, property);
            return this;
        }

        /// <summary>
        /// Convenience method to add permission errors
        /// </summary>
        public ApiResponse AddPermissionError(string? detail = null, string? property = null)
        {
            AddError(CallStatusCode.PermissionDenied, detail, property);
            return this;
        }

        /// <summary>
        /// Convenience method to add authentication errors
        /// </summary>
        public ApiResponse AddAuthenticationError(string? detail = null)
        {
            AddError(CallStatusCode.Unauthenticated, detail);
            return this;
        }

        /// <summary>
        /// Convenience method to add internal server errors
        /// </summary>
        public ApiResponse AddInternalError(string? detail = null)
        {
            AddError(CallStatusCode.InternalError, detail);
            return this;
        }

        /// <summary>
        /// Convenience method to add cancellation errors
        /// </summary>
        public ApiResponse AddCancellationError(string? detail = null)
        {
            AddError(CallStatusCode.Cancelled, detail);
            return this;
        }

        #endregion

        #region Error Management

        /// <summary>
        /// Clears all errors and resets call status to success
        /// </summary>
        public ApiResponse ClearErrors()
        {
            Errors?.Clear();
            UpdateCallStatus();
            return this;
        }

        /// <summary>
        /// Updates the CallStatus in MetaData based on current errors state.
        /// This ensures MetaData.CallStatus is the authoritative source of truth.
        /// </summary>
        protected void UpdateCallStatus()
        {
            if (Errors == null || !Errors.Any())
            {
                MetaData.CallStatus = CallStatusCode.Ok;
            }
            else
            {
                // Set to the highest priority (most severe) error
                var highestPriorityError = Errors.OrderBy(e => e.Priority).FirstOrDefault();
                MetaData.CallStatus = highestPriorityError?.CallStatus ?? CallStatusCode.Ok;
            }
        }

        /// <summary>
        /// Explicitly sets the call status. Use with caution - prefer using AddError methods.
        /// </summary>
        public ApiResponse SetCallStatus(CallStatusCode statusCode)
        {
            MetaData.CallStatus = statusCode;
            return this;
        }

        #endregion

        #region Bulk Error Operations

        /// <summary>
        /// Adds multiple validation errors for a property
        /// </summary>
        public ApiResponse AddValidationErrors(string propertyName, IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                AddValidationError(propertyName, message);
            }
            return this;
        }

        /// <summary>
        /// Adds validation errors from a dictionary
        /// </summary>
        public ApiResponse AddValidationErrors(Dictionary<string, List<string>> validationErrors)
        {
            foreach (var kvp in validationErrors)
            {
                foreach (var error in kvp.Value)
                {
                    AddValidationError(kvp.Key, error);
                }
            }
            return this;
        }

        #endregion

        #region JSON Serialization Helpers

        /// <summary>
        /// Determines whether to serialize the Errors property
        /// </summary>
        public bool ShouldSerializeErrors() => Errors is { Count: > 0 };

        /// <summary>
        /// Determines whether to serialize the Links property
        /// </summary>
        public bool ShouldSerializeLinks() => Links is { Count: > 0 };

        #endregion

        #region Error Query Methods

        /// <summary>
        /// Gets all errors for a specific property
        /// </summary>
        public IEnumerable<ErrorMessage> GetErrorsForProperty(string propertyName)
        {
            return Errors?.Where(e => string.Equals(e.Property, propertyName, StringComparison.OrdinalIgnoreCase)) 
                   ?? Enumerable.Empty<ErrorMessage>();
        }

        /// <summary>
        /// Gets all errors of a specific CallStatusCode type
        /// </summary>
        public IEnumerable<ErrorMessage> GetErrorsByCode(CallStatusCode statusCode)
        {
            return Errors?.Where(e => e.CallStatus == statusCode) ?? Enumerable.Empty<ErrorMessage>();
        }

        /// <summary>
        /// Checks if there are any errors of a specific type
        /// </summary>
        public bool HasErrorCode(CallStatusCode statusCode)
        {
            return Errors?.Any(e => e.CallStatus == statusCode) ?? false;
        }

        /// <summary>
        /// Gets a summary of all error messages
        /// </summary>
        public string GetErrorSummary(string separator = "; ")
        {
            if (Errors == null || !Errors.Any()) return string.Empty;
            return string.Join(separator, Errors.Select(e => e.Detail ?? e.GetDescription()));
        }

        #endregion

        #region Fluent API

        /// <summary>
        /// Fluent API to add an error and return the response
        /// </summary>
        public ApiResponse WithError(CallStatusCode statusCode, string? detail = null, string? property = null)
        {
            AddError(statusCode, detail, property);
            return this;
        }

        /// <summary>
        /// Fluent API to add validation error and return the response
        /// </summary>
        public ApiResponse WithValidationError(string property, string detail)
        {
            AddValidationError(property, detail);
            return this;
        }

        #endregion
    }
}

