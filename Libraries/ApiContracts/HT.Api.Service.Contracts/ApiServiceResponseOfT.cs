using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    public class ApiServiceResponse<TApiPayload> : ApiServiceResponse where TApiPayload : class, new()
    {
        private ApiResponse<TApiPayload>? _apiResponse;

        public new ApiResponse<TApiPayload>? ApiResponse
        {
            get
            {
                if (_apiResponse != null) return _apiResponse;
                _apiResponse = new ApiResponse<TApiPayload>();
                return _apiResponse;
            }
            set => _apiResponse = value;
        }

        [JsonIgnore]
        public TApiPayload? Data
        {
            get => ApiResponse?.Data;
            set
            {
                if (ApiResponse != null) 
                    ApiResponse.Data = value;
            }
        }

        /// <summary>
        /// Indicates if the response has data
        /// </summary>
        [JsonIgnore]
        public bool HasData => Data != null;

        /// <summary>
        /// Sets the data and marks the response as successful (200 OK)
        /// </summary>
        public ApiServiceResponse<TApiPayload> SetData(TApiPayload data, string? message = null)
        {
            Data = data;
            SetSuccess(message);
            return this;
        }

        /// <summary>
        /// Sets the data with a created status (201 Created)
        /// </summary>
        public ApiServiceResponse<TApiPayload> SetCreatedData(TApiPayload data, string? message = null)
        {
            Data = data;
            SetCreated(message);
            return this;
        }

        /// <summary>
        /// Clears the data
        /// </summary>
        public new ApiServiceResponse<TApiPayload> ClearData()
        {
            Data = null;
            return this;
        }

        /// <summary>
        /// Resets the response and clears data
        /// </summary>
        public new ApiServiceResponse<TApiPayload> Reset()
        {
            base.Reset();
            ClearData();
            return this;
        }

        // Override fluent methods to return the correct type
        public new ApiServiceResponse<TApiPayload> AddHeader(string key, string value)
        {
            base.AddHeader(key, value);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> AppendHeader(string key, string value)
        {
            base.AppendHeader(key, value);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> RemoveHeader(string key)
        {
            base.RemoveHeader(key);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetStatusCode(System.Net.HttpStatusCode statusCode, string? statusMessage = null)
        {
            base.SetStatusCode(statusCode, statusMessage);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetStatusMessage(string? statusMessage)
        {
            base.SetStatusMessage(statusMessage);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetSuccess(string? message = null)
        {
            base.SetSuccess(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetCreated(string? message = null)
        {
            base.SetCreated(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetAccepted(string? message = null)
        {
            base.SetAccepted(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetNoContent()
        {
            base.SetNoContent();
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetBadRequest(string? message = null)
        {
            base.SetBadRequest(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetUnauthorized(string? message = null)
        {
            base.SetUnauthorized(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetForbidden(string? message = null)
        {
            base.SetForbidden(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetNotFound(string? message = null)
        {
            base.SetNotFound(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetConflict(string? message = null)
        {
            base.SetConflict(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetUnprocessableEntity(string? message = null)
        {
            base.SetUnprocessableEntity(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetInternalServerError(string? message = null)
        {
            base.SetInternalServerError(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetServiceUnavailable(string? message = null)
        {
            base.SetServiceUnavailable(message);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> AddMessage(string code, string text)
        {
            base.AddMessage(code, text);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> AddInfoMessage(string text, string? code = null)
        {
            base.AddInfoMessage(text, code);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> AddWarningMessage(string text, string? code = null)
        {
            base.AddWarningMessage(text, code);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> AddErrorMessage(string text, string? code = null)
        {
            base.AddErrorMessage(text, code);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> ClearErrors()
        {
            base.ClearErrors();
            return this;
        }

        public new ApiServiceResponse<TApiPayload> ClearMessages()
        {
            base.ClearMessages();
            return this;
        }
    }
}
