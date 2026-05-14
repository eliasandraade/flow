namespace Flow.Application.Projects;

public record TimelineEntryDto(
    string Action,
    Guid ActorId,
    string ActorName,
    string? OldValue,
    string? NewValue,
    string? Reason,
    DateTimeOffset Timestamp);
