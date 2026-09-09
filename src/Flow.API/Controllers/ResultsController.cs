using Flow.Application.Results;
using Flow.Application.Results.Commands.RecordResult;
using Flow.Application.Results.Queries.GetResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/result")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResultsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ResultDto>> Get(Guid projectId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetResultQuery(projectId), ct);
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ResultDto>> Upsert(
        Guid projectId,
        [FromBody] RecordResultCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command with { ProjectId = projectId }, ct);
        return Ok(result);
    }
}
