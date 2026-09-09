namespace Flow.Domain.Enums;

public enum OutboxStatus
{
    Pending,
    Dispatched,
    Failed,
    DeadLettered
}
