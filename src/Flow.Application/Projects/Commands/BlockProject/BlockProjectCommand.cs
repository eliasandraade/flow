using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Projects.Commands.BlockProject;

public record BlockProjectCommand(Guid ProjectId, [Required] string Reason) : IRequest;
