using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    public class ApiServiceResponse<TApiPayload>  where TApiPayload : class, new()
    {
        public  ApiResponse<TApiPayload> ApiResponse { get; set; } = new();
        public HttpApiResponse HttpResponse { get; set; }
    }
}
