using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HT.Api.Service.Contracts
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

        public HttpResponseBuilder<T> Http =>_responseBuilder.Http;
        public ValidationBuilder<T> Validation => _responseBuilder.Validation;
        public MetaDataBuilderOfT<T> Meta => _responseBuilder.Meta;

        public ApiServiceResponse<T> Build() => _responseBuilder.Build();
    }

}
