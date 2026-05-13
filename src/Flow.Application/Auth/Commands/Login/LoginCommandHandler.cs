using Flow.Application.Auth;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
namespace Flow.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _context;

    public LoginCommandHandler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new NotFoundException(nameof(User), request.Email);

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid) throw new ForbiddenException("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = Flow.Domain.Entities.RefreshToken.Create(user.Id, refreshTokenValue, DateTimeOffset.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email!,
            Role: user.Role.ToString()
        );
    }
}
