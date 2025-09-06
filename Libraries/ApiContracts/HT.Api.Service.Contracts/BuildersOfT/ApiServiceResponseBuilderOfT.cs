using HT.Api.Client.Contracts;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class ServiceResponseBuilder<TResponseData> where TResponseData : class, new()
    {
        private readonly ApiServiceResponse<TResponseData> _serviceResponse = new();

        public ServiceResponseBuilder()
        {
            Http = new HttpApiResponseBuilder<TResponseData>(this);
            Data = new DataBuilder<TResponseData>(this);
            Meta = new MetaBuilder<TResponseData>(this);

            Validation = new ValidationBuilder<TResponseData>(this);
            Errors = new ErrorBuilder<TResponseData>(this);
            Links = new LinksBuilder<TResponseData>(this);

        }

        // Backing object access
        public ApiServiceResponse<TResponseData> BackingServiceResponse => _serviceResponse;

        // Finalized response with cleanup
      

        // Nested builder access
        public HttpApiResponseBuilder<TResponseData> Http { get; }

        public ValidationBuilder<TResponseData> Validation { get; }
        public ErrorBuilder<TResponseData> Errors { get; }
        public LinksBuilder<TResponseData> Links { get; }
        public MetaBuilder<TResponseData> Meta { get; }
        public DataBuilder<TResponseData> Data { get; }

        // Direct property access for convenience

        public ApiServiceResponse<TResponseData> BuildResponse(Action<ApiServiceResponse<TResponseData>>? postBuildFix = null)
        {
            var apiResponse = BackingServiceResponse.ApiResponse;

            // set Meta call status (one is required for this implementation)
            if (apiResponse.MetaData.CallStatus == null)
            {
                if (apiResponse.Errors is { Count: > 0 })
                {
                    var firstError = apiResponse.Errors.FirstOrDefault(e => e.CallStatus.IsError());
                    if (firstError != null)
                    {
                        // if there are errors, set call status based on first error
                        apiResponse.MetaData.CallStatus =
                           firstError?.CallStatus ??
                            CallStatusCode.Error; // use generic 'Error' since we don't have a more specific code
                    }
                    else
                    {
                        // no error CallStatus found, so use generic 'Error'
                        apiResponse.MetaData.CallStatus = CallStatusCode.Error;
                    }
                }
                else
                {
                    // no errors, so this is a success
                    apiResponse.MetaData.CallStatus = CallStatusCode.Ok;
                }
            }

            // fix error collection ids
            if (apiResponse?.Errors is { Count: 0 })
            {
                apiResponse.Errors = null; // do not serialize empty errors collection
            }

            if (apiResponse?.Errors is { Count: > 0 })
            {
                foreach (var error in apiResponse.Errors)
                {
                    FixMessageEnumerationIds(error);
                }
            }

            // fix links collection
            if (apiResponse?.Links is { Count: 0 })
            {
                apiResponse.Links = null; // do not serialize empty links collection
            }

            FixMetaEnumerations(apiResponse?.MetaData);

            SetHttpStatusCode(BackingServiceResponse);

            // allow caller to do any final fixes
            postBuildFix?.Invoke(BackingServiceResponse);
            return BackingServiceResponse;
        }

        internal void SetHttpStatusCode(ApiServiceResponse<TResponseData> serviceResponse)
        {
            if (serviceResponse.HttpResponse == null)
            {
                serviceResponse.HttpResponse = new HttpApiResponse();
            }

            if (serviceResponse.ApiResponse.MetaData.CallStatus == null)
            {
                throw new ArgumentNullException("serviceResponse.ApiResponse.MetaDAta.CallStatus",
                    "MetaData.CallStatus must be set before attempting to build a response");
            }

             // http status code is not set, so set it based on call status
            if (serviceResponse.HttpResponse.HttpStatusCode == null && serviceResponse.ApiResponse.MetaData.CallStatus != null)
            {
                serviceResponse.HttpResponse.HttpStatusCode =
                    serviceResponse.ApiResponse.MetaData.CallStatus.ToHttpStatusCode();
            }
        }

        internal void FixMetaEnumerations(MetaData? metaData)
        {
            if (metaData == null) return;
            if (metaData.Tags is { Count: 0 })
            {
                metaData.Tags = null; // do not serialize empty tags collection
            }
            if (metaData.Messages is { Count: 0 })
            {
                metaData.Messages = null; // do not serialize empty messages collection
            }

            if (metaData.Messages is { Count: > 0 })
            {
                foreach (var message in metaData.Messages)
                {
                    FixMessageEnumerationIds(message);
                }
            }
            if (metaData.CallStatus != null && metaData.CallStatusId == null || metaData.CallStatusId == 0)
            {
                metaData.CallStatusId = (int)metaData!.CallStatus!.Value;
            }

            metaData.Flags ??= new MetaDataFlags();
            metaData.Flags.ResultCategory = metaData!.CallStatus!.Value.GetResultCategory();
            if (metaData.Flags.ResultCategory != null && metaData.Flags.ResultCategoryId == null)
            {
                metaData.Flags.ResultCategoryId = (int)metaData.Flags.ResultCategory;
            }
            metaData.Flags.IsRetryable = metaData!.CallStatus!.Value.IsRetryable();
        }

        internal void FixMessageEnumerationIds(Message message)
        {
            if (message.CallStatus != null && message.CallStatusId == null)
            {
                message.CallStatusId = (int)message.CallStatus;
            }

            if (message.Tags is { Count: 0 })
            {
                message.Tags = null; // do not serialize empty tags collection
            }

            if (message.PropertyLocation != null && message.PropertyLocationId == null)
            {
                message.PropertyLocationId = (int)message.PropertyLocation;
            }

            if (message.Links is { Count: 0 })
            {
                message.Links = null; // do not serialize empty links collection
            }

            if (message.SeverityCode != null && message.SeverityId == null)
            {
                message.SeverityId = (int)message.SeverityCode;
            }

        }



    }

}
