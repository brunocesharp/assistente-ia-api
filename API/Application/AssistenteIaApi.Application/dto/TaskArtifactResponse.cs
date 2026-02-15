namespace AssistenteIaApi.Application.Dto;

public class TaskArtifactResponse
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? Uri { get; set; }
    public string? Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
