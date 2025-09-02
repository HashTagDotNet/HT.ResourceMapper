using System.Net;
using HT.Api.Contracts.Client.Interfaces;
using HT.Api.Contracts.Client.Models;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable RedundantBaseQualifier

namespace HT.Api.Contracts.Server.Response
{
    public class ApiServiceResponse<TApiPayload> : ApiServiceResponse where TApiPayload : class, new()
    {
        private ApiResponse<TApiPayload>? _apiResponse;

        public new ApiResponse<TApiPayload>? ApiResponse
        {
            get => GetApiResponse as ApiResponse<TApiPayload>;
            set => _apiResponse = value;
        }


        protected override IApiResponse GetApiResponse
        {
            get
            {
                _apiResponse ??= new ApiResponse<TApiPayload>();
                return _apiResponse;
            }
        }

        public TApiPayload Data
        {
            get
            {
                _apiResponse ??= new ApiResponse<TApiPayload>();
                _apiResponse.Data ??= new TApiPayload();
                return _apiResponse.Data;
            }
            set
            {
                _apiResponse ??= new ApiResponse<TApiPayload>();
                _apiResponse.Data = value;
            }
        }

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

        public new ApiServiceResponse<TApiPayload> SetHttpStatus(HttpStatusCode statusCode,
            string? statusMessage = null)
        {
            base.SetHttpStatus(statusCode, statusMessage);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> SetStatusMessage(string? statusMessage)
        {
            base.SetHttpStatusMessage(statusMessage);
            return this;
        }

        public new ApiServiceResponse<TApiPayload> AsOk(string? statusMessage = null)
        {
            SetHttpStatus(System.Net.HttpStatusCode.OK, statusMessage);
            return this;
        }

        public ApiServiceResponse<TApiPayload> InvalidArgument(string field, string? message = null)
        {
            base.AsInvalidArgument(field, message);
            return this;
        }

        public ApiServiceResponse<TApiPayload> Conflict(string? message = null)
        {
            // ReSharper disable once RedundantBaseQualifier
            base.AsConflict(message);
            return this;
        }

        public ApiServiceResponse<TApiPayload> NotFound(string field, string? message = null)
        {
            base.AsNotFound(message);
            return this;
        }

        public ApiServiceResponse<TApiPayload> AsInternal(string? message)
        {
            base.AsNotFound(message);
            return this;
        }
     

    }
}