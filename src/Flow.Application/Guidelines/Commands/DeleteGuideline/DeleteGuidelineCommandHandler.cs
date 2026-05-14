using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Commands.DeleteGuideline;

public class DeleteGuidelineCommandHandler : IRequestHandler<DeleteGuidelineCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteGuidelineCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteGuidelineCommand request, CancellationToken cancellationToken)
    {
        var guideline = await _context.StrategicGuidelines
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Guideline", request.Id);

        _context.StrategicGuidelines.Remove(guideline);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
