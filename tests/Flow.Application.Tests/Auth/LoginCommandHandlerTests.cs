using FluentAssertions;
using Flow.Application.Auth.Commands.Login;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _jwtServiceMock = new Mock<IJwtTokenService>();
        _contextMock = new Mock<IApplicationDbContext>();

        _contextMock.Setup(c => c.RefreshTokens).Returns(new Mock<DbSet<RefreshToken>>().Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new LoginCommandHandler(
            _userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResultWithCorrectRole()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Manager);
        var command = new LoginCommand("ana@example.com", "Password123!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Manager" });
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user, It.IsAny<IList<string>>())).Returns("access");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh");
        result.Email.Should().Be("ana@example.com");
        result.Role.Should().Be("Manager");
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsForbiddenException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(
            new LoginCommand("nobody@example.com", "pass"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsForbiddenException()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);
        _userManagerMock.Setup(m => m.FindByEmailAsync("ana@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var act = () => _handler.Handle(
            new LoginCommand("ana@example.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ValidCredentials_PersistsRefreshToken()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);
        var command = new LoginCommand("ana@example.com", "Password123!");
        var refreshTokensMock = new Mock<DbSet<RefreshToken>>();

        _contextMock.Setup(c => c.RefreshTokens).Returns(refreshTokensMock.Object);
        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Operator" });
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>())).Returns("t");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("r");

        await _handler.Handle(command, CancellationToken.None);

        refreshTokensMock.Verify(d => d.Add(It.IsAny<RefreshToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
