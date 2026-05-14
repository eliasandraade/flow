using System.ComponentModel.DataAnnotations;
using Flow.Domain.Enums;
using MediatR;

namespace Flow.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    [Required] string Title,
    [Required] string Description,
    ProjectPriority Priority,
    Guid OwnerId,
    decimal? EstimatedCost,
    DateTimeOffset? Deadline) : IRequest<ProjectSummaryDto>;
