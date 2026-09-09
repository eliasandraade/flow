using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas.Commands.RejectIdea;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Ideas;

public class RejectIdeaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public RejectIdeaCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.UserName).Returns("Manager");
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_UnderReviewIdea_RejectsAndWritesAuditLog()
    {
        var idea = Idea.Create("Title", "Desc", "Problem", Guid.NewGuid());
        idea.Submit();

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);

        var handler = new RejectIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new RejectIdeaCommand(idea.Id, "Not aligned."), CancellationToken.None);

        idea.Status.Should().Be(IdeaStatus.Rejected);
        idea.ManagerComment.Should().Be("Not aligned.");
        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Rejected" && l.Reason == "Not aligned.")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
