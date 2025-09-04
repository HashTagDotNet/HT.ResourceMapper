using System.Net;
using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    public class ApiServiceResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public List<KeyValuePair<string, string>>? Headers { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public HttpStatusCode? HttpStatusCode { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HttpStatusMessage { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual ApiResponse ApiResponse { get; set; } = new();

        /// <summary>
        /// Indicates if the response represents a successful operation (2xx status codes)
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => HttpStatusCode >= System.Net.HttpStatusCode.OK && 
                                HttpStatusCode < System.Net.HttpStatusCode.MultipleChoices;

        /// <summary>
        /// Indicates if the response has validation errors
        /// </summary>
        [JsonIgnore]
        public bool HasValidationErrors => ApiResponse?.Errors?.Any() == true;

        /// <summary>
        /// Indicates if the response has any errors (validation or otherwise)
        /// </summary>
        [JsonIgnore]
        public bool HasErrors => ApiResponse?.Errors?.Any() == true;

        /// <summary>
        /// Gets the count of validation errors
        /// </summary>
        [JsonIgnore]
        public int ErrorCount => ApiResponse?.Errors?.Count ?? 0;

        public ApiServiceResponse AddHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            
            Headers ??= [];
            Headers.Add(new KeyValuePair<string, string>(key, value));
            return this;
        }

        public ApiServiceResponse AppendHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            Headers ??= [];
            var existingHeader = Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existingHeader.Key != null)
            {
                Headers.Remove(existingHeader);
                Headers.Add(new KeyValuePair<string, string>(key, $"{existingHeader.Value},{value}"));
            }
            else
            {
                Headers.Add(new KeyValuePair<string, string>(key, value));
            }
            return this;
        }

        /// <summary>
        /// Removes a header by key (case-insensitive)
        /// </summary>
        public ApiServiceResponse RemoveHeader(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            
            if (Headers != null)
            {
                var headerToRemove = Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (headerToRemove.Key != null)
                {
                    Headers.Remove(headerToRemove);
                }
            }
            return this;
        }

        /// <summary>
        /// Gets a header value by key (case-insensitive)
        /// </summary>
        public string? GetHeaderValue(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return Headers?.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>
        /// Checks if a header exists by key (case-insensitive)
        /// </summary>
        public bool HasHeader(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return Headers?.Any(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) == true;
        }
        
        public ApiServiceResponse SetStatusCode(HttpStatusCode statusCode, string? statusMessage = null)
        {
            HttpStatusCode = statusCode;
            HttpStatusMessage = statusMessage;
            return this;
        }

        public ApiServiceResponse SetStatusMessage(string? statusMessage)
        {
            HttpStatusMessage = statusMessage;
            return this;
        }

        /// <summary>
        /// Sets a successful status (200 OK) with optional message
        /// </summary>
        public ApiServiceResponse SetSuccess(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.OK, message);
        }

        /// <summary>
        /// Sets a created status (201 Created) with optional message
        /// </summary>
        public ApiServiceResponse SetCreated(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.Created, message ?? "Resource created successfully");
        }

        /// <summary>
        /// Sets an accepted status (202 Accepted) with optional message
        /// </summary>
        public ApiServiceResponse SetAccepted(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.Accepted, message ?? "Request accepted for processing");
        }

        /// <summary>
        /// Sets a no content status (204 No Content)
        /// </summary>
        public ApiServiceResponse SetNoContent()
        {
            return SetStatusCode(System.Net.HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Sets a bad request status (400) with optional message
        /// </summary>
        public ApiServiceResponse SetBadRequest(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.BadRequest, message ?? "Bad Request");
        }

        /// <summary>
        /// Sets an unauthorized status (401) with optional message
        /// </summary>
        public ApiServiceResponse SetUnauthorized(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.Unauthorized, message ?? "Unauthorized");
        }

        /// <summary>
        /// Sets a forbidden status (403) with optional message
        /// </summary>
        public ApiServiceResponse SetForbidden(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.Forbidden, message ?? "Forbidden");
        }

        /// <summary>
        /// Sets a not found status (404) with optional message
        /// </summary>
        public ApiServiceResponse SetNotFound(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.NotFound, message ?? "Resource not found");
        }

        /// <summary>
        /// Sets a conflict status (409) with optional message
        /// </summary>
        public ApiServiceResponse SetConflict(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.Conflict, message ?? "Conflict");
        }

        /// <summary>
        /// Sets an unprocessable entity status (422) with optional message
        /// </summary>
        public ApiServiceResponse SetUnprocessableEntity(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.UnprocessableEntity, message ?? "Unprocessable Entity");
        }

        /// <summary>
        /// Sets an internal server error status (500) with optional message
        /// </summary>
        public ApiServiceResponse SetInternalServerError(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.InternalServerError, message ?? "Internal server error");
        }

        /// <summary>
        /// Sets a service unavailable status (503) with optional message
        /// </summary>
        public ApiServiceResponse SetServiceUnavailable(string? message = null)
        {
            return SetStatusCode(System.Net.HttpStatusCode.ServiceUnavailable, message ?? "Service unavailable");
        }

        public ApiServiceResponse AddMessage(string code, string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            
            ApiResponse.MetaData.Messages ??= new List<Message>();
            ApiResponse.MetaData.Messages.Add(new Message()
            {
                MessageCode = code,
                Detail = text
            });
            return this;
        }

        /// <summary>
        /// Adds an informational message
        /// </summary>
        public ApiServiceResponse AddInfoMessage(string text, string? code = null)
        {
            return AddMessage(code ?? "INFO", text);
        }

        /// <summary>
        /// Adds a warning message
        /// </summary>
        public ApiServiceResponse AddWarningMessage(string text, string? code = null)
        {
            return AddMessage(code ?? "WARNING", text);
        }

        /// <summary>
        /// Adds an error message
        /// </summary>
        public ApiServiceResponse AddErrorMessage(string text, string? code = null)
        {
            return AddMessage(code ?? "ERROR", text);
        }

        /// <summary>
        /// Gets all validation error messages as a single string
        /// </summary>
        public string GetValidationErrorSummary(string separator = "; ")
        {
            if (ApiResponse?.Errors == null || !ApiResponse.Errors.Any())
                return string.Empty;
                
            return string.Join(separator, ApiResponse.Errors.Select(e => e.Detail ?? "Unknown error"));
        }

        /// <summary>
        /// Gets all error messages for a specific property
        /// </summary>
        public IEnumerable<string> GetErrorsForProperty(string propertyName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            
            return ApiResponse?.Errors?
                .Where(e => string.Equals(e.Property, propertyName, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Detail ?? "Unknown error") ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets the first error message for a specific property
        /// </summary>
        public string? GetFirstErrorForProperty(string propertyName)
        {
            return GetErrorsForProperty(propertyName).FirstOrDefault();
        }

        /// <summary>
        /// Checks if there are validation errors for a specific property
        /// </summary>
        public bool HasErrorsForProperty(string propertyName)
        {
            return GetErrorsForProperty(propertyName).Any();
        }

        /// <summary>
        /// Clears all validation errors
        /// </summary>
        public ApiServiceResponse ClearErrors()
        {
            if (ApiResponse?.Errors != null)
            {
                ApiResponse.Errors.Clear();
            }
            return this;
        }

        /// <summary>
        /// Clears all messages
        /// </summary>
        public ApiServiceResponse ClearMessages()
        {
            if (ApiResponse?.MetaData?.Messages != null)
            {
                ApiResponse.MetaData.Messages.Clear();
            }
            return this;
        }

        /// <summary>
        /// Resets the response to a clean state
        /// </summary>
        public ApiServiceResponse Reset()
        {
            Headers?.Clear();
            HttpStatusCode = null;
            HttpStatusMessage = null;
            ClearErrors();
            ClearMessages();
            return this;
        }
    }
}
