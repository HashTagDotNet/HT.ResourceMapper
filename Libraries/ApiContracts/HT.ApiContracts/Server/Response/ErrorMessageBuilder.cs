using HT.Api.Contracts.Client.Interfaces;
using HT.Api.Contracts.Client.Models;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace HT.Api.Contracts.Server.Response
{
    public class ErrorMessageBuilder
    {
        private readonly ErrorMessage _message;

        internal ErrorMessageBuilder(ErrorMessage message, IApiResponse response)
        {
            _message = message;
        }
        public IApiResponse? Api { get; set; }

        public ErrorMessageBuilder Title(string title)
        {
            _message.Title = title;
            return this;
        }
        public ErrorMessageBuilder Detail(string detail)
        {
            _message.Detail = detail;
            return this;
        }

        public ErrorMessageBuilder Status(ErrorCodes status= ErrorCodes.Ok)
        {
            _message.StatusCode = status;
            return this;
        }

        public ErrorMessageBuilder MessageCode(string messageCode)
        {
            _message.MessageCode = messageCode;
            return this;
        }
        public ErrorMessageBuilder Build(Action<ErrorMessage>? errorMessage)
        {
            errorMessage?.Invoke(_message);
            return this;
        }
        public ErrorMessageBuilder RequestPart(RequestLocation source)
        {
            _message.RequestLocation = source;
            return this;
        }

        public ErrorMessageBuilder Property(string? property)
        {
            _message.Property = property;
            return this;
        }

        public ErrorMessageBuilder AddTag(string key, string? value=null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (value == null)
            {
                _message.Tags ??= new Dictionary<string, string?>();
                _message.Tags.Remove(key);
            }
            _message.Tags ??= new Dictionary<string, string?>();
            _message.Tags[key] = value;
            return this;
        }
        public ErrorMessageBuilder AddLink(string href, string? rel = null, string? title = null, string? method = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(href);
            _message.Links ??= new List<Link>();
            _message.Links.Add(new Link(href, rel, title, method));
            return this;
        }

        public ErrorMessageBuilder ApplicationActions(string? actions = null)
        {
            _message.SuggestedApplicationActions = actions;
            return this;
        }
        public ErrorMessageBuilder UserActions(string? actions = null)
        {
            _message.SuggestedUserActions = actions;
            return this;
        }
    }
}
