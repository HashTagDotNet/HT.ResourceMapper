//using MacroPoint.Azure.ResourceExplorer.SqlClient.Extensions;
//using MacroPoint.Azure.ResourceExplorer.SqlClient.Extensions.Abstractions.Interfaces;
//using Microsoft.Data.SqlClient;
//using MacroPoint.Azure.ResourceExplorer.Common.Server.Sample.Interfaces;

//namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Sample
//{
//    public class SampleSqlRepository : ISampleRepository
//    {
//        private readonly IDbConnector _db;

//        public SampleSqlRepository(IDbConnector db)
//        {
//            _db = db;
//        }

//        /// <summary>
//        /// Demo method to show how to call a stored procedure and map the result (method comments are not normally set in real repositories (interface and implementation are 'self documenting'))
//        /// </summary>
//        /// <param name="daySpan"></param>
//        /// <param name="cancellationToken"></param>
//        public async Task<DateTime> GetDaysAgoAysnc(int daySpan, CancellationToken cancellationToken)
//        {
//            using var cmd = _db.RO.SprocCommand("ResourceExplorer.Resource_GetDaysAgo") // use ResourceExplorer schema
//                .AddInteger("@DaysAgo", daySpan); // create parameters as needed, output parameters are supports

           
//           // return DateTime.UtcNow.AddDays(daySpan);
//            var result = await _db.Execute.ExecuteRowAsync(cmd, // execute query, most frequently ExecuteQueryAsync or ExecuteQueryAsync
//                dr => dr.ReadDateTime("DaysAgo") // map result using extension methods
//                , cancellationToken: cancellationToken);

                
//            return result;

//        }
//    }
//}
