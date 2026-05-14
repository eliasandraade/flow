using System.ComponentModel.DataAnnotations;
using Flow.Application.Guidelines;
using MediatR;

namespace Flow.Application.Guidelines.Commands.CreateGuideline;

public record CreateGuidelineCommand(
    [Required] string Title,
    [Required] string Description) : IRequest<GuidelineDto>;
