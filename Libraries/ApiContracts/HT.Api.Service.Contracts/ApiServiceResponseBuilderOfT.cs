using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    public class ServiceResponseBuilder<TResponseData> where TResponseData : class, new()
    {
        internal readonly ApiServiceResponse<TResponseData> _serviceResponse = new();

        public ServiceResponseBuilder()
        {
            Http = new HttpResponseBuilder<TResponseData>(this);
            Data = new DataBuilder<TResponseData>(this);
            Meta = new MetaBuilder<TResponseData>(this);

            Validation = new ValidationBuilder(this);
            Errors = new ErrorBuilder<TResponseData>(this);
            Links = new LinksBuilder<TResponseData>(this);

        }

        // Backing object access
        public ApiServiceResponse<TResponseData> BackingServiceResponse => _serviceResponse;

        // Finalized response with cleanup
        public ApiServiceResponse<TResponseData> Response
        {
            get
            {
                // Fix up correlation IDs, call status, and validate data/error consistency
                FixupCallStatus();
                ValidateResponseState();
                return _serviceResponse;
            }
        }

        // Nested builder access
        public HttpResponseBuilder<TResponseData> Http { get; }

        public ValidationBuilder Validation { get; }
        public ErrorBuilder<TResponseData> Errors { get; }
        public LinksBuilder<TResponseData> Links { get; }
        public MetaBuilder<TResponseData> Meta { get; }
        public DataBuilder<TResponseData> Data { get; }

        // Direct property access for convenience
      

        private void FixupCallStatus()
        {
            // Ensure CallStatus in metadata matches computed status
            if (_serviceResponse.ApiResponse?.MetaData != null)
            {
                _serviceResponse.ApiResponse.MetaData.CallStatus = CallStatus;
            }
        }

        private void ValidateResponseState()
        {
            // Ensure data and errors don't coexist (JSON:API compliance)
            var hasData = _serviceResponse.Data != null;
            var hasErrors = _serviceResponse.ApiResponse.Errors?.Any() == true;

            if (hasData && hasErrors)
            {
                // Clear data when errors exist for JSON:API compliance
                _serviceResponse.Data = null;
            }
        }
       
  
    }

}
