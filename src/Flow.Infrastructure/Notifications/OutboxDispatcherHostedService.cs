using Flow.Application.Common.Interfaces;
using Flow.Application.Common.Persistence;
using Flow.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flow.Infrastructure.Notifications;

/// <summary>
/// Drains the notification outbox outside of any domain transaction.
///
/// This is what keeps push-provider availability from deciding whether an idea approval is
/// persisted: the domain writes the notification and the outbox row inside its own
/// transaction and commits, and delivery happens here afterwards, with its own retry
/// schedule.
/// </summary>
public sealed class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox dispatcher is disabled by configuration.");
            return;
        }

        // Give the API a moment to finish starting before competing for the database.
        await Task.Delay(_options.StartupDelay, stoppingToken).ConfigureAwait(false);

        using (var scope = _scopeFactory.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<IPushNotificationSender>();

            if (!sender.IsConfigured)
            {
                // Stated plainly and once. Messages stay queued rather than being marked
                // delivered, so nothing in the system claims a push that never happened.
                _logger.LogWarning(
                    "Push provider is not configured. The in-app notification centre keeps working; "
                    + "outbox messages remain pending until credentials are supplied.");
                return;
            }
        }

        _logger.LogInformation("Outbox dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dispatched = await DrainOnceAsync(stoppingToken);

                // Back off only when there was nothing to do, so a burst drains quickly.
                if (dispatched == 0)
                    await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A failure here must never kill the worker; the next tick tries again.
                _logger.LogError(ex, "Outbox dispatch cycle failed.");
                await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Outbox dispatcher stopped.");
    }

    /// <summary>Processes one batch. Exposed for tests so the loop does not have to be run.</summary>
    public async Task<int> DrainOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<IPushNotificationSender>();
        var metrics = scope.ServiceProvider.GetService<IFlowMetrics>();

        var due = await outbox.GetDueAsync(DateTimeOffset.UtcNow, _options.BatchSize, cancellationToken);
        if (due.Count == 0) return 0;

        var dispatched = 0;

        foreach (var message in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await sender.SendAsync(
                new PushNotificationRequest(
                    message.UserId, message.Title, message.Body, message.DeepLink, message.DedupeKey),
                cancellationToken);

            switch (result.Outcome)
            {
                case PushDeliveryOutcome.Delivered:
                    message.MarkDispatched();
                    metrics?.NotificationSent();
                    dispatched++;
                    break;

                case PushDeliveryOutcome.TransientFailure:
                    // Backoff with jitter, and a dead-letter once the attempts run out.
                    message.MarkFailed(result.Error ?? "Transient failure", _options.MaxAttempts);
                    metrics?.NotificationFailed("transient");
                    break;

                case PushDeliveryOutcome.PermanentFailure:
                    // No point retrying a malformed request or a rejected credential.
                    message.MarkFailed(result.Error ?? "Permanent failure", maxAttempts: 1);
                    metrics?.NotificationFailed("permanent");
                    break;

                case PushDeliveryOutcome.NotConfigured:
                    // Leave it exactly as it is: pending, and honest about it.
                    return dispatched;
            }

            await outbox.UpdateAsync(message, cancellationToken);

            if (message.Status == Domain.Enums.OutboxStatus.DeadLettered)
            {
                _logger.LogError(
                    "Outbox message {MessageId} dead-lettered after {Attempts} attempts: {Error}",
                    message.Id, message.AttemptCount, message.LastError);
            }
        }

        return dispatched;
    }
}

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
    public int MaxAttempts { get; set; } = OutboxMessage.DefaultMaxAttempts;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(5);
}
