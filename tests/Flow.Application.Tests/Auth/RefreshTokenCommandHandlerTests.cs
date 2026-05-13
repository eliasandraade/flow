using FluentAssertions;
using Flow.Application.Auth.Commands.RefreshToken;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;

    public RefreshTokenCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _jwtServiceMock = new Mock<IJwtTokenService>();
        _contextMock = new Mock<IApplicationDbContext>();
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ThrowsForbiddenException()
    {
        _jwtServiceMock.Setup(j => j.GetUserIdFromToken("bad-token")).Returns((Guid?)null);

        var handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);

        var act = () => handler.Handle(
            new RefreshTokenCommand("bad-token", "any-refresh"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_RevokedRefreshToken_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var refreshToken = RefreshToken.Create(userId, "revoked-token", DateTimeOffset.UtcNow.AddDays(7));
        refreshToken.Revoke();

        _jwtServiceMock.Setup(j => j.GetUserIdFromToken("access")).Returns(userId);

        var tokens = new List<RefreshToken> { refreshToken }.AsQueryable();
        var asyncProvider = new TestAsyncQueryProvider<RefreshToken>(tokens.Provider);
        var mockDbSet = new Mock<DbSet<RefreshToken>>();
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.Provider).Returns(asyncProvider);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.Expression).Returns(tokens.Expression);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.ElementType).Returns(tokens.ElementType);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.GetEnumerator()).Returns(tokens.GetEnumerator());
        mockDbSet.As<IAsyncEnumerable<RefreshToken>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<RefreshToken>(tokens.GetEnumerator()));

        _contextMock.Setup(c => c.RefreshTokens).Returns(mockDbSet.Object);

        var handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);

        var act = () => handler.Handle(new RefreshTokenCommand("access", "revoked-token"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}

// Helpers for async EF Core queries in tests
file class TestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => _inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        // EF Core calls ExecuteAsync<Task<T>>; execute synchronously and wrap in completed Task
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var syncResult = _inner.Execute(expression);
        var fromResult = typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType);
        return (TResult)fromResult.Invoke(null, new[] { syncResult })!;
    }
}

file class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

file class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
}
