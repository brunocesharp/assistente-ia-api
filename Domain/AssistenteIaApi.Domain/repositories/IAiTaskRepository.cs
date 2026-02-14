using AssistenteIaApi.Domain.Entities;

namespace AssistenteIaApi.Domain.Repositories;

public interface IAiTaskRepository
{
    Task AddAsync(AiTask task, CancellationToken cancellationToken = default);
    Task<AiTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
