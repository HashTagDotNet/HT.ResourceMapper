using HT.Api.Client.Contracts.Models;

namespace HT.Api.Client.Contracts
{
    public static class CallStatusExtensions
    {
        public static bool IsError(this CallStatusCode? statusCode) =>
            statusCode != null && (statusCode & CallStatusCode.Error) == CallStatusCode.Error;

        /// <summary>
        /// Converts ErrorCode to appropriate HTTP status code (primary mapping for API responses)
        /// </summary>
        public static System.Net.HttpStatusCode ToHttpStatusCode(this CallStatusCode? errorCode)
        {
            if (errorCode == null) return System.Net.HttpStatusCode.OK;

            return errorCode switch
            {
                CallStatusCode.Ok => System.Net.HttpStatusCode.OK,
                CallStatusCode.Cancelled => System.Net.HttpStatusCode.RequestTimeout,
                CallStatusCode.InvalidArgument => System.Net.HttpStatusCode.BadRequest,
                CallStatusCode.OperationTimeOut => System.Net.HttpStatusCode.RequestTimeout,
                
                // IMPORTANT: Resource not found maps to 422 Unprocessable Entity
                // This differentiates from ASP.NET Core's automatic 404 for unknown endpoints
                CallStatusCode.NotFound => System.Net.HttpStatusCode.UnprocessableEntity, 
                
                CallStatusCode.AlreadyExists => System.Net.HttpStatusCode.Conflict,
                CallStatusCode.PermissionDenied => System.Net.HttpStatusCode.Forbidden,
                CallStatusCode.ResourceExhausted => System.Net.HttpStatusCode.TooManyRequests,
                CallStatusCode.FailedPrecondition => System.Net.HttpStatusCode.PreconditionFailed,
                CallStatusCode.Aborted => System.Net.HttpStatusCode.Conflict,
                CallStatusCode.OutOfRange => System.Net.HttpStatusCode.BadRequest,
                CallStatusCode.NotImplemented => System.Net.HttpStatusCode.NotImplemented,
                CallStatusCode.InternalError => System.Net.HttpStatusCode.InternalServerError,
                CallStatusCode.Unavailable => System.Net.HttpStatusCode.ServiceUnavailable,
                CallStatusCode.Unauthenticated => System.Net.HttpStatusCode.Unauthorized,
                CallStatusCode.Error => System.Net.HttpStatusCode.InternalServerError,

                _ => System.Net.HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Converts ErrorCode to HTTP status code with option to use traditional 404 for resource not found
        /// Use this method when you need backward compatibility or client expectations for 404
        /// </summary>
        public static System.Net.HttpStatusCode ToHttpStatusCodeTraditional(this CallStatusCode? errorCode)
        {
            if (errorCode == null) return System.Net.HttpStatusCode.OK;

            return errorCode switch
            {
                CallStatusCode.Ok => System.Net.HttpStatusCode.OK,
                CallStatusCode.Cancelled => System.Net.HttpStatusCode.RequestTimeout,
                CallStatusCode.InvalidArgument => System.Net.HttpStatusCode.BadRequest,
                CallStatusCode.OperationTimeOut => System.Net.HttpStatusCode.RequestTimeout,
                
                // Traditional approach: both endpoint and resource not found use 404
                CallStatusCode.NotFound => System.Net.HttpStatusCode.NotFound,
                
                CallStatusCode.AlreadyExists => System.Net.HttpStatusCode.Conflict,
                CallStatusCode.PermissionDenied => System.Net.HttpStatusCode.Forbidden,
                CallStatusCode.ResourceExhausted => System.Net.HttpStatusCode.TooManyRequests,
                CallStatusCode.FailedPrecondition => System.Net.HttpStatusCode.PreconditionFailed,
                CallStatusCode.Aborted => System.Net.HttpStatusCode.Conflict,
                CallStatusCode.OutOfRange => System.Net.HttpStatusCode.BadRequest,
                CallStatusCode.NotImplemented => System.Net.HttpStatusCode.NotImplemented,
                CallStatusCode.InternalError => System.Net.HttpStatusCode.InternalServerError,
                CallStatusCode.Unavailable => System.Net.HttpStatusCode.ServiceUnavailable,
                CallStatusCode.Unauthenticated => System.Net.HttpStatusCode.Unauthorized,
                CallStatusCode.Error => System.Net.HttpStatusCode.InternalServerError,

                _ => System.Net.HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Determines if the error code indicates a successful operation
        /// </summary>
        public static bool IsSuccess(this CallStatusCode errorCode)
        {
            return errorCode == CallStatusCode.Ok;
        }

        /// <summary>
        /// Determines if the error code indicates a failure (has Error bit flag set, excluding base Error)
        /// </summary>
        public static bool IsFailure(this CallStatusCode errorCode)
        {
            return HasErrorFlag(errorCode) && errorCode != CallStatusCode.Error;
        }

        /// <summary>
        /// Determines if the error code has the Error bit flag set (internal helper)
        /// </summary>
        public static bool HasErrorFlag(this CallStatusCode errorCode)
        {
            return ((int)errorCode & (int)CallStatusCode.Error) == (int)CallStatusCode.Error;
        }

        /// <summary>
        /// Determines if the error code indicates an operation was cancelled
        /// </summary>
        public static bool IsCancelled(this CallStatusCode errorCode)
        {
            return errorCode == CallStatusCode.Cancelled;
        }

        /// <summary>
        /// Gets the specific error bits (excluding the base Error flag)
        /// </summary>
        public static int GetSpecificErrorBits(this CallStatusCode errorCode)
        {
            if (!HasErrorFlag(errorCode)) return 0;
            return (int)errorCode & ~(int)CallStatusCode.Error;
        }

        /// <summary>
        /// Verifies that the bit mask implementation is working correctly
        /// </summary>
        public static bool VerifyBitMask(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => (int)errorCode == 0,
                CallStatusCode.Cancelled => (int)errorCode == 1,
                CallStatusCode.Error => (int)errorCode == 8,
                CallStatusCode.InvalidArgument => (int)errorCode == 24 && HasErrorFlag(errorCode),
                CallStatusCode.OperationTimeOut => (int)errorCode == 40 && HasErrorFlag(errorCode),
                CallStatusCode.NotFound => (int)errorCode == 72 && HasErrorFlag(errorCode),
                CallStatusCode.AlreadyExists => (int)errorCode == 136 && HasErrorFlag(errorCode),
                CallStatusCode.PermissionDenied => (int)errorCode == 264 && HasErrorFlag(errorCode),
                CallStatusCode.ResourceExhausted => (int)errorCode == 520 && HasErrorFlag(errorCode),
                CallStatusCode.FailedPrecondition => (int)errorCode == 1032 && HasErrorFlag(errorCode),
                CallStatusCode.Aborted => (int)errorCode == 2056 && HasErrorFlag(errorCode),
                CallStatusCode.OutOfRange => (int)errorCode == 4104 && HasErrorFlag(errorCode),
                CallStatusCode.NotImplemented => (int)errorCode == 8200 && HasErrorFlag(errorCode),
                CallStatusCode.InternalError => (int)errorCode == 16392 && HasErrorFlag(errorCode),
                CallStatusCode.Unavailable => (int)errorCode == 32776 && HasErrorFlag(errorCode),
                CallStatusCode.Unauthenticated => (int)errorCode == 65544 && HasErrorFlag(errorCode),
                _ => false
            };
        }

        /// <summary>
        /// Gets a detailed breakdown of the error code bit structure
        /// </summary>
        public static string GetBitMaskAnalysis(this CallStatusCode errorCode)
        {
            var value = (int)errorCode;
            var binary = Convert.ToString(value, 2).PadLeft(16, '0');
            var hasErrorFlag = HasErrorFlag(errorCode);
            var specificBits = GetSpecificErrorBits(errorCode);

            return $"ErrorCode: {errorCode} | Value: {value} | Binary: {binary} | HasErrorFlag: {hasErrorFlag} | SpecificBits: {specificBits}";
        }

        /// <summary>
        /// Determines if the error code indicates a client error (4xx equivalent)
        /// </summary>
        public static bool IsClientError(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.InvalidArgument => true,
                CallStatusCode.NotFound => true,
                CallStatusCode.AlreadyExists => true,
                CallStatusCode.PermissionDenied => true,
                CallStatusCode.Unauthenticated => true,
                CallStatusCode.FailedPrecondition => true,
                CallStatusCode.OutOfRange => true,
                _ => false
            };
        }

        /// <summary>
        /// Determines if the error code indicates a server error (5xx equivalent)
        /// </summary>
        public static bool IsServerError(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.InternalError => true,
                CallStatusCode.NotImplemented => true,
                CallStatusCode.Unavailable => true,
                CallStatusCode.ResourceExhausted => true,
                CallStatusCode.OperationTimeOut => true,
                CallStatusCode.Aborted => true,
                _ => false
            };
        }

        /// <summary>
        /// Determines if the operation can be retried
        /// </summary>
        public static bool IsRetryable(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Unavailable => true,
                CallStatusCode.ResourceExhausted => true,
                CallStatusCode.OperationTimeOut => true,
                CallStatusCode.Aborted => true,
              //  CallStatusCode.InternalError => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets a user-friendly description of the error code
        /// </summary>
        public static string GetDescription(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => "Operation completed successfully",
                CallStatusCode.Cancelled => "Operation was cancelled",
                CallStatusCode.Error => "An error occurred",
                CallStatusCode.InvalidArgument => "Invalid input provided",
                CallStatusCode.OperationTimeOut => "Operation timed out",
                CallStatusCode.NotFound => "Resource not found",
                CallStatusCode.AlreadyExists => "Resource already exists",
                CallStatusCode.PermissionDenied => "Permission denied",
                CallStatusCode.ResourceExhausted => "Resource limit exceeded",
                CallStatusCode.FailedPrecondition => "Precondition failed",
                CallStatusCode.Aborted => "Operation was aborted",
                CallStatusCode.OutOfRange => "Value out of range",
                CallStatusCode.NotImplemented => "Feature not implemented",
                CallStatusCode.InternalError => "Internal server error",
                CallStatusCode.Unavailable => "Service unavailable",
                CallStatusCode.Unauthenticated => "Authentication required",
                _ => "Unknown error"
            };
        }

        /// <summary>
        /// Gets the priority level for error handling (1 = highest, 5 = lowest)
        /// </summary>
        public static int GetPriority(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => 5,
                CallStatusCode.Cancelled => 4,
                CallStatusCode.InternalError => 1,
                CallStatusCode.Unavailable => 1,
                CallStatusCode.Unauthenticated => 2,
                CallStatusCode.PermissionDenied => 2,
                CallStatusCode.ResourceExhausted => 2,
                CallStatusCode.OperationTimeOut => 3,
                CallStatusCode.InvalidArgument => 3,
                CallStatusCode.NotFound => 3,
                CallStatusCode.AlreadyExists => 3,
                CallStatusCode.FailedPrecondition => 3,
                CallStatusCode.OutOfRange => 3,
                CallStatusCode.Aborted => 3,
                CallStatusCode.NotImplemented => 4,
                CallStatusCode.Error => 2,
                _ => 3
            };
        }

        /// <summary>
        /// Determines the overall result category for business logic decisions
        /// </summary>
        public static ResultCategory GetResultCategory(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => ResultCategory.Success,
                CallStatusCode.Cancelled => ResultCategory.Cancelled,
                _ when errorCode.IsClientError() => ResultCategory.ClientError,
                _ when errorCode.IsServerError() => ResultCategory.ServerError,
                _ => ResultCategory.GeneralFailure
            };
        }
       
        /// <summary>
        /// Gets user-friendly action suggestions based on the error code
        /// </summary>
        public static string GetUserActionSuggestion(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => "Operation completed successfully.",
                CallStatusCode.Cancelled => "The operation was cancelled. You can try again if needed.",
                CallStatusCode.InvalidArgument => "Please check your input and try again.",
                CallStatusCode.NotFound => "The requested resource could not be found. Please verify the information and try again.",
                CallStatusCode.AlreadyExists => "This resource already exists. Please use a different name or identifier.",
                CallStatusCode.PermissionDenied => "You don't have permission to perform this action. Please contact an administrator.",
                CallStatusCode.Unauthenticated => "Please log in to continue.",
                CallStatusCode.ResourceExhausted => "The service is currently at capacity. Please try again later.",
                CallStatusCode.FailedPrecondition => "The operation cannot be completed due to the current state. Please check the requirements and try again.",
                CallStatusCode.Aborted => "The operation was interrupted. Please try again.",
                CallStatusCode.OutOfRange => "The provided value is outside the acceptable range. Please adjust and try again.",
                CallStatusCode.NotImplemented => "This feature is not yet available. Please check back later.",
                CallStatusCode.InternalError => "An unexpected error occurred. Please try again later or contact support.",
                CallStatusCode.Unavailable => "The service is temporarily unavailable. Please try again in a few moments.",
                CallStatusCode.OperationTimeOut => "The operation took too long to complete. Please try again.",
                CallStatusCode.Error => "An error occurred. Please try again or contact support.",
                _ => "Please try again or contact support if the problem persists."
            };
        }

        /// <summary>
        /// Gets developer-focused action suggestions for troubleshooting
        /// </summary>
        public static string GetDeveloperActionSuggestion(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => "No action required.",
                CallStatusCode.Cancelled => "Check for cancellation token usage and timeout configurations.",
                CallStatusCode.InvalidArgument => "Validate input parameters and request structure.",
                CallStatusCode.NotFound => "Verify resource existence and access patterns.",
                CallStatusCode.AlreadyExists => "Implement proper uniqueness checks and conflict resolution.",
                CallStatusCode.PermissionDenied => "Review authorization policies and user permissions.",
                CallStatusCode.Unauthenticated => "Check authentication middleware and token validation.",
                CallStatusCode.ResourceExhausted => "Review rate limiting, quotas, and scaling configurations.",
                CallStatusCode.FailedPrecondition => "Validate business rules and state requirements.",
                CallStatusCode.Aborted => "Check for concurrency issues and transaction handling.",
                CallStatusCode.OutOfRange => "Validate input ranges and boundary conditions.",
                CallStatusCode.NotImplemented => "Implement the requested functionality.",
                CallStatusCode.InternalError => "Check logs for exceptions and system state.",
                CallStatusCode.Unavailable => "Review service health, dependencies, and infrastructure.",
                CallStatusCode.OperationTimeOut => "Check timeout configurations and async patterns.",
                CallStatusCode.Error => "Review logs and implement more specific error handling.",
                _ => "Investigate the specific error context and implement appropriate handling."
            };
        }

        /// <summary>
        /// Gets a user-friendly message for common scenarios.
        /// This method provides human-readable messages that can be displayed directly to end users.
        /// Particularly useful for client applications (Blazor, mobile, etc.) that need to show 
        /// meaningful error messages without additional translation logic.
        /// </summary>
        /// <param name="statusCode">The CallStatusCode to get a user message for</param>
        /// <returns>A user-friendly string that describes the status in simple terms</returns>
        /// <example>
        /// <code>
        /// var message = CallStatusCode.InvalidArgument.GetUserMessage();
        /// // Returns: "Please check your input and try again"
        /// 
        /// var success = CallStatusCode.Ok.GetUserMessage();
        /// // Returns: "Success" (note: no exclamation mark)
        /// </code>
        /// </example>
        public static string GetUserMessage(this CallStatusCode statusCode) => statusCode switch
        {
            CallStatusCode.Ok => "Success",
            CallStatusCode.Cancelled => "Operation was cancelled",
            CallStatusCode.InvalidArgument => "Please check your input and try again",
            CallStatusCode.NotFound => "The requested item was not found",
            CallStatusCode.AlreadyExists => "This item already exists",
            CallStatusCode.PermissionDenied => "You don't have permission to perform this action",
            CallStatusCode.Unauthenticated => "Please log in to continue",
            CallStatusCode.ResourceExhausted => "Resource limit exceeded. Please try again later",
            CallStatusCode.FailedPrecondition => "Operation cannot be completed due to current conditions",
            CallStatusCode.Aborted => "Operation was interrupted. Please try again",
            CallStatusCode.OutOfRange => "The provided value is outside the acceptable range",
            CallStatusCode.NotImplemented => "This feature is not yet available",
            CallStatusCode.InternalError => "Something went wrong. Please try again later",
            CallStatusCode.Unavailable => "Service is temporarily unavailable",
            CallStatusCode.OperationTimeOut => "The operation took too long to complete",
            CallStatusCode.Error => "An error occurred",
            _ => "Please try again or contact support if the problem persists"
        };
    }
}
