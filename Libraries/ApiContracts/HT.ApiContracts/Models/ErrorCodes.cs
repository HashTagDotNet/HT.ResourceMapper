using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// A list of error specific status codes that are parallel to HttpStatusCodes but
    /// more useful in describing errors.  (https://grpc.io/docs/guides/status-codes/)
    /// </summary>
   // [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ErrorCodes
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
        public static bool IsSuccess(this ErrorCodes errorCode)
        {
            return errorCode == ErrorCodes.Ok;
        }

        /// <summary>
        /// Determines if the error code indicates a failure (has Error bit flag set, excluding base Error)
        /// </summary>
        public static bool IsFailure(this ErrorCodes errorCode)
        {
            return HasErrorFlag(errorCode) && errorCode != ErrorCodes.Error;
        }

        /// <summary>
        /// Determines if the error code has the Error bit flag set (internal helper)
        /// </summary>
        public static bool HasErrorFlag(this ErrorCodes errorCode)
        {
            return ((int)errorCode & (int)ErrorCodes.Error) == (int)ErrorCodes.Error;
        }

        /// <summary>
        /// Determines if the error code indicates an operation was cancelled
        /// </summary>
        public static bool IsCancelled(this ErrorCodes errorCode)
        {
            return errorCode == ErrorCodes.Cancelled;
        }

        /// <summary>
        /// Gets the specific error bits (excluding the base Error flag)
        /// </summary>
        public static int GetSpecificErrorBits(this ErrorCodes errorCode)
        {
            if (!HasErrorFlag(errorCode)) return 0;
            return (int)errorCode & ~(int)ErrorCodes.Error;
        }

        /// <summary>
        /// Verifies that the bit mask implementation is working correctly
        /// </summary>
        public static bool VerifyBitMask(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => (int)errorCode == 0,
                ErrorCodes.Cancelled => (int)errorCode == 1,
                ErrorCodes.Error => (int)errorCode == 8,
                ErrorCodes.InvalidArgument => (int)errorCode == 24 && HasErrorFlag(errorCode),
                ErrorCodes.OperationTimeOut => (int)errorCode == 40 && HasErrorFlag(errorCode),
                ErrorCodes.NotFound => (int)errorCode == 72 && HasErrorFlag(errorCode),
                ErrorCodes.AlreadyExists => (int)errorCode == 136 && HasErrorFlag(errorCode),
                ErrorCodes.PermissionDenied => (int)errorCode == 264 && HasErrorFlag(errorCode),
                ErrorCodes.ResourceExhausted => (int)errorCode == 520 && HasErrorFlag(errorCode),
                ErrorCodes.FailedPrecondition => (int)errorCode == 1032 && HasErrorFlag(errorCode),
                ErrorCodes.Aborted => (int)errorCode == 2056 && HasErrorFlag(errorCode),
                ErrorCodes.OutOfRange => (int)errorCode == 4104 && HasErrorFlag(errorCode),
                ErrorCodes.NotImplemented => (int)errorCode == 8200 && HasErrorFlag(errorCode),
                ErrorCodes.InternalError => (int)errorCode == 16392 && HasErrorFlag(errorCode),
                ErrorCodes.Unavailable => (int)errorCode == 32776 && HasErrorFlag(errorCode),
                ErrorCodes.Unauthenticated => (int)errorCode == 65544 && HasErrorFlag(errorCode),
                _ => false
            };
        }

        /// <summary>
        /// Gets a detailed breakdown of the error code bit structure
        /// </summary>
        public static string GetBitMaskAnalysis(this ErrorCodes errorCode)
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
        public static bool IsClientError(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.InvalidArgument => true,
                ErrorCodes.NotFound => true,
                ErrorCodes.AlreadyExists => true,
                ErrorCodes.PermissionDenied => true,
                ErrorCodes.Unauthenticated => true,
                ErrorCodes.FailedPrecondition => true,
                ErrorCodes.OutOfRange => true,
                _ => false
            };
        }

        /// <summary>
        /// Determines if the error code indicates a server error (5xx equivalent)
        /// </summary>
        public static bool IsServerError(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.InternalError => true,
                ErrorCodes.NotImplemented => true,
                ErrorCodes.Unavailable => true,
                ErrorCodes.ResourceExhausted => true,
                ErrorCodes.OperationTimeOut => true,
                ErrorCodes.Aborted => true,
                _ => false
            };
        }

        /// <summary>
        /// Determines if the operation can be retried
        /// </summary>
        public static bool IsRetryable(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Unavailable => true,
                ErrorCodes.ResourceExhausted => true,
                ErrorCodes.OperationTimeOut => true,
                ErrorCodes.Aborted => true,
                ErrorCodes.InternalError => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets a user-friendly description of the error code
        /// </summary>
        public static string GetDescription(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => "Operation completed successfully",
                ErrorCodes.Cancelled => "Operation was cancelled",
                ErrorCodes.Error => "An error occurred",
                ErrorCodes.InvalidArgument => "Invalid input provided",
                ErrorCodes.OperationTimeOut => "Operation timed out",
                ErrorCodes.NotFound => "Resource not found",
                ErrorCodes.AlreadyExists => "Resource already exists",
                ErrorCodes.PermissionDenied => "Permission denied",
                ErrorCodes.ResourceExhausted => "Resource limit exceeded",
                ErrorCodes.FailedPrecondition => "Precondition failed",
                ErrorCodes.Aborted => "Operation was aborted",
                ErrorCodes.OutOfRange => "Value out of range",
                ErrorCodes.NotImplemented => "Feature not implemented",
                ErrorCodes.InternalError => "Internal server error",
                ErrorCodes.Unavailable => "Service unavailable",
                ErrorCodes.Unauthenticated => "Authentication required",
                _ => "Unknown error"
            };
        }

        /// <summary>
        /// Gets the CSS severity class for UI display (Bootstrap compatible)
        /// </summary>
        public static string GetSeverityClass(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => "success",
                ErrorCodes.Cancelled => "secondary",
                ErrorCodes.InvalidArgument => "warning",
                ErrorCodes.NotFound => "warning", 
                ErrorCodes.AlreadyExists => "info",
                ErrorCodes.OutOfRange => "warning",
                ErrorCodes.FailedPrecondition => "warning",
                ErrorCodes.PermissionDenied => "danger",
                ErrorCodes.Unauthenticated => "danger",
                ErrorCodes.ResourceExhausted => "danger",
                ErrorCodes.OperationTimeOut => "warning",
                ErrorCodes.Aborted => "warning",
                ErrorCodes.NotImplemented => "info",
                ErrorCodes.InternalError => "danger",
                ErrorCodes.Unavailable => "danger",
                ErrorCodes.Error => "danger",
                _ => "secondary"
            };
        }

        /// <summary>
        /// Gets the severity level for UI alerts (compatible with both Bootstrap and custom alert systems)
        /// </summary>
        public static string GetAlertClass(this ErrorCodes errorCode)
        {
            return $"alert-{GetSeverityClass(errorCode)}";
        }

        /// <summary>
        /// Gets the appropriate icon name for the error code (Font Awesome compatible)
        /// </summary>
        public static string GetIconClass(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => "fa-check-circle",
                ErrorCodes.Cancelled => "fa-times-circle",
                ErrorCodes.InvalidArgument => "fa-exclamation-triangle",
                ErrorCodes.NotFound => "fa-search",
                ErrorCodes.AlreadyExists => "fa-copy",
                ErrorCodes.PermissionDenied => "fa-lock",
                ErrorCodes.Unauthenticated => "fa-user-slash",
                ErrorCodes.ResourceExhausted => "fa-battery-empty",
                ErrorCodes.FailedPrecondition => "fa-exclamation-triangle",
                ErrorCodes.Aborted => "fa-stop-circle",
                ErrorCodes.OutOfRange => "fa-ruler",
                ErrorCodes.NotImplemented => "fa-wrench",
                ErrorCodes.InternalError => "fa-bug",
                ErrorCodes.Unavailable => "fa-server",
                ErrorCodes.OperationTimeOut => "fa-clock",
                ErrorCodes.Error => "fa-exclamation-circle",
                _ => "fa-question-circle"
            };
        }

        /// <summary>
        /// Gets the priority level for error handling (1 = highest, 5 = lowest)
        /// </summary>
        public static int GetPriority(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => 5,
                ErrorCodes.Cancelled => 4,
                ErrorCodes.InternalError => 1,
                ErrorCodes.Unavailable => 1,
                ErrorCodes.Unauthenticated => 2,
                ErrorCodes.PermissionDenied => 2,
                ErrorCodes.ResourceExhausted => 2,
                ErrorCodes.OperationTimeOut => 3,
                ErrorCodes.InvalidArgument => 3,
                ErrorCodes.NotFound => 3,
                ErrorCodes.AlreadyExists => 3,
                ErrorCodes.FailedPrecondition => 3,
                ErrorCodes.OutOfRange => 3,
                ErrorCodes.Aborted => 3,
                ErrorCodes.NotImplemented => 4,
                ErrorCodes.Error => 2,
                _ => 3
            };
        }

        /// <summary>
        /// Converts ErrorCode to appropriate HTTP status code (primary mapping for API responses)
        /// </summary>
        public static System.Net.HttpStatusCode ToHttpStatusCode(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => System.Net.HttpStatusCode.OK,
                ErrorCodes.Cancelled => System.Net.HttpStatusCode.RequestTimeout,
                ErrorCodes.InvalidArgument => System.Net.HttpStatusCode.BadRequest,
                ErrorCodes.OperationTimeOut => System.Net.HttpStatusCode.RequestTimeout,
                ErrorCodes.NotFound => System.Net.HttpStatusCode.NotFound,
                ErrorCodes.AlreadyExists => System.Net.HttpStatusCode.Conflict,
                ErrorCodes.PermissionDenied => System.Net.HttpStatusCode.Forbidden,
                ErrorCodes.ResourceExhausted => System.Net.HttpStatusCode.TooManyRequests,
                ErrorCodes.FailedPrecondition => System.Net.HttpStatusCode.PreconditionFailed,
                ErrorCodes.Aborted => System.Net.HttpStatusCode.Conflict,
                ErrorCodes.OutOfRange => System.Net.HttpStatusCode.BadRequest,
                ErrorCodes.NotImplemented => System.Net.HttpStatusCode.NotImplemented,
                ErrorCodes.InternalError => System.Net.HttpStatusCode.InternalServerError,
                ErrorCodes.Unavailable => System.Net.HttpStatusCode.ServiceUnavailable,
                ErrorCodes.Unauthenticated => System.Net.HttpStatusCode.Unauthorized,
                ErrorCodes.Error => System.Net.HttpStatusCode.InternalServerError,
                _ => System.Net.HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Determines the overall result category for business logic decisions
        /// </summary>
        public static ResultCategory GetResultCategory(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => ResultCategory.Success,
                ErrorCodes.Cancelled => ResultCategory.Cancelled,
                _ when errorCode.IsClientError() => ResultCategory.ClientError,
                _ when errorCode.IsServerError() => ResultCategory.ServerError,
                _ => ResultCategory.Failure
            };
        }

        /// <summary>
        /// Gets user-friendly action suggestions based on the error code
        /// </summary>
        public static string GetUserActionSuggestion(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => "Operation completed successfully.",
                ErrorCodes.Cancelled => "The operation was cancelled. You can try again if needed.",
                ErrorCodes.InvalidArgument => "Please check your input and try again.",
                ErrorCodes.NotFound => "The requested resource could not be found. Please verify the information and try again.",
                ErrorCodes.AlreadyExists => "This resource already exists. Please use a different name or identifier.",
                ErrorCodes.PermissionDenied => "You don't have permission to perform this action. Please contact an administrator.",
                ErrorCodes.Unauthenticated => "Please log in to continue.",
                ErrorCodes.ResourceExhausted => "The service is currently at capacity. Please try again later.",
                ErrorCodes.FailedPrecondition => "The operation cannot be completed due to the current state. Please check the requirements and try again.",
                ErrorCodes.Aborted => "The operation was interrupted. Please try again.",
                ErrorCodes.OutOfRange => "The provided value is outside the acceptable range. Please adjust and try again.",
                ErrorCodes.NotImplemented => "This feature is not yet available. Please check back later.",
                ErrorCodes.InternalError => "An unexpected error occurred. Please try again later or contact support.",
                ErrorCodes.Unavailable => "The service is temporarily unavailable. Please try again in a few moments.",
                ErrorCodes.OperationTimeOut => "The operation took too long to complete. Please try again.",
                ErrorCodes.Error => "An error occurred. Please try again or contact support.",
                _ => "Please try again or contact support if the problem persists."
            };
        }

        /// <summary>
        /// Gets developer-focused action suggestions for troubleshooting
        /// </summary>
        public static string GetDeveloperActionSuggestion(this ErrorCodes errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Ok => "No action required.",
                ErrorCodes.Cancelled => "Check for cancellation token usage and timeout configurations.",
                ErrorCodes.InvalidArgument => "Validate input parameters and request structure.",
                ErrorCodes.NotFound => "Verify resource existence and access patterns.",
                ErrorCodes.AlreadyExists => "Implement proper uniqueness checks and conflict resolution.",
                ErrorCodes.PermissionDenied => "Review authorization policies and user permissions.",
                ErrorCodes.Unauthenticated => "Check authentication middleware and token validation.",
                ErrorCodes.ResourceExhausted => "Review rate limiting, quotas, and scaling configurations.",
                ErrorCodes.FailedPrecondition => "Validate business rules and state requirements.",
                ErrorCodes.Aborted => "Check for concurrency issues and transaction handling.",
                ErrorCodes.OutOfRange => "Validate input ranges and boundary conditions.",
                ErrorCodes.NotImplemented => "Implement the requested functionality.",
                ErrorCodes.InternalError => "Check logs for exceptions and system state.",
                ErrorCodes.Unavailable => "Review service health, dependencies, and infrastructure.",
                ErrorCodes.OperationTimeOut => "Check timeout configurations and async patterns.",
                ErrorCodes.Error => "Review logs and implement more specific error handling.",
                _ => "Investigate the specific error context and implement appropriate handling."
            };
        }
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
