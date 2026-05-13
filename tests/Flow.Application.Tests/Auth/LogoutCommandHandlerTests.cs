using FluentAssertions;
using Flow.Application.Auth.Commands.Logout;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public LogoutCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private Mock<DbSet<RefreshToken>> BuildMockDbSet(IEnumerable<RefreshToken> data)
    {
        var list = data.ToList();
        var queryable = list.AsQueryable();
        var asyncProvider = new LogoutTestAsyncQueryProvider<RefreshToken>(queryable.Provider);
        var mockDbSet = new Mock<DbSet<RefreshToken>>();
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.Provider).Returns(asyncProvider);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        mockDbSet.As<IAsyncEnumerable<RefreshToken>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new LogoutTestAsyncEnumerator<RefreshToken>(list.GetEnumerator()));
        return mockDbSet;
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokesAndSaves()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "active-token", DateTimeOffset.UtcNow.AddDays(7));

        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        var mockDbSet = BuildMockDbSet(new[] { token });
        _contextMock.Setup(c => c.RefreshTokens).Returns(mockDbSet.Object);

        var handler = new LogoutCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new LogoutCommand("active-token"), CancellationToken.None);

        token.IsRevoked.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoTokenFound_DoesNotSave()
    {
        var userId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        var mockDbSet = BuildMockDbSet(Array.Empty<RefreshToken>());
        _contextMock.Setup(c => c.RefreshTokens).Returns(mockDbSet.Object);

        var handler = new LogoutCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new LogoutCommand("nonexistent-token"), CancellationToken.None);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

file class LogoutTestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    public LogoutTestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new LogoutTestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new LogoutTestAsyncEnumerable<TElement>(expression);
    public object? Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => _inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var syncResult = _inner.Execute(expression);
        var fromResult = typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType);
        return (TResult)fromResult.Invoke(null, new[] { syncResult })!;
    }
}

file class LogoutTestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public LogoutTestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new LogoutTestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    IQueryProvider IQueryable.Provider => new LogoutTestAsyncQueryProvider<T>(this);
}

file class LogoutTestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public LogoutTestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
}
