using Flow.Application.Common.Interfaces;
using Flow.Application.Projects;
using Flow.Application.Projects.Commands.BlockProject;
using Flow.Application.Projects.Commands.CancelProject;
using Flow.Application.Projects.Commands.CompleteProject;
using Flow.Application.Projects.Commands.ConvertIdeaToProject;
using Flow.Application.Projects.Commands.CreateProject;
using Flow.Application.Projects.Commands.StartProject;
using Flow.Application.Projects.Commands.UnblockProject;
using Flow.Application.Projects.Commands.UpdateProject;
using Flow.Application.Projects.Queries.GetProjectById;
using Flow.Application.Projects.Queries.GetProjectSnapshots;
using Flow.Application.Projects.Queries.GetProjectTimeline;
using Flow.Application.Projects.Queries.GetProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ProjectsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("projects")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ProjectSummaryDto>> Create(
        [FromBody] CreateProjectCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("ideas/{ideaId:guid}/convert")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ProjectSummaryDto>> ConvertFromIdea(
        Guid ideaId, [FromBody] ConvertIdeaToProjectCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command with { IdeaId = ideaId }, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> GetAll(CancellationToken ct)
    {
        var isOperator = User.IsInRole("Operator");
        var ownerId = isOperator ? _currentUser.UserId : null;
        var result = await _mediator.Send(new GetProjectsQuery(ownerId), ct);
        return Ok(result);
    }

    [HttpGet("projects/{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPut("projects/{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateProjectCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { ProjectId = id }, ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/start")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new StartProjectCommand(id), ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/complete")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CompleteProjectCommand(id), ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/cancel")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelProjectCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { ProjectId = id }, ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/block")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Block(
        Guid id, [FromBody] BlockProjectCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { ProjectId = id }, ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/unblock")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new UnblockProjectCommand(id), ct);
        return NoContent();
    }

    [HttpGet("projects/{id:guid}/timeline")]
    public async Task<ActionResult<IReadOnlyList<TimelineEntryDto>>> GetTimeline(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectTimelineQuery(id), ct);
        return Ok(result);
    }

    [HttpGet("projects/{id:guid}/snapshots")]
    [Authorize(Roles = "Manager,Leadership")]
    public async Task<ActionResult<IReadOnlyList<ProjectSnapshotDto>>> GetSnapshots(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectSnapshotsQuery(id), ct);
        return Ok(result);
    }
}
