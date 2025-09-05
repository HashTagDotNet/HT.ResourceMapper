using HT.Api.Client.Contracts.Models;

namespace HT.Api.Client.Contracts.Interfaces
{
    public interface IApiResponse
    {
        public List<ErrorMessage>? Errors { get; set; }
        public List<Link>? Links { get; set; }
        public MetaData MetaData { get; set; }

        

    }
}
