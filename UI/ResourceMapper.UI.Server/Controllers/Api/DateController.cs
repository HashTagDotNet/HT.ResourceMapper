using ResourceMapper.Feature1.Server.Interfaces;
using ResourceMapper.Feature1.Shared.Contracts;
using HT.Api.Contracts.Client.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ResourceMapper.UI.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class DateController : ControllerBase
{
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<DateController> _logger;

    public DateController(IDateTimeService dateTimeService, ILogger<DateController> logger)
    {
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<GetDateTimeResponse>>> GetCurrentDate()
    {
        try
        {
            var result = await _dateTimeService.GetDateTime();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving server date");
            return StatusCode(500, "An error occurred while retrieving the server date");
        }
    }
}