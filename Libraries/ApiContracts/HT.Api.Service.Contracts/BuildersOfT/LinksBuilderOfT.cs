namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class LinksBuilderOfT<TResponseData> where TResponseData : class, new()
    {
        private readonly ServiceResponseBuilder<TResponseData> _parent;
        public LinksBuilderOfT(ServiceResponseBuilder<TResponseData> serviceResponseBuilder)
        {
            _parent = serviceResponseBuilder;
        }
    }
}
