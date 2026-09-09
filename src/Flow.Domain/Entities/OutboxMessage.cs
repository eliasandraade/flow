using Flow.Domain.Enums;
using Flow.Domain.Exceptions;

namespace Flow.Domain.Entities;

/// <summary>
/// Intent to deliver a push notification, written inside the same transaction as the
/// domain change and dispatched outside of it. This is what keeps provider availability
/// from deciding whether an idea approval is persisted.
/// </summary>
public class OutboxMessage
{
    public const int DefaultMaxAttempts = 6;

    public Guid Id { get; private set; }
    public Guid NotificationId { get; private set; }

    /// <summary>
    /// Idempotency key. A unique index on this field turns a reprocessed event into a
    /// failed insert instead of a duplicate push.
    /// </summary>
    public string DedupeKey { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? DeepLink { get; private set; }
    public OutboxStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage For(Notification notification, string dedupeKey)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (string.IsNullOrWhiteSpace(dedupeKey))
            throw new DomainException("Outbox message requires a dedupe key.");

        var now = DateTimeOffset.UtcNow;
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            DedupeKey = dedupeKey,
            UserId = notification.UserId,
            Title = notification.Title,
            Body = notification.Body,
            DeepLink = notification.DeepLink,
            Status = OutboxStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = now,
            CreatedAt = now
        };
    }

    public void MarkDispatched()
    {
        Status = OutboxStatus.Dispatched;
        DispatchedAt = DateTimeOffset.UtcNow;
        LastError = null;
    }

    /// <summary>
    /// Records a failed attempt and schedules the next one with exponential backoff plus
    /// jitter, so a provider outage does not produce a synchronised retry storm.
    /// </summary>
    public void MarkFailed(string error, int maxAttempts = DefaultMaxAttempts)
    {
        AttemptCount++;
        LastError = Truncate(error, 1000);

        if (AttemptCount >= maxAttempts)
        {
            Status = OutboxStatus.DeadLettered;
            return;
        }

        Status = OutboxStatus.Failed;
        NextAttemptAt = DateTimeOffset.UtcNow.Add(BackoffFor(AttemptCount));
    }

    internal static TimeSpan BackoffFor(int attempt)
    {
        var seconds = Math.Min(Math.Pow(2, attempt) * 5, 900);
        var jitter = Random.Shared.NextDouble() * seconds * 0.25;
        return TimeSpan.FromSeconds(seconds + jitter);
    }

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
