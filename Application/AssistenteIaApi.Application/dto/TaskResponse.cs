namespace AssistenteIaApi.Application.Dto;

public class TaskResponse
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string DomainType { get; set; } = string.Empty;
    public string CapabilityType { get; set; } = string.Empty;
    public string TaskExecutionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
