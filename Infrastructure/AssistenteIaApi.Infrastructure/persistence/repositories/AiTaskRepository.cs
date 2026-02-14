using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.Repositories;
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
