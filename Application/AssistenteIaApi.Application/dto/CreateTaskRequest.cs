namespace AssistenteIaApi.Application.Dto;

public class CreateTaskRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public string PayloadJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset? ScheduledAt { get; set; }
    public int MaxAttempts { get; set; } = 3;
}
