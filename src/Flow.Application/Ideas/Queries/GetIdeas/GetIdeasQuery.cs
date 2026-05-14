using MediatR;

namespace Flow.Application.Ideas.Queries.GetIdeas;

public record GetIdeasQuery(Guid? SubmittedById) : IRequest<IReadOnlyList<IdeaSummaryDto>>;
