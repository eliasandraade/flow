using Flow.Application.Dashboard;
using Flow.Application.Dashboard.Queries.GetDashboardSummary;
using Flow.Application.Dashboard.Queries.GetProjectDashboard;
using Flow.Application.Dashboard.Queries.GetStrategyDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "Manager,Leadership")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// The full executive picture: idea funnel, project health, financial and non-financial
    /// outcomes, blockers, risk, rankings, strategy and campaign performance, and trends.
    /// Everything arrives ready to draw.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDashboardSummaryQuery(), ct));

    [HttpGet("projects/{id:guid}")]
    [ProducesResponseType(typeof(ProjectDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDashboardDto>> GetProject(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetProjectDashboardQuery(id), ct));

    [HttpGet("strategies/{id:guid}")]
    [ProducesResponseType(typeof(StrategyDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StrategyDashboardDto>> GetStrategy(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetStrategyDashboardQuery(id), ct));
}
