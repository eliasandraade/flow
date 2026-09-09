using Flow.Application.Guidelines;
using Flow.Application.Guidelines.Commands.CreateGuideline;
using Flow.Application.Guidelines.Commands.DeleteGuideline;
using Flow.Application.Guidelines.Commands.UpdateGuideline;
using Flow.Application.Guidelines.Queries.GetGuidelineById;
using Flow.Application.Guidelines.Queries.GetGuidelines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/guidelines")]
[Authorize]
public class GuidelinesController : ControllerBase
{
    private readonly IMediator _mediator;

    public GuidelinesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuidelineDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGuidelinesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GuidelineDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGuidelineByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Leadership")]
    public async Task<ActionResult<GuidelineDto>> Create(
        [FromBody] CreateGuidelineCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Leadership")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateGuidelineCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Leadership")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteGuidelineCommand(id), ct);
        return NoContent();
    }
}
