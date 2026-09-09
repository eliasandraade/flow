using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.AddIdeaComment;

public record AddIdeaCommentCommand(Guid IdeaId, [Required] string Body) : IRequest<IdeaCommentDto>;
