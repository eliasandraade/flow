namespace Flow.Domain.Entities;

public class PointLedgerEntry
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int Points { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public DateTimeOffset AwardedAt { get; private set; }

    private PointLedgerEntry() { }

    public static PointLedgerEntry Create(
        Guid userId,
        int points,
        string reason,
        string referenceType,
        Guid referenceId)
    {
        return new PointLedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Points = points,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            AwardedAt = DateTimeOffset.UtcNow
        };
    }
}
