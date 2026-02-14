using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.Repositories;
using AssistenteIaApi.Domain.ValueObjects;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using Microsoft.EntityFrameworkCore;

namespace AssistenteIaApi.Infrastructure.Persistence.Repositories;

public class AiTaskRepository : IAiTaskRepository
{
    private readonly AssistenteIaApiDbContext _dbContext;

    public AiTaskRepository(AssistenteIaApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(AiTask task, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tasks.AddAsync(task, cancellationToken).AsTask();
    }

    public Task<AiTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<AiTask?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tasks
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<AiTask?> GetByTenantAndIdempotencyAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<AiTask> Items, int TotalCount)> ListAsync(
        AiTaskStatus? status,
        DomainType? domainType,
        CapabilityType? capabilityType,
        TaskExecutionType? taskExecutionType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tasks.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (domainType.HasValue)
        {
            query = query.Where(x => x.DomainType == domainType.Value);
        }

        if (capabilityType.HasValue)
        {
            query = query.Where(x => x.CapabilityType == capabilityType.Value);
        }

        if (taskExecutionType.HasValue)
        {
            query = query.Where(x => x.TaskExecutionType == taskExecutionType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<TaskAttempt>> ListAttemptsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaskAttempts
            .AsNoTracking()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.AttemptNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskArtifact>> ListArtifactsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaskArtifacts
            .AsNoTracking()
            .Where(x => x.TaskId == taskId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
