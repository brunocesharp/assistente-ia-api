using AssistenteIaApi.Application.Dto;

namespace AssistenteIaApi.Application.Ports.In;

public interface ITaskAppService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedTasksResponse> ListAsync(ListTasksQuery query, CancellationToken cancellationToken = default);
    Task<TaskResponse?> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskAttemptResponse>> ListAttemptsAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskArtifactResponse>> ListArtifactsAsync(Guid taskId, CancellationToken cancellationToken = default);
}
