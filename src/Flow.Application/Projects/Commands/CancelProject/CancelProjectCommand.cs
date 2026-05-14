using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Projects.Commands.CancelProject;

public record CancelProjectCommand(Guid ProjectId, [Required] string Reason) : IRequest;
