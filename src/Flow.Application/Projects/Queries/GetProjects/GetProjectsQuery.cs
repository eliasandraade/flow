using MediatR;

namespace Flow.Application.Projects.Queries.GetProjects;

public record GetProjectsQuery(Guid? OwnerId) : IRequest<IReadOnlyList<ProjectSummaryDto>>;
