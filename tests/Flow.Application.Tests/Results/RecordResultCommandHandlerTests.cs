using Flow.Application.Common.Interfaces;
using Flow.Application.Results.Commands.RecordResult;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;
using Flow.Application.Common.Exceptions;

namespace Flow.Application.Tests.Results;

public class RecordResultCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Guid _actorId = Guid.NewGuid();

    public RecordResultCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(_actorId);
        _currentUserMock.Setup(u => u.UserName).Returns("Manager");
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_NewResult_CreatesResultAndAuditLog()
    {
        var project = Project.Create("Test Project", "Desc", Guid.NewGuid(), ProjectPriority.Medium);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockResultSet = MockDbSetHelper.BuildMockDbSet<Result>(Array.Empty<Result>());

        var capturedResults = new List<Result>();
        mockResultSet.Setup(s => s.Add(It.IsAny<Result>()))
            .Callback<Result>(r => capturedResults.Add(r));

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Results).Returns(mockResultSet.Object);

        var handler = new RecordResultCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var command = new RecordResultCommand(
            ProjectId: project.Id,
            EstimatedRevenue: 100_000m,
            EstimatedSavings: 20_000m,
            EstimatedCost: 40_000m,
            ActualRevenue: null,
            ActualSavings: null,
            ActualCost: null,
            PaybackPeriodMonths: null,
            Notes: null);

        var dto = await handler.Handle(command, CancellationToken.None);

        capturedResults.Should().HaveCount(1);
        capturedResults[0].EstimatedROI.Should().Be(200m);
        capturedResults[0].ActualROI.Should().BeNull();
        dto.EstimatedROI.Should().Be(200m);
        dto.ProjectId.Should().Be(project.Id);

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "ResultRecorded" && l.EntityId == project.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingResult_UpdatesInsteadOfCreating()
    {
        var project = Project.Create("Test Project", "Desc", Guid.NewGuid(), ProjectPriority.Medium);
        var existingResult = Result.Create(project.Id, _actorId);
        existingResult.SetEstimated(80_000m, 0m, 40_000m);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockResultSet = MockDbSetHelper.BuildMockDbSet(new[] { existingResult });
        var capturedResults = new List<Result>();
        mockResultSet.Setup(s => s.Add(It.IsAny<Result>()))
            .Callback<Result>(r => capturedResults.Add(r));

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Results).Returns(mockResultSet.Object);

        var handler = new RecordResultCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var command = new RecordResultCommand(
            ProjectId: project.Id,
            EstimatedRevenue: null,
            EstimatedSavings: null,
            EstimatedCost: null,
            ActualRevenue: 90_000m,
            ActualSavings: 5_000m,
            ActualCost: 35_000m,
            PaybackPeriodMonths: 6,
            Notes: "Exceeded expectations");

        var dto = await handler.Handle(command, CancellationToken.None);

        capturedResults.Should().BeEmpty(); // Add was NOT called
        existingResult.ActualROI.Should().NotBeNull();
        existingResult.EstimatedROI.Should().NotBeNull(); // unchanged
        dto.Notes.Should().Be("Exceeded expectations");
    }

    [Fact]
    public async Task Handle_AllNullFields_ExistingResult_ReturnsCurrentStateWithoutSaving()
    {
        var project = Project.Create("Test Project", "Desc", Guid.NewGuid(), ProjectPriority.Medium);
        var existingResult = Result.Create(project.Id, _actorId);
        existingResult.SetEstimated(100_000m, 0m, 50_000m);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockResultSet = MockDbSetHelper.BuildMockDbSet(new[] { existingResult });

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Results).Returns(mockResultSet.Object);

        var handler = new RecordResultCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var command = new RecordResultCommand(
            ProjectId: project.Id,
            EstimatedRevenue: null,
            EstimatedSavings: null,
            EstimatedCost: null,
            ActualRevenue: null,
            ActualSavings: null,
            ActualCost: null,
            PaybackPeriodMonths: null,
            Notes: null);

        var dto = await handler.Handle(command, CancellationToken.None);

        // SaveChangesWithAuditAsync should NOT have been called
        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        dto.EstimatedROI.Should().NotBeNull(); // existing state preserved
    }

    [Fact]
    public async Task Handle_AllNullFields_NoExistingResult_ThrowsValidationException()
    {
        var project = Project.Create("Test Project", "Desc", Guid.NewGuid(), ProjectPriority.Medium);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockResultSet = MockDbSetHelper.BuildMockDbSet<Result>(Array.Empty<Result>());

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Results).Returns(mockResultSet.Object);

        var handler = new RecordResultCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var command = new RecordResultCommand(
            ProjectId: project.Id,
            EstimatedRevenue: null,
            EstimatedSavings: null,
            EstimatedCost: null,
            ActualRevenue: null,
            ActualSavings: null,
            ActualCost: null,
            PaybackPeriodMonths: null,
            Notes: null);

        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
