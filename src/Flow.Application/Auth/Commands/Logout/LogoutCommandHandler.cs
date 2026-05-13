using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public LogoutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == request.RefreshToken && rt.UserId == _currentUser.UserId,
                cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
