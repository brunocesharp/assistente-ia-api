namespace AssistenteIaApi.Controllers.Models;

public sealed class ListTasksHttpRequest
{
    public string? Status { get; set; }
    public string? DomainType { get; set; }
    public string? CapabilityType { get; set; }
    public string? TaskExecutionType { get; set; }
    public string? Type { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
