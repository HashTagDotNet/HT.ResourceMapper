using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Interfaces;

namespace HT.Api.Client.Contracts.Models
{
    public partial class ApiResponse : IApiResponse 
    {
        /// <summary>
        /// List of errors on this message.  Not included on successful response. https://jsonapi.org/format/#errors
        /// </summary>
        [JsonPropertyOrder(int.MinValue)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ErrorMessage>? Errors { get; set; }
        public bool ShouldSerializeErrors() => Errors is { Count: > 0 };

        /// <summary>
        /// List of links (e.g. to object, log entries, etc.) https://jsonapi.org/format/#document-top-level
        /// </summary>
        [JsonPropertyOrder(int.MaxValue-1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Link>? Links { get; set; }
        public bool ShouldSerializeLinks() => Links is { Count: > 0 };

        /// <summary>
        /// Included in each response. https://jsonapi.org/format/#document-meta
        /// </summary>
        [JsonPropertyName("meta")]
        [JsonPropertyOrder(int.MaxValue)]
        public MetaData MetaData { get; set; } = new MetaData();

        /// <summary>
        /// Indicates if the response represents success (no errors or all errors are success codes)
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => Errors?.All(e => e.IsSuccess) ?? true;

        /// <summary>
        /// Indicates if the response has any failure errors
        /// </summary>
        [JsonIgnore]
        public bool HasFailures => Errors?.Any(e => e.IsFailure) ?? false;

        /// <summary>
        /// Indicates if the response has any server errors
        /// </summary>
        [JsonIgnore]
        public bool HasServerErrors => Errors?.Any(e => e.IsServerError) ?? false;

        /// <summary>
        /// Indicates if the response has any client errors
        /// </summary>
        [JsonIgnore]
        public bool HasClientErrors => Errors?.Any(e => e.IsClientError) ?? false;

        /// <summary>
        /// Gets the highest priority error (lowest number = highest priority)
        /// </summary>
        [JsonIgnore]
        public ErrorMessage? HighestPriorityError => Errors?.OrderBy(e => e.Priority).FirstOrDefault();

        /// <summary>
        /// Gets the primary HTTP status code based on the highest priority error
        /// </summary>
        [JsonIgnore]
        public System.Net.HttpStatusCode PrimaryHttpStatusCode => 
            HighestPriorityError?.HttpStatusCode ?? System.Net.HttpStatusCode.OK;

        /// <summary>
        /// Gets the overall result category for the response
        /// </summary>
        [JsonIgnore]
        public ResultCategory ResultCategory
        {
            get
            {
                if (IsSuccess) return ResultCategory.Success;
                if (HasServerErrors) return ResultCategory.ServerError;
                if (HasClientErrors) return ResultCategory.ClientError;
                return ResultCategory.Failure;
            }
        }

        /// <summary>
        /// Adds a validation error using ErrorCode approach (developer sets ErrorCode, HTTP status derived)
        /// </summary>
        public void AddValidation(string propertyName, string message, ErrorCodes errorCode = ErrorCodes.InvalidArgument)
        {
            Errors ??= new List<ErrorMessage>();
            var errorMessage = new ErrorMessage
            {
                Property = propertyName,
                Detail = message,
                StatusCode = errorCode
            };
            Errors.Add(errorMessage);
        }

        /// <summary>
        /// Adds an error using ErrorCode approach
        /// </summary>
        public void AddError(ErrorCodes errorCode, string? detail = null, string? property = null)
        {
            Errors ??= new List<ErrorMessage>();
            Errors.Add(ErrorMessage.Create(errorCode, detail, property));
        }

        /// <summary>
        /// Adds an error with custom user and developer action suggestions
        /// </summary>
        public void AddError(ErrorCodes errorCode, string? detail, string? property, 
            string? userActions, string? developerActions)
        {
            Errors ??= new List<ErrorMessage>();
            var error = ErrorMessage.Create(errorCode, detail, property);
            error.SuggestedUserActions = userActions;
            error.SuggestedApplicationActions = developerActions;
            Errors.Add(error);
        }

        /// <summary>
        /// Convenience method to add common errors
        /// </summary>
        public void AddNotFoundError(string? detail = null, string? property = null)
        {
            AddError(ErrorCodes.NotFound, detail, property);
        }

        /// <summary>
        /// Convenience method to add validation errors
        /// </summary>
        public void AddValidationError(string property, string detail)
        {
            AddError(ErrorCodes.InvalidArgument, detail, property);
        }

        /// <summary>
        /// Convenience method to add permission errors
        /// </summary>
        public void AddPermissionError(string? detail = null, string? property = null)
        {
            AddError(ErrorCodes.PermissionDenied, detail, property);
        }

        /// <summary>
        /// Convenience method to add authentication errors
        /// </summary>
        public void AddAuthenticationError(string? detail = null)
        {
            AddError(ErrorCodes.Unauthenticated, detail);
        }

        /// <summary>
        /// Convenience method to add internal server errors
        /// </summary>
        public void AddInternalError(string? detail = null)
        {
            AddError(ErrorCodes.InternalError, detail);
        }

        /// <summary>
        /// Gets all errors for a specific property
        /// </summary>
        public IEnumerable<ErrorMessage> GetErrorsForProperty(string propertyName)
        {
            return Errors?.Where(e => string.Equals(e.Property, propertyName, StringComparison.OrdinalIgnoreCase)) 
                   ?? Enumerable.Empty<ErrorMessage>();
        }

        /// <summary>
        /// Gets all errors of a specific ErrorCode type
        /// </summary>
        public IEnumerable<ErrorMessage> GetErrorsByCode(ErrorCodes errorCode)
        {
            return Errors?.Where(e => e.StatusCode == errorCode) ?? Enumerable.Empty<ErrorMessage>();
        }

        /// <summary>
        /// Checks if there are any errors of a specific type
        /// </summary>
        public bool HasErrorCode(ErrorCodes errorCode)
        {
            return Errors?.Any(e => e.StatusCode == errorCode) ?? false;
        }

        /// <summary>
        /// Clears all errors
        /// </summary>
        public void ClearErrors()
        {
            Errors?.Clear();
        }

        /// <summary>
        /// Gets a summary of all error messages
        /// </summary>
        public string GetErrorSummary(string separator = "; ")
        {
            if (Errors == null || !Errors.Any()) return string.Empty;
            return string.Join(separator, Errors.Select(e => e.Detail ?? e.GetDescription()));
        }
    }
}

