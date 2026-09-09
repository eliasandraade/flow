using Flow.Application.Guidelines;
using MediatR;

namespace Flow.Application.Guidelines.Queries.GetGuidelines;

public record GetGuidelinesQuery : IRequest<IReadOnlyList<GuidelineDto>>;
