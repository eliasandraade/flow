using Flow.Application.Common.Interfaces;
using Flow.Application.Projects.Commands.StartProject;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Projects;

public class StartProjectCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public StartProjectCommandHandlerTests()
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
    public async Task Handle_PlannedProject_StartsAndCreatesSnapshotAndAudit()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("My Project", "Desc", ownerId, ProjectPriority.Medium);
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

        var handler = new StartProjectCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new StartProjectCommand(project.Id), CancellationToken.None);

        project.Status.Should().Be(ProjectStatus.InProgress);
        project.StartDate.Should().NotBeNull();

        capturedSnapshots.Should().HaveCount(1);
        capturedSnapshots[0].TriggerAction.Should().Be("Started");
        capturedSnapshots[0].Status.Should().Be(ProjectStatus.InProgress);

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Started" && l.EntityId == project.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
