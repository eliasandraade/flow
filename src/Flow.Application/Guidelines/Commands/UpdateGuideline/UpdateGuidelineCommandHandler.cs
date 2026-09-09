using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Commands.UpdateGuideline;

public class UpdateGuidelineCommandHandler : IRequestHandler<UpdateGuidelineCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateGuidelineCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateGuidelineCommand request, CancellationToken cancellationToken)
    {
        var guideline = await _context.StrategicGuidelines
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Guideline", request.Id);

        guideline.Update(request.Title, request.Description);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
