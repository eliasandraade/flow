using Flow.Application.Common.Interfaces;
using Flow.Application.Common.Persistence;
using MediatR;
using DomainRefreshToken = Flow.Domain.Entities.RefreshToken;

namespace Flow.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ICurrentUserService _currentUser;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens, ICurrentUserService currentUser)
    {
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = DomainRefreshToken.Hash(request.RefreshToken);
        var token = await _refreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        // Logging out with someone else's token must not revoke it, and a token that is
        // already inactive needs no work.
        if (token is null || token.UserId != _currentUser.UserId || !token.IsActive)
            return;

        token.Revoke();
        await _refreshTokens.UpdateAsync(token, cancellationToken);
    }
}
