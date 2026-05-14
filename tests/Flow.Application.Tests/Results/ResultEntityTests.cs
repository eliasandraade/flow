using Flow.Domain.Entities;
using Flow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Flow.Application.Tests.Results;

public class ResultEntityTests
{
    private static Result MakeResult() =>
        Result.Create(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_ValidInput_SetsProjectIdAndRecordedBy()
    {
        var projectId = Guid.NewGuid();
        var recordedBy = Guid.NewGuid();

        var result = Result.Create(projectId, recordedBy);

        result.ProjectId.Should().Be(projectId);
        result.RecordedBy.Should().Be(recordedBy);
        result.EstimatedROI.Should().BeNull();
        result.ActualROI.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyProjectId_ThrowsDomainException()
    {
        var act = () => Result.Create(Guid.Empty, Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*ProjectId*");
    }

    [Fact]
    public void Create_EmptyRecordedBy_ThrowsDomainException()
    {
        var act = () => Result.Create(Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<DomainException>().WithMessage("*RecordedBy*");
    }

    [Fact]
    public void SetEstimated_ValidValues_ComputesROI()
    {
        var result = MakeResult();

        result.SetEstimated(revenue: 100_000m, savings: 20_000m, cost: 40_000m);

        // ROI = (100000 + 20000 - 40000) / 40000 * 100 = 200
        result.EstimatedROI.Should().Be(200m);
        result.EstimatedRevenue.Should().Be(100_000m);
        result.EstimatedRecordedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetActual_ValidValues_ComputesROI()
    {
        var result = MakeResult();

        result.SetActual(revenue: 80_000m, savings: 10_000m, cost: 30_000m);

        // ROI = (80000 + 10000 - 30000) / 30000 * 100 = 200
        result.ActualROI.Should().Be(200m);
        result.ActualRevenue.Should().Be(80_000m);
        result.ActualRecordedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetEstimated_ZeroCost_ReturnsNullROI()
    {
        var result = MakeResult();

        result.SetEstimated(revenue: 50_000m, savings: 10_000m, cost: 0m);

        result.EstimatedROI.Should().BeNull();
    }

    [Fact]
    public void SetActual_NullCost_ReturnsNullROI()
    {
        var result = MakeResult();

        result.SetActual(revenue: 50_000m, savings: 10_000m, cost: null);

        result.ActualROI.Should().BeNull();
    }

    [Fact]
    public void SetEstimated_DoesNotClearActual()
    {
        var result = MakeResult();
        result.SetActual(revenue: 80_000m, savings: 0m, cost: 40_000m);

        result.SetEstimated(revenue: 100_000m, savings: 0m, cost: 50_000m);

        result.ActualRevenue.Should().Be(80_000m);
        result.ActualROI.Should().NotBeNull();
    }

    [Fact]
    public void SetActual_DoesNotClearEstimated()
    {
        var result = MakeResult();
        result.SetEstimated(revenue: 100_000m, savings: 0m, cost: 50_000m);

        result.SetActual(revenue: 80_000m, savings: 0m, cost: 40_000m);

        result.EstimatedRevenue.Should().Be(100_000m);
        result.EstimatedROI.Should().NotBeNull();
    }
}
