using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Persistence;
using MediatR;

namespace Flow.Application.Results.Queries.GetResult;

public class GetResultQueryHandler : IRequestHandler<GetResultQuery, ResultDto>
{
    private readonly IResultRepository _results;

    public GetResultQueryHandler(IResultRepository results) => _results = results;

    public async Task<ResultDto> Handle(GetResultQuery request, CancellationToken cancellationToken)
    {
        var result = await _results.GetByProjectIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Result", request.ProjectId);

        return ResultDto.From(result);
    }
}
