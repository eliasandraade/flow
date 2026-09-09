namespace Flow.Application.Common.Interfaces;

/// <summary>
/// Delivers a push notification through an external provider.
///
/// Implementations must report honestly: a provider that is not configured returns
/// <see cref="PushDeliveryOutcome.NotConfigured"/> rather than pretending to have
/// delivered anything. A notification the user never received must never be recorded
/// as sent.
/// </summary>
public interface IPushNotificationSender
{
    bool IsConfigured { get; }

    Task<PushDeliveryResult> SendAsync(
        PushNotificationRequest request, CancellationToken cancellationToken = default);
}

public sealed record PushNotificationRequest(
    Guid UserId,
    string Title,
    string Body,
    string? DeepLink,
    string DedupeKey);

public enum PushDeliveryOutcome
{
    /// <summary>The provider accepted the message.</summary>
    Delivered,

    /// <summary>Worth retrying: timeout, 5xx, rate limit.</summary>
    TransientFailure,

    /// <summary>Not worth retrying: malformed request, unknown recipient, rejected credentials.</summary>
    PermanentFailure,

    /// <summary>No credentials configured. The message stays queued rather than being faked.</summary>
    NotConfigured
}

public sealed record PushDeliveryResult(
    PushDeliveryOutcome Outcome,
    string? Error = null,
    string? ProviderMessageId = null)
{
    public static PushDeliveryResult Delivered(string? providerMessageId = null) =>
        new(PushDeliveryOutcome.Delivered, ProviderMessageId: providerMessageId);

    public static PushDeliveryResult Transient(string error) =>
        new(PushDeliveryOutcome.TransientFailure, error);

    public static PushDeliveryResult Permanent(string error) =>
        new(PushDeliveryOutcome.PermanentFailure, error);

    public static PushDeliveryResult NotConfigured() =>
        new(PushDeliveryOutcome.NotConfigured, "Push provider is not configured.");
}
