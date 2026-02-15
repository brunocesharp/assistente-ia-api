namespace AssistenteIaApi.Application.Dto;

public class ListTasksQuery
{
    public string? Status { get; set; }
    public string? DomainType { get; set; }
    public string? CapabilityType { get; set; }
    public string? TaskExecutionType { get; set; }
    public string? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
