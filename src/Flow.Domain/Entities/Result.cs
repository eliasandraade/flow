using Flow.Domain.Common;
using Flow.Domain.Exceptions;

namespace Flow.Domain.Entities;

public class Result : BaseEntity
{
    public Guid ProjectId { get; private set; }

    // Estimated phase
    public decimal? EstimatedRevenue { get; private set; }
    public decimal? EstimatedSavings { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? EstimatedROI { get; private set; }
    public DateTimeOffset? EstimatedRecordedAt { get; private set; }

    // Actual phase
    public decimal? ActualRevenue { get; private set; }
    public decimal? ActualSavings { get; private set; }
    public decimal? ActualCost { get; private set; }
    public decimal? ActualROI { get; private set; }
    public DateTimeOffset? ActualRecordedAt { get; private set; }

    public int? PaybackPeriodMonths { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedBy { get; private set; }

    private Result() { }

    public static Result Create(Guid projectId, Guid recordedBy)
    {
        if (projectId == Guid.Empty)
            throw new DomainException("ProjectId is required.");
        if (recordedBy == Guid.Empty)
            throw new DomainException("RecordedBy is required.");

        return new Result
        {
            ProjectId = projectId,
            RecordedBy = recordedBy
        };
    }

    /// <summary>
    /// Updates the estimated ROI group. Does not modify actual values.
    /// </summary>
    public void SetEstimated(decimal? revenue, decimal? savings, decimal? cost)
    {
        EstimatedRevenue = revenue;
        EstimatedSavings = savings;
        EstimatedCost = cost;
        EstimatedROI = ComputeRoi(revenue, savings, cost);
        EstimatedRecordedAt = DateTimeOffset.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Updates the actual ROI group. Does not modify estimated values.
    /// </summary>
    public void SetActual(decimal? revenue, decimal? savings, decimal? cost)
    {
        ActualRevenue = revenue;
        ActualSavings = savings;
        ActualCost = cost;
        ActualROI = ComputeRoi(revenue, savings, cost);
        ActualRecordedAt = DateTimeOffset.UtcNow;
        SetUpdated();
    }

    public void SetNotes(int? paybackPeriodMonths, string? notes)
    {
        PaybackPeriodMonths = paybackPeriodMonths;
        Notes = notes;
        SetUpdated();
    }

    // ROI = (Revenue + Savings - Cost) / Cost * 100
    // Returns null if Cost is null or zero.
    private static decimal? ComputeRoi(decimal? revenue, decimal? savings, decimal? cost)
    {
        if (cost == null || cost == 0m) return null;
        return ((revenue ?? 0m) + (savings ?? 0m) - cost.Value) / cost.Value * 100m;
    }
}
