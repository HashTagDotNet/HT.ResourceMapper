using System.Net;
using HT.Api.Service.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResourceMapper.Common.Client.Config;
using ResourceMapper.Common.Client.Sample.Interfaces;
using ResourceMapper.Common.Shared.Contracts;
using ResourceMapper.Feature1.Server.Interfaces;

namespace ResourceMapper.UI.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class DateController : ControllerBase
{
    private readonly ISampleService _dateTimeService;
    private readonly ILogger<DateController> _logger;

    public DateController(ISampleService dateTimeService, ILogger<DateController> logger, GlobalConfig config)
    {
        _dateTimeService = dateTimeService;
        _logger = logger;
       
    }

    [HttpGet]
    public async Task<ActionResult<ApiServiceResponse<SampleGetDateTimeResponse>>> GetCurrentDate(SampleGetDateTimeRequest request, CancellationToken cancellationToken)
    {
        var svcResponse = await _dateTimeService.GetDaysAgoAsync(request, cancellationToken);
        return StatusCode((int)(svcResponse.HttpStatusCode ?? HttpStatusCode.OK), svcResponse);
    }
}