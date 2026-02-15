using AssistenteIaApi.Application.Ports.Out;
using AssistenteIaApi.Domain.ValueObjects;

namespace AssistenteIaApi.Infrastructure.Messaging.Executors;

internal sealed class TaskExecutorResolver : ITaskExecutorResolver
{
    private readonly IReadOnlyDictionary<DomainType, ITaskExecutor> _executorsByDomain;

    public TaskExecutorResolver(IEnumerable<ITaskExecutor> executors)
    {
        _executorsByDomain = executors.ToDictionary(x => x.DomainType, x => x);
    }

    public ITaskExecutor Resolve(DomainType domainType)
    {
        if (_executorsByDomain.TryGetValue(domainType, out var executor))
        {
            return executor;
        }

        throw new InvalidOperationException($"No task executor registered for domain '{domainType}'.");
    }
}

