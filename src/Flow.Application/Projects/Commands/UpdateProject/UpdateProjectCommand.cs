using System.ComponentModel.DataAnnotations;
using Flow.Domain.Enums;
using MediatR;

namespace Flow.Application.Projects.Commands.UpdateProject;

public record UpdateProjectCommand(
    Guid ProjectId,
    [Required] string Title,
    [Required] string Description,
    ProjectPriority Priority,
    Guid OwnerId,
    decimal? EstimatedCost,
    decimal? ActualCost,
    DateTimeOffset? Deadline) : IRequest;
