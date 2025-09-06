using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class MetaBuilder<TResponseData> where TResponseData : class, new()
    {
        private readonly ServiceResponseBuilder<TResponseData> _parent;

        public MetaBuilder(ServiceResponseBuilder<TResponseData> parent)
        {
            _parent = parent;
        }

        public MetaBuilder<TResponseData> Set(Action<MetaData> metaData)
        {
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;
            metaData.Invoke(_parent.BackingServiceResponse.ApiResponse.MetaData);
            return this;
        }
        public MetaBuilder<TResponseData> AddTag(string key, string value)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key);

            _parent.BackingServiceResponse.ApiResponse.MetaData ??= new MetaData();
            _parent.BackingServiceResponse.ApiResponse.MetaData.Tags ??= new SortedDictionary<string, string>();
            _parent.BackingServiceResponse.ApiResponse.MetaData.Tags[key] = value;
            return this;
        }

        public MetaBuilder<TResponseData> SetStatus(CallStatusCode? callStatus)
        {
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;
            _parent.BackingServiceResponse.ApiResponse.MetaData.CallStatus = callStatus;
            return this;
        }
        public MetaBuilder<TResponseData> AddMessage(Action<Message> message)
        {
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;
            var msg = new Message();
            message.Invoke(msg);
            _parent.BackingServiceResponse.ApiResponse.MetaData.Messages ??= new List<Message>();
            _parent.BackingServiceResponse.ApiResponse.MetaData.Messages?.Add(msg);
            return this;
        }
       
        public MetaBuilder<TResponseData> SetId(string responseId)
        {
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;

            _parent.BackingServiceResponse.ApiResponse.MetaData.ResponseId = string.IsNullOrWhiteSpace(responseId)
                ? Guid.NewGuid().ToString("N")[..8]
                : responseId;
            return this;
        }
        public MetaBuilder<TResponseData> SetTimestamp(DateTime timestamp)
        {
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;
            _parent.BackingServiceResponse.ApiResponse.MetaData.Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.ffZ");
            return this;
        }

        public HttpApiResponseBuilder<TResponseData> Http => _parent.Http;
        public ValidationBuilder<TResponseData> Validation => _parent.Validation;
        public ErrorBuilder<TResponseData> Errors => _parent.Errors;
        public LinksBuilder<TResponseData> Links => _parent.Links;
        public DataBuilder<TResponseData> Data => _parent.Data;

        public ApiServiceResponse<TResponseData> BuildResponse(Action<ApiServiceResponse<TResponseData>>? response=null) => _parent.BuildResponse(response);
    }

}
