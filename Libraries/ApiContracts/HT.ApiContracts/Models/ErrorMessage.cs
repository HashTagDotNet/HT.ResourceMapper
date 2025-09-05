using System.Dynamic;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// https://jsonapi.org/format/#errors
    /// </summary>
    public class ErrorMessage : MessageBase
    {
        /// <summary>
        /// <inheritdoc cref="Models.CallStatusCode"/>
        /// </summary>
        public int CallStatusId { get; set; }
            

        /// <summary>
        /// Indicates if this error represents a success state
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsSuccess => CallStatus.IsSuccess();

        /// <summary>
        /// Indicates if this error represents a failure state
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsFailure => CallStatus.IsFailure();

        /// <summary>
        /// Indicates if this error represents a cancelled operation
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsCancelled => CallStatus.IsCancelled();

        /// <summary>
        /// Indicates if this is a client error (4xx equivalent)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsClientError => CallStatus.IsClientError();

        /// <summary>
        /// Indicates if this is a server error (5xx equivalent)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsServerError => CallStatus.IsServerError();

        /// <summary>
        /// Indicates if the operation can be retried
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsRetryable => CallStatus.IsRetryable();

        /// <summary>
        /// Gets the corresponding HTTP status code (derived from ErrorCode)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public System.Net.HttpStatusCode? HttpStatusCode { get; set; }

        /// <summary>
        /// Practical actions that the developer of application consuming the API could take in order to resolve the error condition.  May be localized to callers language, resource code, or other content. 
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SuggestedApplicationActions { get; set; }

        /// <summary>
        /// Practical actions that a user of the application consuming the API could take in order to resolve the error condition.  May be localized to callers language, resource code, or other content.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SuggestedUserActions { get; set; }

        /// <summary>
        /// Gets a user-friendly description of the error
        /// </summary>
        public string GetDescription()
        {
            return CallStatus.GetDescription();
        }

        /// <summary>
        /// Gets user-friendly action suggestions (auto-generated from ErrorCode if SuggestedUserActions is null)
        /// </summary>
        public string GetUserActionSuggestion()
        {
            return SuggestedUserActions ?? CallStatus.GetUserActionSuggestion();
        }

        /// <summary>
        /// Gets developer action suggestions (auto-generated from ErrorCode if SuggestedApplicationActions is null)
        /// </summary>
        public string GetDeveloperActionSuggestion()
        {
            return SuggestedApplicationActions ?? CallStatus.GetDeveloperActionSuggestion();
        }

        /// <summary>
        /// Creates an ErrorMessage with a specific error code
        /// </summary>
        public static ErrorMessage Create(CallStatusCode errorCode, string? detail = null, string? property = null)
        {
            return new ErrorMessage
            {
                CallStatus = errorCode,
                Detail = detail ?? errorCode.GetDescription(),
                Property = property
            };
        }

        /// <summary>
        /// Creates a success ErrorMessage
        /// </summary>
        public static ErrorMessage CreateSuccess(string? detail = null)
        {
            return Create(CallStatusCode.Ok, detail);
        }

        /// <summary>
        /// Creates an invalid argument ErrorMessage for validation failures
        /// </summary>
        public static ErrorMessage CreateInvalidArgument(string property, string detail)
        {
            return Create(CallStatusCode.InvalidArgument, detail, property);
        }

        /// <summary>
        /// Creates a not found ErrorMessage
        /// </summary>
        public static ErrorMessage CreateNotFound(string? detail = null, string? property = null)
        {
            return Create(CallStatusCode.NotFound, detail, property);
        }

        /// <summary>
        /// Creates an internal error ErrorMessage
        /// </summary>
        public static ErrorMessage CreateInternalError(string? detail = null)
        {
            return Create(CallStatusCode.InternalError, detail);
        }

        /// <summary>
        /// Creates an unauthenticated ErrorMessage
        /// </summary>
        public static ErrorMessage CreateUnauthenticated(string? detail = null)
        {
            return Create(CallStatusCode.Unauthenticated, detail);
        }

        /// <summary>
        /// Creates a permission denied ErrorMessage
        /// </summary>
        public static ErrorMessage CreatePermissionDenied(string? detail = null, string? property = null)
        {
            return Create(CallStatusCode.PermissionDenied, detail, property);
        }

        /// <summary>
        /// Creates an already exists ErrorMessage
        /// </summary>
        public static ErrorMessage CreateAlreadyExists(string? detail = null, string? property = null)
        {
            return Create(CallStatusCode.AlreadyExists, detail, property);
        }

        /// <summary>
        /// Creates a timeout ErrorMessage
        /// </summary>
        public static ErrorMessage CreateTimeout(string? detail = null)
        {
            return Create(CallStatusCode.OperationTimeOut, detail);
        }

        /// <summary>
        /// Creates a service unavailable ErrorMessage
        /// </summary>
        public static ErrorMessage CreateUnavailable(string? detail = null)
        {
            return Create(CallStatusCode.Unavailable, detail);
        }
    }
}
