namespace AssistenteIaApi.Application.Dto;

public class TaskAttemptResponse
{
    public Guid Id { get; set; }
    public int AttemptNo { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int? TokensIn { get; set; }
    public int? TokensOut { get; set; }
    public decimal? Cost { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDetail { get; set; }
}
