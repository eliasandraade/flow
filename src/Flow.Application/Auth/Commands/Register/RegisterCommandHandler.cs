using Flow.Application.Auth;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DomainRefreshToken = Flow.Domain.Entities.RefreshToken;

namespace Flow.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResultDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _context;

    public RegisterCommandHandler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new ConflictException($"A user with email '{request.Email}' already exists.");

        var user = User.Create(request.Name, request.Email, UserRole.Operator);
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationException(errors);
        }

        await _userManager.AddToRoleAsync(user, UserRole.Operator.ToString());

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = DomainRefreshToken.Create(user.Id, refreshTokenValue, DateTimeOffset.UtcNow.AddDays(7));

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
