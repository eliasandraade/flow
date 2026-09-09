using FluentAssertions;
using Flow.Application.Auth;
using Flow.Application.Auth.Commands.Register;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<DbSet<RefreshToken>> _refreshTokensMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtServiceMock = new Mock<IJwtTokenService>();
        _contextMock = new Mock<IApplicationDbContext>();
        _refreshTokensMock = new Mock<DbSet<RefreshToken>>();

        _contextMock.Setup(c => c.RefreshTokens).Returns(_refreshTokensMock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RegisterCommandHandler(
            _userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsAuthResultWithOperatorRole()
    {
        var command = new RegisterCommand("Ana Lima", "ana@example.com", "Password123!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserRole.Operator.ToString()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "Operator" });
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>()))
            .Returns("access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("refresh-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Email.Should().Be("ana@example.com");
        result.Role.Should().Be("Operator");
        result.Name.Should().Be("Ana Lima");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        var command = new RegisterCommand("Ana Lima", "existing@example.com", "Password123!");
        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(User.Create("Existing", "existing@example.com", UserRole.Operator));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_IdentityFailure_ThrowsValidationException()
    {
        var command = new RegisterCommand("Ana Lima", "ana@example.com", "weak");
        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password too short." }));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsRefreshTokenToContext()
    {
        var command = new RegisterCommand("Ana Lima", "ana@example.com", "Password123!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "Operator" });
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>())).Returns("token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");

        await _handler.Handle(command, CancellationToken.None);

        _refreshTokensMock.Verify(d => d.Add(It.IsAny<RefreshToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
