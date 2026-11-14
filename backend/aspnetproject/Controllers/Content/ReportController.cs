using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Reports;
using aspnetproject.Infrastructure.Services.BusinessLogic.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Content;

[Authorize]
[Route("api/reports")]
public class ReportController : BaseController
{
    private readonly ReportService _reportService;

    public ReportController(ReportService reportService)
    {
        _reportService = reportService;
    }

    // 200, not 201: there is no endpoint that reads one report back, so there is no location to point at.
    // The failures mean different things (target not found, own target, already pending), so they collapse to 400.
    [HttpPost]
    [EnableRateLimiting(RateLimitConfig.Policies.CreateReport)]
    public async Task<ActionResult<ApplicationResponse<StandardReportDto>>> Create([FromBody] CreateReportDto createReportDto)
    {
        var response = await _reportService.CreateReportAsync(GetUserId(), IsAdministrator(), createReportDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpGet]
    [Authorize(Roles = nameof(RoleName.Administrator))]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminReadReports)]
    public async Task<ActionResult<ListResponse<AdminReportDto>>> GetAll([FromQuery] ReportListQuery reportQuery)
    {
        var response = await _reportService.GetReportsAsync(reportQuery);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}
