namespace AssistenteIaApi.Application.Dto;

public class CreateTaskRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string DomainType { get; set; } = string.Empty;
    public string CapabilityType { get; set; } = string.Empty;
    public string TaskExecutionType { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public string PayloadJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset? ScheduledAt { get; set; }
    public int MaxAttempts { get; set; } = 3;
}
