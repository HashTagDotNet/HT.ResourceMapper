using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// A list of error specific status codes that are parallel to HttpStatusCodes but
    /// more useful in describing errors.  (https://grpc.io/docs/guides/status-codes/)
    /// </summary>
   // [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CallStatusCode
    {
        /// <summary>
        /// Result not explicitly set - indicates success
        /// </summary>
        Ok = 0,

        /// <summary>
        /// The execution was cancelled (typically by the OperationCancelledException).
        /// This is not necessarily a failure, but indicates the operation was intentionally stopped.
        /// </summary>
        Cancelled = 1,
        
        /// <summary>
        /// Generic error. For example, this error may be returned when a Status value received from another address space belongs to an
        /// error space that is not known in this address space. Also, errors raised by APIs that do not return enough error information
        /// may be converted to this error
        /// </summary>
        Error = 2 << 2, // 8

        /// <summary>
        /// The client specified an invalid argument. Note that this differs from FAILED_PRECONDITION.
        /// INVALID_ARGUMENT indicates arguments that are problematic regardless of the state of the system (e.g., a malformed file name).
        /// </summary>
        InvalidArgument = Error | 2 << 3, // 8 | 16 = 24

        /// <summary>
        /// The operation timed out before the operation could complete. For operations that change the state of the system, this error may be returned even if the operation has completed successfully. For example, a successful response from a server could have been delayed long enough for the deadline to expire.
        /// </summary>
        OperationTimeOut = Error | 2 << 4, // 8 | 32 = 40

        /// <summary>
        /// Some requested entity (e.g., file or directory) was not found. Note to server developers: if a request is denied for an entire class of users, such as gradual feature rollout or undocumented allowlist, NOT_FOUND may be used. If a request is denied for some users within a class of users, such as user-based access control, PERMISSION_DENIED must be used
        /// </summary>
        NotFound = Error | 2 << 5, // 8 | 64 = 72

        /// <summary>
        /// The request entity (e.g., file or directory) already exists.`
        /// </summary>
        AlreadyExists = Error | 2 << 6, // 8 | 128 = 136

        /// <summary>
        /// The caller does not have permission to execute the specified operation. PERMISSION_DENIED must not be used for rejections caused by exhausting some resource (use RESOURCE_EXHAUSTED instead for those errors). PERMISSION_DENIED must not be used if the caller can not be identified (use UNAUTHENTICATED instead for those errors). This error code does not imply the request is valid or the requested entity exists or satisfies other pre-conditions.
        /// </summary>
        PermissionDenied = Error | 2 << 7, // 8 | 256 = 264

        /// <summary>
        /// Some resource has been exhausted, perhaps a per-user quota, or perhaps the entire file system is out of space.
        /// </summary>
        ResourceExhausted = Error | 2 << 8, // 8 | 512 = 520

        /// <summary>
        /// The operation was rejected because the system is not in a state required for the operation's execution. For example, the directory to be deleted is non-empty, an rmdir operation is applied to a non-directory, etc. Service implementors can use the following guidelines to decide between FAILED_PRECONDITION, ABORTED, and UNAVAILABLE: (a) Use UNAVAILABLE if the client can retry just the failing call. (b) Use ABORTED if the client should retry at a higher level (e.g., when a client-specified test-and-set fails, indicating the client should restart a read-modify-write sequence). (c) Use FAILED_PRECONDITION if the client should not retry until the system state has been explicitly fixed. E.g., if an "rmdir" fails because the directory is non-empty, FAILED_PRECONDITION should be returned since the client should not retry unless the files are deleted from the directory.
        /// </summary>
        FailedPrecondition = Error | 2 << 9, // 8 | 1024 = 1032

        /// <summary>
        /// The operation was aborted, typically due to a concurrency issue such as a sequencer check failure or transaction abort. See the guidelines above for deciding between FAILED_PRECONDITION, ABORTED, and UNAVAILABLE
        /// </summary>
        Aborted = Error | 2 << 10, // 8 | 2048 = 2056

        /// <summary>
        /// The operation was attempted past the valid range. E.g., seeking or reading past end-of-file. Unlike INVALID_ARGUMENT, this error indicates a problem that may be fixed if the system state changes. For example, a 32-bit file system will generate INVALID_ARGUMENT if asked to read at an offset that is not in the range [0,2^32-1], but it will generate OUT_OF_RANGE if asked to read from an offset past the current file size. There is a fair bit of overlap between FAILED_PRECONDITION and OUT_OF_RANGE. We recommend using OUT_OF_RANGE (the more specific error) when it applies so that callers who are iterating through a space can easily look for an OUT_OF_RANGE error to detect when they are done.
        /// </summary>
        OutOfRange = Error | 2 << 11, // 8 | 4096 = 4104

        /// <summary>
        /// The operation is not implemented or is not supported/enabled in this service.
        /// </summary>
        NotImplemented = Error | 2 << 12, // 8 | 8192 = 8200

        /// <summary>
        /// Internal errors. This means that some invariants expected by the underlying system have been broken. This error code is reserved for serious errors.
        /// </summary>
        InternalError = Error | 2 << 13, // 8 | 16384 = 16392

        /// <summary>
        /// The service is currently unavailable. This is most likely a transient condition, which can be corrected by retrying with a backoff. Note that it is not always safe to retry non-idempotent operations.
        /// </summary>
        Unavailable = Error | 2 << 14, // 8 | 32768 = 32776

        /// <summary>
        /// The request lacks valid authentication credentials for the target resource.
        /// </summary>
        Unauthenticated = Error | 2 << 15, // 8 | 65536 = 65544
    }

    /// <summary>
    /// Extension methods for ErrorCodes to determine success/failure and provide additional utilities
    /// </summary>
    public static class ErrorCodesExtensions
    {
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
                CallStatusCode.InternalError => true,
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
        /// Gets the CSS severity class for UI display (Bootstrap compatible)
        /// </summary>
        public static string GetSeverityClass(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => "success",
                CallStatusCode.Cancelled => "secondary",
                CallStatusCode.InvalidArgument => "warning",
                CallStatusCode.NotFound => "warning", 
                CallStatusCode.AlreadyExists => "info",
                CallStatusCode.OutOfRange => "warning",
                CallStatusCode.FailedPrecondition => "warning",
                CallStatusCode.PermissionDenied => "danger",
                CallStatusCode.Unauthenticated => "danger",
                CallStatusCode.ResourceExhausted => "danger",
                CallStatusCode.OperationTimeOut => "warning",
                CallStatusCode.Aborted => "warning",
                CallStatusCode.NotImplemented => "info",
                CallStatusCode.InternalError => "danger",
                CallStatusCode.Unavailable => "danger",
                CallStatusCode.Error => "danger",
                _ => "secondary"
            };
        }

        /// <summary>
        /// Gets the severity level for UI alerts (compatible with both Bootstrap and custom alert systems)
        /// </summary>
        public static string GetAlertClass(this CallStatusCode errorCode)
        {
            return $"alert-{GetSeverityClass(errorCode)}";
        }

        /// <summary>
        /// Gets the appropriate icon name for the error code (Font Awesome compatible)
        /// </summary>
        public static string GetIconClass(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => "fa-check-circle",
                CallStatusCode.Cancelled => "fa-times-circle",
                CallStatusCode.InvalidArgument => "fa-exclamation-triangle",
                CallStatusCode.NotFound => "fa-search",
                CallStatusCode.AlreadyExists => "fa-copy",
                CallStatusCode.PermissionDenied => "fa-lock",
                CallStatusCode.Unauthenticated => "fa-user-slash",
                CallStatusCode.ResourceExhausted => "fa-battery-empty",
                CallStatusCode.FailedPrecondition => "fa-exclamation-triangle",
                CallStatusCode.Aborted => "fa-stop-circle",
                CallStatusCode.OutOfRange => "fa-ruler",
                CallStatusCode.NotImplemented => "fa-wrench",
                CallStatusCode.InternalError => "fa-bug",
                CallStatusCode.Unavailable => "fa-server",
                CallStatusCode.OperationTimeOut => "fa-clock",
                CallStatusCode.Error => "fa-exclamation-circle",
                _ => "fa-question-circle"
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
        /// Converts ErrorCode to appropriate HTTP status code (primary mapping for API responses)
        /// </summary>
        public static System.Net.HttpStatusCode ToHttpStatusCode(this CallStatusCode errorCode)
        {
            return errorCode switch
            {
                CallStatusCode.Ok => System.Net.HttpStatusCode.OK,
                CallStatusCode.Cancelled => System.Net.HttpStatusCode.RequestTimeout,
                CallStatusCode.InvalidArgument => System.Net.HttpStatusCode.BadRequest,
                CallStatusCode.OperationTimeOut => System.Net.HttpStatusCode.RequestTimeout,
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
                _ => ResultCategory.Failure
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

        /// <summary>
        /// Gets the HTTP status code value as integer.
        /// This method provides a convenient way to get the numeric HTTP status code
        /// without having to cast the result of ToHttpStatusCode().
        /// Useful for APIs that need to set HTTP response codes programmatically.
        /// </summary>
        /// <param name="statusCode">The CallStatusCode to convert</param>
        /// <returns>The corresponding HTTP status code as an integer (e.g., 404, 500, etc.)</returns>
        /// <example>
        /// <code>
        /// var httpCode = CallStatusCode.NotFound.GetHttpStatusCodeValue();
        /// // Returns: 404
        /// 
        /// var serverError = CallStatusCode.InternalError.GetHttpStatusCodeValue();
        /// // Returns: 500
        /// 
        /// // Compare with existing method:
        /// var traditional = (int)CallStatusCode.NotFound.ToHttpStatusCode();
        /// var convenient = CallStatusCode.NotFound.GetHttpStatusCodeValue();
        /// // Both return 404, but the second is more readable
        /// </code>
        /// </example>
        public static int GetHttpStatusCodeValue(this CallStatusCode statusCode) 
            => (int)statusCode.ToHttpStatusCode();
    }

    /// <summary>
    /// Categories for result classification in business logic
    /// </summary>
    public enum ResultCategory
    {
        Success,
        Cancelled,
        ClientError,
        ServerError,
        Failure
    }
}
