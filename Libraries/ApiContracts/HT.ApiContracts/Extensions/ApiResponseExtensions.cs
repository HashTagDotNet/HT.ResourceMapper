using HT.Api.Client.Contracts.Models;
using System.Net;

namespace HT.Api.Client.Contracts.Extensions
{
    /// <summary>
    /// Extension methods for ApiResponse to provide computed properties while maintaining source generation compatibility.
    /// These methods replace the computed properties that were moved from ApiResponse to support System.Text.Json source generation.
    /// </summary>
    public static class ApiResponseExtensions
    {
        /// <summary>
        /// Indicates if the response represents success (no errors or all errors are success codes).
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool IsSuccess(this ApiResponse response) 
            => response.Errors?.All(e => e.IsSuccess) ?? true;

        /// <summary>
        /// Indicates if the response has any failure errors.
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>True if there are failure errors, false otherwise</returns>
        public static bool HasFailures(this ApiResponse response) 
            => response.Errors?.Any(e => e.IsFailure) ?? false;

        /// <summary>
        /// Indicates if the response has any server errors.
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>True if there are server errors, false otherwise</returns>
        public static bool HasServerErrors(this ApiResponse response) 
            => response.Errors?.Any(e => e.IsServerError) ?? false;

        /// <summary>
        /// Indicates if the response has any client errors.
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>True if there are client errors, false otherwise</returns>
        public static bool HasClientErrors(this ApiResponse response) 
            => response.Errors?.Any(e => e.IsClientError) ?? false;

        /// <summary>
        /// Indicates if the response has any cancelled operations.
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>True if there are cancelled operations, false otherwise</returns>
        public static bool HasCancellations(this ApiResponse response) 
            => response.Errors?.Any(e => e.IsCancelled) ?? false;

        /// <summary>
        /// Gets the highest priority error (lowest number = highest priority).
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The highest priority error, or null if no errors exist</returns>
        public static ErrorMessage? GetHighestPriorityError(this ApiResponse response) 
            => response.Errors?.OrderBy(e => e.Priority).FirstOrDefault();

        /// <summary>
        /// Gets the most severe error based on priority and error code.
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The most severe error, or null if no errors exist</returns>
        public static ErrorMessage? GetMostSevereError(this ApiResponse response) 
            => response.Errors?.OrderBy(e => e.Priority).ThenBy(e => e.StatusCode).FirstOrDefault();

        /// <summary>
        /// Gets the primary HTTP status code based on the highest priority error.
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The HTTP status code derived from the highest priority error</returns>
        public static HttpStatusCode GetPrimaryHttpStatusCode(this ApiResponse response) 
            => response.GetHighestPriorityError()?.HttpStatusCode ?? HttpStatusCode.OK;

        /// <summary>
        /// Gets the overall result category for the response.
        /// This method is dynamically calculated on each access and is not cached.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The result category based on the types of errors present</returns>
        public static ResultCategory GetResultCategory(this ApiResponse response)
        {
            if (response.IsSuccess()) return ResultCategory.Success;
            if (response.HasCancellations()) return ResultCategory.Cancelled;
            if (response.HasServerErrors()) return ResultCategory.ServerError;
            if (response.HasClientErrors()) return ResultCategory.ClientError;
            return ResultCategory.Failure;
        }

        /// <summary>
        /// Gets the count of errors in the response.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The total number of errors</returns>
        public static int GetErrorCount(this ApiResponse response) 
            => response.Errors?.Count ?? 0;

        /// <summary>
        /// Gets the count of client errors in the response.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The number of client errors</returns>
        public static int GetClientErrorCount(this ApiResponse response) 
            => response.Errors?.Count(e => e.IsClientError) ?? 0;

        /// <summary>
        /// Gets the count of server errors in the response.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The number of server errors</returns>
        public static int GetServerErrorCount(this ApiResponse response) 
            => response.Errors?.Count(e => e.IsServerError) ?? 0;

        /// <summary>
        /// Gets errors grouped by their error code.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>An array of error groups by error code</returns>
        public static IGrouping<ErrorCodes, ErrorMessage>[] GetErrorGroupsByCode(this ApiResponse response)
            => response.Errors?.GroupBy(e => e.StatusCode).ToArray() ?? Array.Empty<IGrouping<ErrorCodes, ErrorMessage>>();

        /// <summary>
        /// Gets errors that can be retried.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>Enumerable of retryable errors</returns>
        public static IEnumerable<ErrorMessage> GetRetryableErrors(this ApiResponse response)
            => response.Errors?.Where(e => e.IsRetryable) ?? Enumerable.Empty<ErrorMessage>();

        /// <summary>
        /// Gets errors by priority level.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <param name="priority">The priority level to filter by</param>
        /// <returns>Enumerable of errors with the specified priority</returns>
        public static IEnumerable<ErrorMessage> GetErrorsByPriority(this ApiResponse response, int priority)
            => response.Errors?.Where(e => e.Priority == priority) ?? Enumerable.Empty<ErrorMessage>();

        /// <summary>
        /// Gets errors by result category.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <param name="category">The result category to filter by</param>
        /// <returns>Enumerable of errors with the specified category</returns>
        public static IEnumerable<ErrorMessage> GetErrorsByCategory(this ApiResponse response, ResultCategory category)
            => response.Errors?.Where(e => e.ResultCategory == category) ?? Enumerable.Empty<ErrorMessage>();

        /// <summary>
        /// Gets CSS classes for all errors combined (for Blazor UI).
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <param name="separator">Separator between CSS classes</param>
        /// <returns>Combined CSS classes string</returns>
        public static string GetCombinedSeverityClasses(this ApiResponse response, string separator = " ")
        {
            if (response.Errors == null || !response.Errors.Any()) return "alert-success";
            
            var severityClasses = response.Errors.Select(e => e.AlertClass).Distinct();
            return string.Join(separator, severityClasses);
        }

        /// <summary>
        /// Gets the most severe CSS class for display.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The CSS class for the most severe error</returns>
        public static string GetPrimarySeverityClass(this ApiResponse response) 
            => response.GetMostSevereError()?.AlertClass ?? "alert-success";

        /// <summary>
        /// Gets user-friendly error messages for display.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>Enumerable of user-friendly messages</returns>
        public static IEnumerable<string> GetUserFriendlyMessages(this ApiResponse response)
            => response.Errors?.Select(e => e.GetUserActionSuggestion()) ?? Enumerable.Empty<string>();

        /// <summary>
        /// Gets a detailed error summary with error codes.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <param name="separator">Separator between error messages</param>
        /// <returns>Detailed error summary string</returns>
        public static string GetDetailedErrorSummary(this ApiResponse response, string separator = "; ")
        {
            if (response.Errors == null || !response.Errors.Any()) return string.Empty;
            
            return string.Join(separator, response.Errors.Select(e => 
                $"[{e.StatusCode}] {e.Detail ?? e.GetDescription()}"
            ));
        }

        /// <summary>
        /// Gets errors formatted for logging.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>Log-formatted error summary</returns>
        public static string GetLoggingErrorSummary(this ApiResponse response)
        {
            if (response.Errors == null || !response.Errors.Any()) return "No errors";
            
            var errorGroups = response.GetErrorGroupsByCode();
            var groupSummary = string.Join(", ", errorGroups.Select(g => $"{g.Key}({g.Count()})"));
            return $"Errors: {response.GetErrorCount()}, Categories: {groupSummary}";
        }

        /// <summary>
        /// Validates that the response state is consistent.
        /// </summary>
        /// <param name="response">The ApiResponse to validate</param>
        /// <returns>True if the state is valid, false otherwise</returns>
        public static bool IsValidState(this ApiResponse response)
        {
            // If we have errors, IsSuccess should be false
            if (response.Errors?.Any() == true && response.IsSuccess())
                return false;
                
            // If no errors, we should be successful
            if ((response.Errors == null || !response.Errors.Any()) && !response.IsSuccess())
                return false;
                
            return true;
        }

        /// <summary>
        /// Gets validation issues with the response state.
        /// </summary>
        /// <param name="response">The ApiResponse to validate</param>
        /// <returns>Enumerable of validation issue descriptions</returns>
        public static IEnumerable<string> GetStateValidationIssues(this ApiResponse response)
        {
            var issues = new List<string>();
            
            if (response.Errors?.Any() == true && response.IsSuccess())
                issues.Add("Response has errors but IsSuccess() returns true");
                
            if ((response.Errors == null || !response.Errors.Any()) && !response.IsSuccess())
                issues.Add("Response has no errors but IsSuccess() returns false");
                
            return issues;
        }

        /// <summary>
        /// Checks if the response has errors that suggest a retry might succeed.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>True if retry is recommended, false otherwise</returns>
        public static bool IsRetryRecommended(this ApiResponse response)
            => response.GetRetryableErrors().Any();

        /// <summary>
        /// Gets the highest priority error code in the response.
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>The error code of the highest priority error, or Ok if no errors</returns>
        public static ErrorCodes GetHighestPriorityErrorCode(this ApiResponse response)
            => response.GetHighestPriorityError()?.StatusCode ?? ErrorCodes.Ok;

        /// <summary>
        /// Checks if the response contains only warnings (info/warning severity errors).
        /// </summary>
        /// <param name="response">The ApiResponse to check</param>
        /// <returns>True if only warnings are present, false otherwise</returns>
        public static bool HasOnlyWarnings(this ApiResponse response)
        {
            if (response.Errors == null || !response.Errors.Any()) return false;
            
            return response.Errors.All(e => 
                e.StatusCode == ErrorCodes.AlreadyExists || 
                e.StatusCode == ErrorCodes.NotImplemented ||
                e.SeverityClass == "warning" || 
                e.SeverityClass == "info");
        }

        #region ApiResponse<TData> Extensions

        /// <summary>
        /// Indicates if the response has data
        /// </summary>
        /// <param name="response">The ApiResponse&lt;TData&gt; to check</param>
        /// <returns>True if data is present, false otherwise</returns>
        public static bool HasData<TData>(this ApiResponse<TData> response) where TData : class, new()
            => response.Data != null;

        /// <summary>
        /// Gets the data or returns a new instance if data is null (useful for Blazor binding)
        /// </summary>
        /// <param name="response">The ApiResponse&lt;TData&gt; to check</param>
        /// <returns>The data or a new default instance</returns>
        public static TData DataOrDefault<TData>(this ApiResponse<TData> response) where TData : class, new()
            => response.Data ?? new TData();

        /// <summary>
        /// Validates JSON:API compliance - ensures data and errors don't coexist
        /// </summary>
        /// <param name="response">The ApiResponse&lt;TData&gt; to check</param>
        /// <returns>True if compliant, false otherwise</returns>
        public static bool IsJsonApiCompliant<TData>(this ApiResponse<TData> response) where TData : class, new()
            => !(response.HasData() && (response.Errors?.Any() == true));

        /// <summary>
        /// Gets validation issues with JSON:API compliance
        /// </summary>
        /// <param name="response">The ApiResponse&lt;TData&gt; to check</param>
        /// <returns>Enumerable of compliance issue descriptions</returns>
        public static IEnumerable<string> GetComplianceIssues<TData>(this ApiResponse<TData> response) where TData : class, new()
        {
            var issues = new List<string>();
            
            if (response.HasData() && response.Errors?.Any() == true)
            {
                issues.Add("JSON:API violation: Data and Errors cannot coexist in the same response");
            }
            
            return issues;
        }

        /// <summary>
        /// Gets CSS class based on response state (for Blazor UI)
        /// </summary>
        /// <param name="response">The ApiResponse&lt;TData&gt; to check</param>
        /// <returns>CSS class string for UI styling</returns>
        public static string GetResponseStateClass<TData>(this ApiResponse<TData> response) where TData : class, new()
        {
            if (response.HasData()) return "response-success";
            if (response.Errors?.Any(e => e.IsServerError) == true) return "response-server-error";
            if (response.Errors?.Any(e => e.IsClientError) == true) return "response-client-error";
            if (response.Errors?.Any(e => e.IsCancelled) == true) return "response-cancelled";
            return "response-unknown";
        }

        /// <summary>
        /// Gets icon class for response state (Font Awesome compatible)
        /// </summary>
        /// <param name="response">The ApiResponse&lt;TData&gt; to check</param>
        /// <returns>Icon class string for UI display</returns>
        public static string GetResponseStateIcon<TData>(this ApiResponse<TData> response) where TData : class, new()
        {
            if (response.HasData()) return "fa-check-circle";
            if (response.Errors?.Any() == true) return "fa-exclamation-circle";
            return "fa-question-circle";
        }

        #endregion
    }
}