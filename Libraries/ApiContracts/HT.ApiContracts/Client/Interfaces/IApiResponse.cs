using HT.Api.Contracts.Client.Models;

namespace HT.Api.Contracts.Client.Interfaces
{
    public interface IApiResponse
    {
        public List<ErrorMessage>? Errors { get; set; }
        public List<Link>? Links { get; set; }
        public MetaData MetaData { get; set; }

        

    }
}
