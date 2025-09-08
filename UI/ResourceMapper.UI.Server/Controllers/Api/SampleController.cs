//using HT.Api.Client.Contracts.Models;
//using Microsoft.AspNetCore.Mvc;
//using ResourceMapper.Common.Shared.Contracts;
//using System.Net;
//using Microsoft.Data.SqlClient;
//using ResourceMapper.Common.Server.Sample.Interfaces;

//namespace ResourceMapper.UI.Server.Controllers.Api;

//[ApiController]
//[Route("api/[controller]")]
//public class SampleController : ControllerBase
//{
//    private readonly ISampleService _dateTimeService;

//    public SampleController(ISampleService dateTimeService)
//    {
//        _dateTimeService = dateTimeService;
//    }

//    [HttpGet]
//    public async Task<ActionResult<ApiResponse<SampleGetDateTimeResponse>>> GetCurrentDate([FromQuery] SampleGetDateTimeRequest request, CancellationToken cancellationToken = default)
//    {
//        var svcResponse = await _dateTimeService.GetDaysAgoAsync(request, cancellationToken);
//        return StatusCode((int)( HttpStatusCode.OK), svcResponse.ApiResponse);
//    }

//    [HttpPost]
//    public async Task<ActionResult<ApiResponse<SampleGetDateTimeResponse>>> GetCurrentDatePost([FromBody] SampleGetDateTimeRequest request, CancellationToken cancellationToken = default)
//    {
//        var svcResponse = await _dateTimeService.GetDaysAgoAsync(request, cancellationToken);
//        return StatusCode((int)(HttpStatusCode.OK), svcResponse.ApiResponse);
//    }
//}