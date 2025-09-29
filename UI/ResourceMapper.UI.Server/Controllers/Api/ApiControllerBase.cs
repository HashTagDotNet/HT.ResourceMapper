using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ResourceMapper.UI.Server.Controllers.Api
{
    [ApiController]
    public class ApiControllerBase:ControllerBase
    {

        /// <summary>
        /// Maps an ApiServiceResponse to an ActionResult based on the HttpResponse status code
        /// </summary>
        /// <typeparam name="T">The type of the response data</typeparam>
        /// <param name="serviceResponse">The service response to map</param>
        /// <returns>An ActionResult with the appropriate HTTP status code and ApiResponse payload</returns>
        protected ActionResult<ApiResponse<T>> MapServiceResponseToActionResult<T>(ApiServiceResponse<T> serviceResponse)
            where T : class, new()
        {
            // Get the HTTP status code from the service response
            var httpStatusCode = serviceResponse.HttpResponse?.HttpStatusCode ?? HttpStatusCode.OK;

            // Get the HTTP status message if provided
            var httpStatusMessage = serviceResponse.HttpResponse?.HttpStatusMessage;

            // Add any custom headers from the service response with proper validation and error handling
            if (serviceResponse.HttpResponse?.Headers != null)
            {
                foreach (var header in serviceResponse.HttpResponse.Headers)
                {
                    try
                    {
                        // Validate header name and value
                        if (IsValidHeaderName(header.Key) && IsValidHeaderValue(header.Value))
                        {
                            // Check if this is a restricted header that ASP.NET Core manages
                            if (!IsRestrictedHeader(header.Key))
                            {
                                // Use TryAdd to avoid duplicates and conflicts
                                HttpContext.Response.Headers.TryAdd(header.Key, header.Value);
                            }
                            else
                            {
                                // Log warning about restricted header (if logging is available)
                                // For now, we'll silently skip restricted headers
                                System.Diagnostics.Debug.WriteLine($"Skipping restricted header: {header.Key}");
                            }
                        }
                        else
                        {
                            // Log warning about invalid header format
                            System.Diagnostics.Debug.WriteLine($"Skipping invalid header: {header.Key}={header.Value}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception but don't fail the entire request
                        System.Diagnostics.Debug.WriteLine($"Failed to add header {header.Key}: {ex.Message}");
                        // In production, you might want to use proper logging here
                        // _logger?.LogWarning(ex, "Failed to add custom header {HeaderName}", header.Key);
                    }
                }
            }

            // Create the ActionResult with the appropriate status code and ApiResponse payload
            var result = StatusCode((int)httpStatusCode, serviceResponse.ApiResponse);

            // Set custom status message if provided
            if (!string.IsNullOrEmpty(httpStatusMessage))
            {
                try
                {
                    // Set the custom reason phrase for the HTTP response
                    HttpContext.Response.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>()!.ReasonPhrase = httpStatusMessage;
                }
                catch (Exception ex)
                {
                    // Log the exception but don't fail the request
                    System.Diagnostics.Debug.WriteLine($"Failed to set custom status message: {ex.Message}");
                    // In production: _logger?.LogWarning(ex, "Failed to set custom HTTP status message");
                }
            }

            return result;
        }

        /// <summary>
        /// Validates if a header name is valid according to HTTP specifications
        /// </summary>
        /// <param name="headerName">The header name to validate</param>
        /// <returns>True if the header name is valid</returns>
        protected static bool IsValidHeaderName(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                return false;

            // HTTP header names can only contain token characters (RFC 7230)
            // Token characters: VCHAR except separators
            foreach (char c in headerName)
            {
                if (c <= 32 || c >= 127 || "()<>@,;:\\\"/[]?={}".Contains(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates if a header value is valid according to HTTP specifications
        /// </summary>
        /// <param name="headerValue">The header value to validate</param>
        /// <returns>True if the header value is valid</returns>
        protected static bool IsValidHeaderValue(string headerValue)
        {
            if (headerValue == null)
                return false;

            // HTTP header values can contain VCHAR, WSP, and obs-text (RFC 7230)
            // We'll be conservative and allow printable ASCII + space/tab
            foreach (char c in headerValue)
            {
                if (c < 32 && c != 9) // Allow tab (9) but not other control characters
                    return false;
                if (c >= 127)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a header is restricted and managed by ASP.NET Core
        /// </summary>
        /// <param name="headerName">The header name to check</param>
        /// <returns>True if the header is restricted</returns>
        protected static bool IsRestrictedHeader(string headerName)
        {
            // Headers that ASP.NET Core manages automatically and shouldn't be set manually
            var restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Content-Length",
            "Transfer-Encoding",
            "Connection",
            "Date",
            "Server",
            "Upgrade",
            "Via",
            "Warning",
            "Host",
            "Content-Type", // Usually set by formatters
            "Content-Encoding" // Usually set by compression middleware
        };

            return restrictedHeaders.Contains(headerName);
        }

    }
}
