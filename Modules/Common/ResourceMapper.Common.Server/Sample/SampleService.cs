using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Microsoft.ILogger.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ResourceMapper.Common.Server.Sample.Interfaces;
using ResourceMapper.Common.Shared.Contracts;
using System.Net;
using HT.Api.Service.Contracts.BuildersOfT;

namespace ResourceMapper.Common.Server.Sample
{
    public class SampleService : ISampleService
    {
        private readonly ILogger _logger;
        private readonly ISampleRepository _repo;

        public SampleService(ILogger<SampleService> logger,
            ISampleRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

     
        public async Task<ApiServiceResponse<SampleGetDateTimeResponse>> GetDaysAgoAsync(SampleGetDateTimeRequest request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<SampleGetDateTimeResponse>();
            return builder.Meta.SetStatus(CallStatusCode.Ok).BuildResponse();

            //try
            //{
            //    // HTTP layer configuration
            //    builder.Http
            //        .AddHeader("x-ht-api-version", "1.0")
            //        .AddHeader("x-request-id", Guid.NewGuid().ToString())
            //        .SetStatusCode(HttpStatusCode.OK);

            //    // Validation layer with conditional logic and rich error details
            //    builder.Validation
            //        .When(() => request.DateOffsetToGet < -10)
            //        .AddError("DateOffsetToGet", "Must be greater than -10")
            //        .Location(PropertyLocation.Body)
            //        .AddLink("https://help.example.com/validation#date-offset");

            //    builder.Validation
            //        .When(() => request.DateOffsetToGet >= 10)
            //        .AddError("DateOffsetToGet", "Must be less than 10")
            //        .Location(PropertyLocation.Body);

            //    // Meta information with operational context
            //    builder.Meta
            //        .AddTag("operation", "date-calculation")
            //        .AddTag("request-type", "date-offset")
            //        .AddMessage(msg =>
            //        {
            //            msg.MessageType = "Info";
            //            msg.Detail = "Date calculation performed";
            //            msg.SeverityCode = MessageSeverity.Info;
            //        });

            //    // Early return if validation failed
            //    if (builder.CallStatus != CallStatusCode.Ok)
            //    {
            //        builder.Meta.AddTag("validation-failed", "true");
            //        return builder.Response;
            //    }

            //    // Add informational error (using the enhanced AddError method)
            //    builder.Errors
            //        .AddError(CallStatusCode.Ok, "Date is valid on the server!", "DateOffsetToGet")
            //        .WithSeverity(MessageSeverity.Info);

            //    // Business logic execution with async validation
            //    var demoStateCheck = await _repo.GetDaysAgoAysnc(request.DateOffsetToGet, cancellationToken);
            //    var date = await _repo.GetDaysAgoAysnc(request.DateOffsetToGet, cancellationToken);

            //    // Data setting with success status and operational metadata
            //    builder.Data
            //        .Set(new SampleGetDateTimeResponse { FoundDate = date })
            //        .AsSuccess("Date retrieved successfully");

            //    builder.Meta
            //        .AddTag("execution-time", DateTime.UtcNow.ToString("HH:mm:ss.fff"))
            //        .AddTag("result-date", date.ToString("yyyy-MM-dd"));

            //    return builder.Response;
            //}
            //catch (OperationCanceledException)
            //{
            //    return builder.Errors
            //        .AddCancellation("Operation was cancelled by user request")
            //        .Root.Meta
            //        .AddTag("cancellation-source", "user-request")
            //        .Build();
            //}
            //catch (SqlException sqlEx)
            //{
            //    _logger.Error(sqlEx, "Database error occurred during date calculation");
            //    return builder.Errors
            //        .AddDatabaseError("Database operation failed", sqlEx.Message)
            //        .AddCorrelationId($"DB-{sqlEx.GetHashCode():X}")
            //        .Root.Meta
            //        .AddTag("error-source", "database")
            //        .AddTag("sql-error-number", sqlEx.Number.ToString())
            //        .Build();
            //}
            //catch (Exception ex)
            //{
            //    _logger.Error(ex, "Unexpected error retrieving date span");
            //    return builder.Errors
            //        .AddInternalError("UNEXPECTED_ERROR", "An unexpected error occurred")
            //        .WithStatus(CallStatusCode.InternalError)
            //        .AddCorrelationId($"ERR-{ex.GetHashCode():X}")
            //        .AddLink("https://support.example.com/errors/unexpected", "help", "Get help with unexpected errors")
            //        .Root.Meta
            //        .AddTag("error-type", ex.GetType().Name)
            //        .AddTag("stack-trace-hash", ex.StackTrace?.GetHashCode().ToString() ?? "none")
            //        .Build();
            //}
        }

    
    }
}
