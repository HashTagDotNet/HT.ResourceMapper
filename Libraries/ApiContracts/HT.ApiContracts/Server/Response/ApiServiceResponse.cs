using System.Net;
using System.Text.Json.Serialization;
using HT.Api.Contracts.Client.Interfaces;
using HT.Api.Contracts.Client.Models;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace HT.Api.Contracts.Server.Response
{
    /// <summary>
    /// Response from the service layer to the controller layer.  Contains
    /// information the controller layer can use to format the response.
    /// </summary>
    public class ApiServiceResponse
    {
        /// <summary>
        /// Headers the response serializer should add to the returning response
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public List<KeyValuePair<string, string>>? Headers { get; set; }
        
        public HttpStatusCode? HttpStatusCode { get; set; }

        /// <summary>
        /// If not null, the response builder should provide this text instead
        /// of standard HTTP text
        /// </summary>
        public string? HttpStatusMessage { get; set; }

        /// <summary>
        /// The entirety of the message to be returned to the caller
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual IApiResponse? ApiResponse { get; set; }

        public ApiServiceResponse AddHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            Headers ??= [];
            Headers.Add(new KeyValuePair<string, string>(key,value));
            return this;
        }

        /// <summary>
        /// Append a comma-delimited value to a header.  Used for multivalued headers.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ApiServiceResponse AppendHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            Headers ??= [];
            var existingHeader = Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existingHeader.Key != null && !string.IsNullOrWhiteSpace(key))
            {
                Headers.Remove(existingHeader);
                Headers.Add(new KeyValuePair<string, string>(key, existingHeader.Value + "," + value));
            }
            else
            {
                Headers.Add(new KeyValuePair<string, string>(key, value));
            }
            return this;
        }
        
        public ApiServiceResponse SetHttpStatus(HttpStatusCode statusCode, string? statusMessage=null)
        {
            HttpStatusCode = statusCode;
            HttpStatusMessage = statusMessage;
            return this;
        }

        public ApiServiceResponse SetHttpStatusMessage(string? statusMessage)
        {
            HttpStatusMessage = statusMessage;
            return this;
        }

        public ApiServiceResponse AddMessage(string code, string text)
        {

            GetApiResponse.MetaData.Messages ??= new List<Message>();
            GetApiResponse.MetaData.Messages.Add(new Message()
            {
               MessageCode = code,
               Detail = text
            });
            return this;
        }

        public ApiServiceResponse ForError(Action<ErrorMessage> errorBuilder)

        {
            if (errorBuilder == null) return this;

            var response = GetApiResponse;
            var errorMessage = new ErrorMessage();
            response.Errors ??= new List<ErrorMessage>();
            errorBuilder.Invoke(errorMessage);
            response.Errors.Add(errorMessage);

            return this;
        }
        public ErrorMessageBuilder WithError
        {
            get
            {
                var response = GetApiResponse;
                var errorMessage = new ErrorMessage();
                response.Errors ??= new List<ErrorMessage>();
                response.Errors.Add(errorMessage);
                var builder = new ErrorMessageBuilder(errorMessage, response);
                return builder;
            }
        }

        protected virtual IApiResponse GetApiResponse
        {
            get
            {
                if (ApiResponse != null) return ApiResponse;
                ApiResponse = new ApiResponse();
                return ApiResponse;
            }
        }
  
        public virtual ApiServiceResponse AsInvalidArgument(string field, string? message)
        {
            SetHttpStatus(System.Net.HttpStatusCode.BadRequest);
            IApiResponse apiResponse = GetApiResponse;
            apiResponse.Errors ??= new List<ErrorMessage>();
            var errorMessage = new ErrorMessage()
            {
                //SourceCode = RequestLocation.Body,
                Property = field,
                Detail = message,
                StatusCode = ErrorCodes.InvalidArgument
            };
            apiResponse.Errors.Add(errorMessage);
            return this;
        }

        public ApiServiceResponse AsNotFound(string? messageDetail)
        {
            SetHttpStatus(System.Net.HttpStatusCode.NotFound);
            IApiResponse apiResponse = GetApiResponse;
            apiResponse.Errors ??= new List<ErrorMessage>();
            var errorMessage = new ErrorMessage()
            {
                //SourceCode = RequestLocation.Body,
                Detail = messageDetail,
                StatusCode = ErrorCodes.InvalidArgument
            };
            apiResponse.Errors.Add(errorMessage);
            return this;
        }

        public bool IsOk
        {
            get
            {
                var response = GetApiResponse;
                if (response?.Errors is { Count: > 0 }) return false;
                return true;
            }
        }
        public ApiServiceResponse AsConflict(string? messageDetail)
        {
            SetHttpStatus(System.Net.HttpStatusCode.Conflict);
            IApiResponse apiResponse = GetApiResponse;
            apiResponse.Errors ??= new List<ErrorMessage>();
            var errorMessage = new ErrorMessage()
            {
                //SourceCode = RequestLocation.Body,
                Detail = messageDetail,
                StatusCode = ErrorCodes.InvalidArgument
            };
            apiResponse.Errors.Add(errorMessage);
            return this;
        }
        public ApiServiceResponse AsOk(string? statusMessage = null)
        {
            SetHttpStatus(System.Net.HttpStatusCode.OK,statusMessage);
            
            return this;
        }
        public void AddValidation(string propertyName, string message)
        {
            var apiResponse = GetApiResponse as ApiResponse;
            apiResponse.AddValidation(propertyName, message);
        }
    }
}
