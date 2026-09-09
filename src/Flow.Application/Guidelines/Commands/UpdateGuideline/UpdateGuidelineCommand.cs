using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Guidelines.Commands.UpdateGuideline;

public record UpdateGuidelineCommand(
    Guid Id,
    [Required] string Title,
    [Required] string Description) : IRequest;
