namespace HT.Api.Contracts.Client.Interfaces
{
    public interface IApiResponse<TApiData>:IApiResponse where TApiData : class,new()
    {
        /// <summary>
        /// The data returned by the API
        /// </summary>
        public TApiData? Data { get; set; }
      
    }

}
