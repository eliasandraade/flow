using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.CreateIdea;

public record CreateIdeaCommand(
    [Required] string Title,
    [Required] string Description,
    [Required] string Problem,
    Guid? LinkedGuidelineId) : IRequest<IdeaSummaryDto>;
