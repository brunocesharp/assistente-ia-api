using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.ValueObjects;

namespace AssistenteIaApi.Domain.Repositories;

public interface IAiTaskRepository
{
    Task AddAsync(AiTask task, CancellationToken cancellationToken = default);
    Task<AiTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AiTask?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AiTask?> GetByTenantAndIdempotencyAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AiTask> Items, int TotalCount)> ListAsync(
        AiTaskStatus? status,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskAttempt>> ListAttemptsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskArtifact>> ListArtifactsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
