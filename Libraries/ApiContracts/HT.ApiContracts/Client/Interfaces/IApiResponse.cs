using HT.ApiContracts.Client.Models;

namespace HT.ApiContracts.Client.Interfaces
{
    public interface IApiResponse
    {
        public List<ErrorMessage>? Errors { get; set; }
        public List<Link>? Links { get; set; }
        public MetaData MetaData { get; set; }

        

    }
}
