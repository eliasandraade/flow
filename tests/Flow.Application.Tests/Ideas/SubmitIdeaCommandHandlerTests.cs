using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas.Commands.SubmitIdea;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Ideas;

public class SubmitIdeaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public SubmitIdeaCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_OwnDraftIdea_SubmitsAndWritesAuditLog()
    {
        var actorId = Guid.NewGuid();
        var idea = Idea.Create("Title", "Desc", "Problem", actorId);

        _currentUserMock.Setup(u => u.UserId).Returns(actorId);
        _currentUserMock.Setup(u => u.UserName).Returns("Test User");

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);

        var handler = new SubmitIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new SubmitIdeaCommand(idea.Id), CancellationToken.None);

        idea.Status.Should().Be(IdeaStatus.UnderReview);
        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Submitted" && l.EntityId == idea.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AnotherUsersIdea_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var idea = Idea.Create("Title", "Desc", "Problem", ownerId);

        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);

        var handler = new SubmitIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var act = async () => await handler.Handle(new SubmitIdeaCommand(idea.Id), CancellationToken.None);

        await act.Should().ThrowAsync<Flow.Application.Common.Exceptions.ForbiddenException>();
    }
}
