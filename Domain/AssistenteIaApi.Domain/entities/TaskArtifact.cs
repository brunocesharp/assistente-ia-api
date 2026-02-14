namespace AssistenteIaApi.Domain.Entities;

public class TaskArtifact : Entity
{
    public Guid TaskId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string? Uri { get; private set; }
    public string? Content { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public AiTask? Task { get; private set; }

    private TaskArtifact()
    {
    }

    public TaskArtifact(Guid taskId, string kind, string? uri, string? content)
    {
        TaskId = taskId;
        Kind = kind;
        Uri = uri;
        Content = content;
    }
}
