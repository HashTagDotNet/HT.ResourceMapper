using HT.Api.Client.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class ErrorMessageBuilderOfT<TResponseData> where TResponseData : class, new()
    {
        private readonly ServiceResponseBuilder<TResponseData> _parent;
        private readonly ErrorMessage _errorMessage;

        public ErrorMessageBuilderOfT(ServiceResponseBuilder<TResponseData> parent, ErrorMessage errorMessage)
        {
            _parent = parent;
            _errorMessage = errorMessage;
        }


        public ErrorMessageBuilderOfT WithStatus(CallStatusCode statusCode)
        {
            _errorMessage.CallStatus = statusCode;
            return this;
        }

        public ErrorMessageBuilderOfT WithSeverity(MessageSeverity severity)
        {
            // Note: ErrorMessage might inherit severity from MessageBase
            // This would need to be implemented based on the actual inheritance structure
            return this;
        }

        public ErrorMessageBuilderOfT AddCorrelationId(string correlationId)
        {
            _errorMessage.MessageUid = correlationId;
            return this;
        }

        public ErrorMessageBuilderOfT AddLink(string href, string? rel = null, string? title = null, string? method = null)
        {
            _errorMessage.Links ??= new List<Link>();
            _errorMessage.Links.Add(new Link(href, rel, title, method));
            return this;
        }

        // Builder completion
        public ServiceResponseBuilder<TResponseData> And => _parent;
        public ApiServiceResponse<TResponseData> Build() => _parent.Response;
    }

}
