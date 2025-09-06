using System.Reflection.Metadata.Ecma335;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class ErrorBuilder<TResponseData> where TResponseData : class, new()
    {
        private readonly ServiceResponseBuilder<TResponseData> _parentBuilder;

        public ErrorBuilder(ServiceResponseBuilder<TResponseData> parent)
        {
            _parentBuilder = parent;
        }

        public ApiServiceResponse<TResponseData> Build()
        {
            return _parentBuilder.Response;
        }
        public ErrorMessageBuilderOfT<TResponseData> AddError(string title, string detail)
        {
            var errorMessage = new ErrorMessage
            {
                Title = title,
                Detail = detail
            };
            _parentBuilder.BackingServiceResponse.AddErrorMessage(errorMessage);
            return new ErrorMessageBuilderOfT<TResponseData>(_parentBuilder, errorMessage);
        }

    }
}
   
    

