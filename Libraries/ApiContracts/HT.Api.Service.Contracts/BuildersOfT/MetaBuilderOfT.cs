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
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;
            _parent.BackingServiceResponse.ApiResponse.MetaData.AddTag(key, value);
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
            _parent.BackingServiceResponse.ApiResponse.MetaData.AddMessage(message);
            return this;
        }

        public MetaBuilder<TResponseData> SetId(string responseId)
        {
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;
            _parent.BackingServiceResponse.ApiResponse.MetaData.ResponseId = responseId;
            return this;
        }
        public MetaBuilder<TResponseData> SetTimestamp(string timestamp)
        {
            if (_parent.BackingServiceResponse.ApiResponse?.MetaData == null) return this;
            _parent.BackingServiceResponse.ApiResponse.MetaData.Timestamp = timestamp;
            return this;
        }
    }

}
