using Flow.Application.Common.Interfaces;
using Flow.Application.Projects.Commands.BlockProject;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Projects;

public class BlockProjectCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public BlockProjectCommandHandlerTests()
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
    public async Task Handle_PlannedProject_BlocksAndCreatesSnapshotWithReason()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Project", "Desc", ownerId, ProjectPriority.High);
        var owner = User.Create("Owner", "owner@test.com", UserRole.Manager);
        owner.Id = ownerId;

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockUserSet = MockDbSetHelper.BuildMockDbSet(new[] { owner });

        var capturedSnapshots = new List<ProjectSnapshot>();
        var mockSnapshotSet = MockDbSetHelper.BuildMockDbSet<ProjectSnapshot>(Array.Empty<ProjectSnapshot>());
        mockSnapshotSet.Setup(s => s.Add(It.IsAny<ProjectSnapshot>()))
            .Callback<ProjectSnapshot>(s => capturedSnapshots.Add(s));

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Users).Returns(mockUserSet.Object);
        _contextMock.Setup(c => c.ProjectSnapshots).Returns(mockSnapshotSet.Object);

        var handler = new BlockProjectCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(
            new BlockProjectCommand(project.Id, "Awaiting budget approval."),
            CancellationToken.None);

        project.Status.Should().Be(ProjectStatus.Blocked);
        project.BlockedReason.Should().Be("Awaiting budget approval.");

        capturedSnapshots.Should().HaveCount(1);
        capturedSnapshots[0].TriggerAction.Should().Be("Blocked");
        capturedSnapshots[0].BlockedReason.Should().Be("Awaiting budget approval.");

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l =>
                        l.Action == "Blocked" &&
                        l.Reason == "Awaiting budget approval." &&
                        l.EntityId == project.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
