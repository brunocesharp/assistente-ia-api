namespace AssistenteIaApi.Domain.Entities;

public class OutboxMessage : Entity
{
    public Guid? TaskId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; private set; }

    public AiTask? Task { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(Guid? taskId, string eventType, string payloadJson)
    {
        TaskId = taskId;
        EventType = eventType;
        PayloadJson = payloadJson;
    }
}
