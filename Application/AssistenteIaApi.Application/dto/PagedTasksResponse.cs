namespace AssistenteIaApi.Application.Dto;

public class PagedTasksResponse
{
    public IReadOnlyList<TaskResponse> Items { get; set; } = Array.Empty<TaskResponse>();
    public int TotalCount { get; set; }
}
