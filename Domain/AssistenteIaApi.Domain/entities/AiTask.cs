using AssistenteIaApi.Domain.ValueObjects;

namespace AssistenteIaApi.Domain.Entities;

public class AiTask : Entity
{
    public string TenantId { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public AiTaskStatus Status { get; private set; } = AiTaskStatus.Created;
    public string PayloadJson { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }
    public int MaxAttempts { get; private set; } = 3;
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public ICollection<TaskAttempt> Attempts { get; private set; } = new List<TaskAttempt>();
    public ICollection<TaskArtifact> Artifacts { get; private set; } = new List<TaskArtifact>();
    public ICollection<OutboxMessage> OutboxMessages { get; private set; } = new List<OutboxMessage>();

    private AiTask()
    {
    }

    public AiTask(
        string tenantId,
        string type,
        int priority,
        string payloadJson,
        string idempotencyKey,
        DateTimeOffset? scheduledAt,
        int maxAttempts)
    {
        TenantId = tenantId;
        Type = type;
        Priority = priority;
        PayloadJson = payloadJson;
        IdempotencyKey = idempotencyKey;
        ScheduledAt = scheduledAt;
        MaxAttempts = maxAttempts;
        Status = AiTaskStatus.Queued;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(AiTaskStatus status, string? error = null)
    {
        Status = status;
        LastError = error;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
