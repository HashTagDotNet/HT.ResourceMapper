using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;
using ResourceMapper.Common.Server.Sample.Interfaces;

namespace ResourceMapper.Common.Server.Sample
{
    public class SampleSqlRepository : ISampleRepository
    {
        private readonly IDbConnector _db;

        public SampleSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        /// <summary>
        /// Demo method to show how to call a stored procedure and map the result (method comments are not normally set in real repositories (interface and implementation are 'self documenting'))
        /// </summary>
        /// <param name="daySpan"></param>
        /// <param name="cancellationToken"></param>
        public async Task<DateTime> GetDaysAgoAysnc(int daySpan, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("HTResourceMapper.Resource_GetDaysAgo") // use HTResourceMapper schema
                .AddInteger("@DaysAgo", daySpan); // create parameters as needed, output parameters are supports

           
           // return DateTime.UtcNow.AddDays(daySpan);
            var result = await _db.Execute.ExecuteRowAsync(cmd, // execute query, most frequently ExecuteQueryAsync or ExecuteQueryAsync
                dr => dr.ReadDateTime("DaysAgo") // map result using extension methods
                , cancellationToken: cancellationToken);

                
            return result;

        }
    }
}
