using Flow.Application.Auth;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DomainRefreshToken = Flow.Domain.Entities.RefreshToken;

namespace Flow.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _context;

    public RefreshTokenCommandHandler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtTokenService.GetUserIdFromToken(request.AccessToken)
            ?? throw new ForbiddenException("Invalid access token.");

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, cancellationToken)
            ?? throw new ForbiddenException("Refresh token not found.");

        if (!storedToken.IsActive)
            throw new ForbiddenException("Refresh token is expired or revoked.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        var roles = await _userManager.GetRolesAsync(user);

        storedToken.Revoke();
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var newRefreshValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = DomainRefreshToken.Create(user.Id, newRefreshValue, DateTimeOffset.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshValue,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email!,
            Role: user.Role.ToString()
        );
    }
}
