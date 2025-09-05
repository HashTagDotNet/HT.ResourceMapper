using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    /// <summary>
    /// Return value from the service that is handling an HTTP request.
    /// </summary>
    public class ApiServiceResponse
    {
        /// <summary>
        /// Raw http response to return to the caller from the API controller. If null, the controller will create a default api response with the headers
        /// </summary>
        public virtual ApiResponse ApiResponse { get; set; } = new();

        public HttpResponse HttpDetails { get; set; }
    }
}

///// <summary>
///// Indicates if the response represents a successful operation (2xx status codes)
///// </summary>
//[JsonIgnore]
//public bool IsSuccess => HttpStatusCode >= System.Net.HttpStatusCode.OK && 
//                        HttpStatusCode < System.Net.HttpStatusCode.MultipleChoices;

///// <summary>
///// Indicates if the response has validation errors
///// </summary>
//[JsonIgnore]
//public bool HasValidationErrors => ApiResponse?.Errors?.Any() == true;

///// <summary>
///// Indicates if the response has any errors (validation or otherwise)
///// </summary>
//[JsonIgnore]
//public bool HasErrors => ApiResponse?.Errors?.Any() == true;

///// <summary>
///// Gets the count of validation errors
///// </summary>
//[JsonIgnore]
//public int ErrorCount => ApiResponse?.Errors?.Count ?? 0;
///// <summary>
///// Sets a successful status (200 OK) with optional message
///// </summary>
//public ApiServiceResponse SetSuccess(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.OK, message);
//}

///// <summary>
///// Sets a created status (201 Created) with optional message
///// </summary>
//public ApiServiceResponse SetCreated(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.Created, message ?? "Resource created successfully");
//}

///// <summary>
///// Sets an accepted status (202 Accepted) with optional message
///// </summary>
//public ApiServiceResponse SetAccepted(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.Accepted, message ?? "Request accepted for processing");
//}

///// <summary>
///// Sets a no content status (204 No Content)
///// </summary>
//public ApiServiceResponse SetNoContent()
//{
//    return SetStatusCode(System.Net.HttpStatusCode.NoContent);
//}

///// <summary>
///// Sets a bad request status (400) with optional message
///// </summary>
//public ApiServiceResponse SetBadRequest(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.BadRequest, message ?? "Bad Request");
//}

///// <summary>
///// Sets an unauthorized status (401) with optional message
///// </summary>
//public ApiServiceResponse SetUnauthorized(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.Unauthorized, message ?? "Unauthorized");
//}

///// <summary>
///// Sets a forbidden status (403) with optional message
///// </summary>
//public ApiServiceResponse SetForbidden(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.Forbidden, message ?? "Forbidden");
//}

///// <summary>
///// Sets a not found status (404) with optional message
///// </summary>
//public ApiServiceResponse SetNotFound(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.NotFound, message ?? "Resource not found");
//}

///// <summary>
///// Sets a conflict status (409) with optional message
///// </summary>
//public ApiServiceResponse SetConflict(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.Conflict, message ?? "Conflict");
//}

///// <summary>
///// Sets an unprocessable entity status (422) with optional message
///// </summary>
//public ApiServiceResponse SetUnprocessableEntity(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.UnprocessableEntity, message ?? "Unprocessable Entity");
//}

///// <summary>
///// Sets an internal server error status (500) with optional message
///// </summary>
//public ApiServiceResponse SetInternalServerError(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.InternalServerError, message ?? "Internal server error");
//}

///// <summary>
///// Sets a service unavailable status (503) with optional message
///// </summary>
//public ApiServiceResponse SetServiceUnavailable(string? message = null)
//{
//    return SetStatusCode(System.Net.HttpStatusCode.ServiceUnavailable, message ?? "Service unavailable");
//}
