using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.UpdateIdea;

public record UpdateIdeaCommand(
    Guid IdeaId,
    [Required] string Title,
    [Required] string Description,
    [Required] string Problem,
    Guid? LinkedGuidelineId) : IRequest;
