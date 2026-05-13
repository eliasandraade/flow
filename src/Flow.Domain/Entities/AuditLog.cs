namespace Flow.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorId { get; private set; }
    public string ActorName { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        Guid entityId,
        string action,
        Guid actorId,
        string actorName,
        string? oldValue = null,
        string? newValue = null,
        string? reason = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorId = actorId,
            ActorName = actorName,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
