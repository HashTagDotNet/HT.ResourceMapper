using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ResourceMapper.UI.Web.Controllers.Api
{
    [ApiController]
    public class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Maps an ApiServiceResponse to an ActionResult based on the HttpResponse status code.
        /// </summary>
        protected ActionResult<ApiResponse<T>> MapServiceResponseToActionResult<T>(ApiServiceResponse<T> serviceResponse)
            where T : class, new()
        {
            var httpStatusCode = serviceResponse.HttpResponse?.HttpStatusCode ?? HttpStatusCode.OK;
            var httpStatusMessage = serviceResponse.HttpResponse?.HttpStatusMessage;

            if (serviceResponse.HttpResponse?.Headers != null)
            {
                foreach (var header in serviceResponse.HttpResponse.Headers)
                {
                    try
                    {
                        if (IsValidHeaderName(header.Key) && IsValidHeaderValue(header.Value) && !IsRestrictedHeader(header.Key))
                            HttpContext.Response.Headers.TryAdd(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to add header {header.Key}: {ex.Message}");
                    }
                }
            }

            var result = StatusCode((int)httpStatusCode, serviceResponse.ApiResponse);

            if (!string.IsNullOrEmpty(httpStatusMessage))
            {
                try
                {
                    HttpContext.Response.HttpContext.Features
                        .Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>()!.ReasonPhrase = httpStatusMessage;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set custom status message: {ex.Message}");
                }
            }

            return result;
        }

        protected static bool IsValidHeaderName(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName)) return false;
            foreach (char c in headerName)
                if (c <= 32 || c >= 127 || "()<>@,;:\\\"/[]?={}".Contains(c)) return false;
            return true;
        }

        protected static bool IsValidHeaderValue(string headerValue)
        {
            if (headerValue == null) return false;
            foreach (char c in headerValue)
            {
                if (c < 32 && c != 9) return false;
                if (c >= 127) return false;
            }
            return true;
        }

        protected static bool IsRestrictedHeader(string headerName)
        {
            var restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Content-Length", "Transfer-Encoding", "Connection", "Date", "Server",
                "Upgrade", "Via", "Warning", "Host", "Content-Type", "Content-Encoding"
            };
            return restrictedHeaders.Contains(headerName);
        }
    }
}
