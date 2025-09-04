using System.Net;
using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    public class ApiServiceResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public List<KeyValuePair<string, string>>? Headers { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public HttpStatusCode? HttpStatusCode { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HttpStatusMessage { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual ApiResponse ApiResponse { get; set; } = new();

        public ApiServiceResponse AddHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            Headers ??= [];
            Headers.Add(new KeyValuePair<string, string>(key,value));
            return this;
        }

        public ApiServiceResponse AppendHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            Headers ??= [];
            var existingHeader = Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existingHeader.Key != null && !string.IsNullOrWhiteSpace(key))
            {
                Headers.Remove(existingHeader);
                Headers.Add(new KeyValuePair<string, string>(key, existingHeader.Value + "," + value));
            }
            else
            {
                Headers.Add(new KeyValuePair<string, string>(key, value));
            }
            return this;
        }
        
        public ApiServiceResponse SetStatusCode(HttpStatusCode statusCode, string? statusMessage=null)
        {
            HttpStatusCode = statusCode;
            HttpStatusMessage = statusMessage;
            return this;
        }

        public ApiServiceResponse SetStatusMessage(string? statusMessage)
        {
            HttpStatusMessage = statusMessage;
            return this;
        }

        public ApiServiceResponse AddMessage(string code, string text)
        {
            
            ApiResponse.MetaData.Messages ??= new List<Message>();
            ApiResponse.MetaData.Messages.Add(new Message()
            {
               MessageCode = code,
               Detail = text
            });
            return this;
        }
    }
}
