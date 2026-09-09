namespace Flow.Application.Common.Interfaces;

// No-op in MVP. Swap DI registration to enable push or email notifications.
public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default);
}
