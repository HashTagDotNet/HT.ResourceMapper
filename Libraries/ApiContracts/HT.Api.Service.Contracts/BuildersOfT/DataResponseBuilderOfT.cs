namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class DataBuilder<T> where T : class, new()
    {
        private readonly ServiceResponseBuilder<T> _responseBuilder;

        public DataBuilder(ServiceResponseBuilder<T> parent)
        {
            _responseBuilder = parent;
        }
        
        public DataBuilder<T> Set(T data)
        {
            _responseBuilder.BackingServiceResponse.SetData(data);
            return this;
        }

        public DataBuilder<T> Clear()
        {
            _responseBuilder.BackingServiceResponse.ClearData();
            return this;
        }

        public HttpApiResponseBuilderOfT<T> Http =>_responseBuilder.Http;
        public ValidationBuilder<T> Validation => _responseBuilder.Validation;
        public MetaDataBuilderOfT<T> Meta => _responseBuilder.Meta;

        public ApiServiceResponse<T> Build() => _responseBuilder.Build();
    }

}
