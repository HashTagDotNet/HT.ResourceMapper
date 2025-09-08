using System.Diagnostics;
using HT.Api.Client.Contracts;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    public class ApiServiceResponse<TApiPayload> where TApiPayload : class, new()
    {
        public ApiResponse<TApiPayload> ApiResponse { get; set; } = new();
        public HttpApiResponse? HttpResponse { get; set; }

     
        /// <summary>
        /// Gets the HTTP status code that should be used for this response
        /// </summary>
        /// <returns>The HTTP status code from HttpResponse, or OK if not specified</returns>
        public System.Net.HttpStatusCode GetHttpStatusCode()
        {
            return HttpResponse?.HttpStatusCode ?? System.Net.HttpStatusCode.OK;
        }

        /// <summary>
        /// Determines if this service response represents a successful operation
        /// </summary>
        /// <returns>True if the response is successful, false otherwise</returns>
        public bool IsSuccess()
        {
            // Check if there are any errors in the API response
            if (ApiResponse.Errors?.Count > 0)
                return false;

            // Check if the call status indicates success
            var callStatus = ApiResponse.MetaData?.CallStatus;
            if (callStatus.HasValue && callStatus.Value != CallStatusCode.Ok)
                return false;

            // Check if HTTP status code indicates success (2xx range)
            var httpStatusCode = GetHttpStatusCode();
            return (int)httpStatusCode >= 200 && (int)httpStatusCode < 300;
        }
    }
}
