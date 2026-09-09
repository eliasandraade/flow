using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Application.Common.Persistence;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = Flow.Domain.Entities.RefreshToken;

namespace Flow.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthTokenIssuer _tokenIssuer;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        AuthTokenIssuer tokenIssuer,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtTokenService.GetUserIdFromToken(request.AccessToken)
            ?? throw new UnauthorizedException("Invalid access token.");

        var tokenHash = DomainRefreshToken.Hash(request.RefreshToken);

        var storedToken = await _refreshTokens.GetByHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Refresh token not found.");

        if (storedToken.UserId != userId)
            throw new UnauthorizedException("Refresh token does not belong to this user.");

        if (!storedToken.IsActive)
        {
            // A revoked token being presented again is the classic replay signature: the
            // safe response is to invalidate the whole chain and force a fresh login.
            _logger.LogWarning(
                "Refresh token replay detected for user {UserId}. Revoking all active tokens.", userId);
            await _refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);
            throw new UnauthorizedException("Refresh token is expired or revoked.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        var roles = await _userManager.GetRolesAsync(user);

        return await _unitOfWork.ExecuteAsync(async ct =>
        {
            var (result, issued) = await _tokenIssuer.IssueAsync(user, roles, ct);

            // Rotation: the presented token dies here and points at its replacement.
            storedToken.RotateTo(issued);
            await _refreshTokens.UpdateAsync(storedToken, ct);

            return result;
        }, cancellationToken);
    }
}
