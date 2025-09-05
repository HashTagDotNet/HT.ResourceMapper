namespace HT.Api.Service.Contracts
{
    public class LinksBuilder<TResponseData> where TResponseData : class, new()
    {
        private readonly ServiceResponseBuilder<TResponseData> _parent;
        public LinksBuilder(ServiceResponseBuilder<TResponseData> serviceResponseBuilder)
        {
            _parent = serviceResponseBuilder;
        }
    }
}
