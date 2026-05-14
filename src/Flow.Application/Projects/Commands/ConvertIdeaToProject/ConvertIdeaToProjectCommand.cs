using System.ComponentModel.DataAnnotations;
using Flow.Application.Projects;
using Flow.Domain.Enums;
using MediatR;

namespace Flow.Application.Projects.Commands.ConvertIdeaToProject;

public record ConvertIdeaToProjectCommand(
    Guid IdeaId,
    [Required] string Title,
    [Required] string Description,
    ProjectPriority Priority,
    Guid OwnerId,
    decimal? EstimatedCost,
    DateTimeOffset? Deadline) : IRequest<ProjectSummaryDto>;
