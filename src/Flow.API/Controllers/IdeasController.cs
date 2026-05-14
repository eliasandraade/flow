using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas;
using Flow.Application.Ideas.Commands.AddIdeaComment;
using Flow.Application.Ideas.Commands.ApproveIdea;
using Flow.Application.Ideas.Commands.CreateIdea;
using Flow.Application.Ideas.Commands.RejectIdea;
using Flow.Application.Ideas.Commands.SubmitIdea;
using Flow.Application.Ideas.Commands.UpdateIdea;
using Flow.Application.Ideas.Queries.GetIdeaById;
using Flow.Application.Ideas.Queries.GetIdeaComments;
using Flow.Application.Ideas.Queries.GetIdeas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/ideas")]
[Authorize]
public class IdeasController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public IdeasController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<IdeaSummaryDto>> Create(
        [FromBody] CreateIdeaCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IdeaSummaryDto>>> GetAll(CancellationToken ct)
    {
        var isPrivileged = User.IsInRole("Manager") || User.IsInRole("Leadership");
        var submittedById = isPrivileged ? (Guid?)null : _currentUser.UserId;
        var result = await _mediator.Send(new GetIdeasQuery(submittedById), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IdeaDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIdeaByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateIdeaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { IdeaId = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new SubmitIdeaCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Approve(
        Guid id, [FromBody] ApproveIdeaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { IdeaId = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectIdeaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { IdeaId = id }, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<IdeaCommentDto>>> GetComments(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIdeaCommentsQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IdeaCommentDto>> AddComment(
        Guid id, [FromBody] AddIdeaCommentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command with { IdeaId = id }, ct);
        return Created(string.Empty, result);
    }
}
