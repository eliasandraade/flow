namespace Flow.Application.Results;

public record ResultDto(
    Guid Id,
    Guid ProjectId,
    decimal? EstimatedRevenue,
    decimal? EstimatedSavings,
    decimal? EstimatedCost,
    decimal? EstimatedROI,
    DateTimeOffset? EstimatedRecordedAt,
    decimal? ActualRevenue,
    decimal? ActualSavings,
    decimal? ActualCost,
    decimal? ActualROI,
    DateTimeOffset? ActualRecordedAt,
    int? PaybackPeriodMonths,
    string? Notes,
    Guid RecordedBy,
    DateTimeOffset RecordedAt,
    DateTimeOffset UpdatedAt);
