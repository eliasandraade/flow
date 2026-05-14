using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas.Commands.ApproveIdea;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Ideas;

public class ApproveIdeaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public ApproveIdeaCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_UnderReviewIdea_ApprovesAndAwardsPoints()
    {
        var actorId = Guid.NewGuid();
        var submitterId = Guid.NewGuid();
        var idea = Idea.Create("Title", "Desc", "Problem", submitterId);
        idea.Submit();

        var submitter = User.Create("Submitter", "submitter@test.com", UserRole.Operator);
        submitter.Id = submitterId;

        _currentUserMock.Setup(u => u.UserId).Returns(actorId);
        _currentUserMock.Setup(u => u.UserName).Returns("Manager");

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        var submitterByIdSet = MockDbSetHelper.BuildMockDbSet(new[] { submitter });

        var capturedLedgerEntries = new List<PointLedgerEntry>();
        var mockLedgerSet = MockDbSetHelper.BuildMockDbSet<PointLedgerEntry>(Array.Empty<PointLedgerEntry>());
        mockLedgerSet.Setup(s => s.Add(It.IsAny<PointLedgerEntry>()))
            .Callback<PointLedgerEntry>(e => capturedLedgerEntries.Add(e));

        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);
        _contextMock.Setup(c => c.Users).Returns(submitterByIdSet.Object);
        _contextMock.Setup(c => c.PointLedgerEntries).Returns(mockLedgerSet.Object);

        var handler = new ApproveIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new ApproveIdeaCommand(idea.Id, "Well done!"), CancellationToken.None);

        idea.Status.Should().Be(IdeaStatus.Approved);
        idea.ManagerComment.Should().Be("Well done!");
        submitter.Points.Should().Be(50);
        capturedLedgerEntries.Should().HaveCount(1);
        capturedLedgerEntries[0].Points.Should().Be(50);
        capturedLedgerEntries[0].ReferenceType.Should().Be("Idea");

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Approved" && l.EntityId == idea.Id && l.ActorId == actorId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
