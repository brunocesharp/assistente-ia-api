using System.Text.Json;

namespace AssistenteIaApi.Controllers.Models;

public sealed class CreateTaskHttpRequest
{
    public string? Type { get; set; }
    public string DomainType { get; set; } = string.Empty;
    public string CapabilityType { get; set; } = string.Empty;
    public string TaskExecutionType { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public JsonElement Payload { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public int MaxAttempts { get; set; } = 3;
}
