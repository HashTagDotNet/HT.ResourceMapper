using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class LinksBuilder<T> where T : class, new()
    {
        private readonly ServiceResponseBuilder<T> _responseBuilder;
        private ApiResponse<T> ApiResponse => _responseBuilder.BackingServiceResponse.ApiResponse;

        public LinksBuilder(ServiceResponseBuilder<T> serviceResponseBuilder)
        {
            _responseBuilder = serviceResponseBuilder;
        }
        public LinksBuilder<T> AddLink(string href, string? rel = null, string? title = null, string? method = null)
        {
            if (ApiResponse.Links == null)
            {
                ApiResponse.Links = new List<Link>();
            }
            ApiResponse.Links.Add(new Link(href, rel, title, method));
            return this;
        }
        public HttpApiResponseBuilder<T> Http => _responseBuilder.Http;
        public ValidationBuilder<T> Validation => _responseBuilder.Validation;
        public ErrorBuilder<T> Errors => _responseBuilder.Errors;
        public DataBuilder<T> Data => _responseBuilder.Data;
        public MetaBuilder<T> Meta => _responseBuilder.Meta;
        public ApiServiceResponse<T> BuildResponse(Action<ApiServiceResponse<T>>? response = null) => _responseBuilder.BuildResponse(response);
    }
}
