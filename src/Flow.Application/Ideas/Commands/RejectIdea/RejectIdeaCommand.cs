using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.RejectIdea;

public record RejectIdeaCommand(Guid IdeaId, [Required] string ManagerComment) : IRequest;
