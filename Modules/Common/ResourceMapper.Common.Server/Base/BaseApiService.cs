using System.Net;
using HT.Api.Service.Contracts;
using HT.Microsoft.ILogger.Extensions;
using Microsoft.Extensions.Logging;
using HT.Api.Client.Contracts.Models;

namespace ResourceMapper.Common.Server.Base
{
    /// <summary>
    /// Base class for API services that provides common validation and response patterns
    /// Uses ErrorCode-first approach: developers set ErrorCodes, HTTP status codes are derived
    /// </summary>
    public abstract class BaseApiService
    {
        protected readonly ILogger Logger;

        protected BaseApiService(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Execute an API operation with validation and error handling (ErrorCode-first approach)
        /// </summary>
        protected async Task<ApiServiceResponse<TResponse>> ExecuteApiOperationAsync<TRequest, TResponse>(
            TRequest request,
            Func<TRequest, ValidationResult> validateRequest,
            Func<TRequest, CancellationToken, Task<TResponse>> executeOperation,
            CancellationToken cancellationToken,
            string operationName = "API Operation")
            where TResponse : class, new()
        {
            var response = new ApiServiceResponse<TResponse>();
            try
            {
                var validationResult = validateRequest(request);
                if (!validationResult.IsValid)
                {
                    ApplyValidationErrors(response, validationResult);
                    return response;
                }

                var data = await executeOperation(request, cancellationToken);
                return response.SetDataWithErrorCode(data, ErrorCodes.Ok);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in {operationName}. {ex}");
                return response.SetErrorCodeResponse(ErrorCodes.InternalError, $"Unexpected error in {operationName}");
            }
        }

        /// <summary>
        /// Execute an API operation with validation and error handling (synchronous version)
        /// </summary>
        protected ApiServiceResponse<TResponse> ExecuteApiOperation<TRequest, TResponse>(
            TRequest request,
            Func<TRequest, ValidationResult> validateRequest,
            Func<TRequest, TResponse> executeOperation,
            string operationName = "API Operation")
            where TResponse : class, new()
        {
            var response = new ApiServiceResponse<TResponse>();
            try
            {
                var validationResult = validateRequest(request);
                if (!validationResult.IsValid)
                {
                    ApplyValidationErrors(response, validationResult);
                    return response;
                }

                var data = executeOperation(request);
                return response.SetDataWithErrorCode(data, ErrorCodes.Ok);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in {operationName}. {ex}");
                return response.SetErrorCodeResponse(ErrorCodes.InternalError, $"Unexpected error in {operationName}");
            }
        }

        /// <summary>
        /// Execute an API create operation with validation and error handling
        /// </summary>
        protected async Task<ApiServiceResponse<TResponse>> ExecuteApiCreateOperationAsync<TRequest, TResponse>(
            TRequest request,
            Func<TRequest, ValidationResult> validateRequest,
            Func<TRequest, CancellationToken, Task<TResponse>> executeOperation,
            CancellationToken cancellationToken,
            string operationName = "API Create Operation")
            where TResponse : class, new()
        {
            var response = new ApiServiceResponse<TResponse>();
            try
            {
                var validationResult = validateRequest(request);
                if (!validationResult.IsValid)
                {
                    ApplyValidationErrors(response, validationResult);
                    return response;
                }

                var data = await executeOperation(request, cancellationToken);
                return response.SetDataWithErrorCode(data, ErrorCodes.Ok, "Resource created successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in {operationName}. {ex}");
                return response.SetErrorCodeResponse(ErrorCodes.InternalError, $"Unexpected error in {operationName}");
            }
        }

        /// <summary>
        /// Execute an API operation that doesn't return data (e.g., delete operations)
        /// </summary>
        protected async Task<ApiServiceResponse> ExecuteApiVoidOperationAsync<TRequest>(
            TRequest request,
            Func<TRequest, ValidationResult> validateRequest,
            Func<TRequest, CancellationToken, Task> executeOperation,
            CancellationToken cancellationToken,
            string operationName = "API Operation")
        {
            var response = new ApiServiceResponse();
            try
            {
                var validationResult = validateRequest(request);
                if (!validationResult.IsValid)
                {
                    ApplyValidationErrors(response, validationResult);
                    return response;
                }

                await executeOperation(request, cancellationToken);
                return response.SetErrorCodeResponse(ErrorCodes.Ok, "Operation completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in {operationName}. {ex}");
                return response.SetErrorCodeResponse(ErrorCodes.InternalError, $"Unexpected error in {operationName}");
            }
        }

        /// <summary>
        /// Apply validation errors using ErrorCode-first approach
        /// </summary>
        private static void ApplyValidationErrors(ApiServiceResponse response, ValidationResult validationResult)
        {
            foreach (var error in validationResult.Errors)
            {
                response.ApiResponse.AddValidationError(error.PropertyName, error.ErrorMessage);
            }

            // Set HTTP status code based on the primary error code
            var primaryHttpStatus = response.ApiResponse.PrimaryHttpStatusCode;
            response.SetStatusCode(primaryHttpStatus);

            if (validationResult.Errors.Count == 1)
            {
                response.SetStatusMessage(validationResult.Errors.First().ErrorMessage);
            }
            else
            {
                response.SetStatusMessage($"Validation failed with {validationResult.Errors.Count} errors");
            }
        }
    }

    /// <summary>
    /// Extension methods for ApiServiceResponse to support ErrorCode-first approach
    /// </summary>
    public static class ApiServiceResponseExtensions
    {
        /// <summary>
        /// Sets data and ErrorCode, derives HTTP status code automatically
        /// </summary>
        public static ApiServiceResponse<T> SetDataWithErrorCode<T>(this ApiServiceResponse<T> response, 
            T data, ErrorCodes errorCode, string? message = null) where T : class, new()
        {
            response.Data = data;
            response.ApiResponse.AddError(errorCode, message);
            response.SetStatusCode(errorCode.ToHttpStatusCode(), message);
            return response;
        }

        /// <summary>
        /// Sets ErrorCode and derives HTTP status code automatically
        /// </summary>
        public static ApiServiceResponse SetErrorCodeResponse(this ApiServiceResponse response, 
            ErrorCodes errorCode, string? message = null)
        {
            response.ApiResponse.AddError(errorCode, message);
            response.SetStatusCode(errorCode.ToHttpStatusCode(), message);
            return response;
        }

        /// <summary>
        /// Sets ErrorCode and derives HTTP status code automatically (generic version)
        /// </summary>
        public static ApiServiceResponse<T> SetErrorCodeResponse<T>(this ApiServiceResponse<T> response, 
            ErrorCodes errorCode, string? message = null) where T : class, new()
        {
            response.ApiResponse.AddError(errorCode, message);
            response.SetStatusCode(errorCode.ToHttpStatusCode(), message);
            return response;
        }

        /// <summary>
        /// Adds an error with ErrorCode and updates HTTP status if this error has higher priority
        /// </summary>
        public static ApiServiceResponse AddErrorCode(this ApiServiceResponse response, 
            ErrorCodes errorCode, string? detail = null, string? property = null)
        {
            response.ApiResponse.AddError(errorCode, detail, property);
            
            // Update HTTP status if this error has higher priority than current status
            var newHttpStatus = errorCode.ToHttpStatusCode();
            if (response.HttpStatusCode == null || errorCode.GetPriority() < GetHttpStatusPriority(response.HttpStatusCode.Value))
            {
                response.SetStatusCode(newHttpStatus);
            }
            
            return response;
        }

        /// <summary>
        /// Adds an error with ErrorCode (generic version)
        /// </summary>
        public static ApiServiceResponse<T> AddErrorCode<T>(this ApiServiceResponse<T> response, 
            ErrorCodes errorCode, string? detail = null, string? property = null) where T : class, new()
        {
            ((ApiServiceResponse)response).AddErrorCode(errorCode, detail, property);
            return response;
        }

        private static int GetHttpStatusPriority(HttpStatusCode statusCode)
        {
            return (int)statusCode switch
            {
                >= 500 => 1, // Server errors highest priority
                >= 400 => 2, // Client errors
                >= 300 => 3, // Redirects
                >= 200 => 4, // Success
                _ => 5       // Info
            };
        }
    }

    /// <summary>
    /// Simple validation result class - no external dependencies
    /// </summary>
    public class ValidationResult
    {
        public List<ValidationError> Errors { get; } = new List<ValidationError>();
        public bool IsValid => !Errors.Any();

        public void AddError(string propertyName, string errorMessage)
        {
            Errors.Add(new ValidationError(propertyName, errorMessage));
        }

        public void AddErrors(IEnumerable<ValidationError> errors)
        {
            Errors.AddRange(errors);
        }
    }

    /// <summary>
    /// Represents a validation error for a specific property
    /// </summary>
    public record ValidationError(string PropertyName, string ErrorMessage);

    /// <summary>
    /// Common validation helper methods
    /// </summary>
    public static class ValidationHelpers
    {
        public static ValidationResult ValidateRequired<T>(T? value, string propertyName, string? customMessage = null)
        {
            var result = new ValidationResult();
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                result.AddError(propertyName, customMessage ?? $"{propertyName} is required");
            }
            return result;
        }

        public static ValidationResult ValidateRange(int value, string propertyName, int min, int max, string? customMessage = null)
        {
            var result = new ValidationResult();
            if (value < min || value > max)
            {
                result.AddError(propertyName, customMessage ?? $"{propertyName} must be between {min} and {max}");
            }
            return result;
        }

        public static ValidationResult ValidateStringLength(string? value, string propertyName, int maxLength, int minLength = 0, string? customMessage = null)
        {
            var result = new ValidationResult();
            if (value != null && (value.Length < minLength || value.Length > maxLength))
            {
                result.AddError(propertyName, customMessage ?? $"{propertyName} must be between {minLength} and {maxLength} characters");
            }
            return result;
        }

        public static ValidationResult ValidateEmail(string? email, string propertyName, string? customMessage = null)
        {
            var result = new ValidationResult();
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!email.Contains('@') || !email.Contains('.') || email.IndexOf('@') == 0 || email.LastIndexOf('.') == email.Length - 1)
                {
                    result.AddError(propertyName, customMessage ?? $"{propertyName} must be a valid email address");
                }
            }
            return result;
        }

        public static ValidationResult ValidateUrl(string? url, string propertyName, string? customMessage = null)
        {
            var result = new ValidationResult();
            if (!string.IsNullOrWhiteSpace(url))
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    result.AddError(propertyName, customMessage ?? $"{propertyName} must be a valid URL");
                }
            }
            return result;
        }

        public static ValidationResult Combine(params ValidationResult[] results)
        {
            var combined = new ValidationResult();
            foreach (var result in results)
            {
                combined.AddErrors(result.Errors);
            }
            return combined;
        }
    }
}