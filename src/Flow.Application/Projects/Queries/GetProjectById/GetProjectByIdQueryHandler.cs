using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Persistence;
using MediatR;

namespace Flow.Application.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IProjectRepository _projects;
    private readonly IIdeaRepository _ideas;
    private readonly IGuidelineRepository _guidelines;

    public GetProjectByIdQueryHandler(
        IProjectRepository projects,
        IIdeaRepository ideas,
        IGuidelineRepository guidelines)
    {
        _projects = projects;
        _ideas = ideas;
        _guidelines = guidelines;
    }

    public async Task<ProjectDetailDto> Handle(
        GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        string? sourceIdeaTitle = null;
        if (project.SourceIdeaId is { } ideaId)
            sourceIdeaTitle = (await _ideas.GetByIdAsync(ideaId, cancellationToken))?.Title;

        string? guidelineTitle = null;
        if (project.LinkedGuidelineId is { } guidelineId)
            guidelineTitle = (await _guidelines.GetByIdAsync(guidelineId, cancellationToken))?.Title;

        return ProjectDetailDto.From(project, sourceIdeaTitle, guidelineTitle, DateTimeOffset.UtcNow);
    }
}
