using AssistenteIaApi.Domain.ValueObjects;

namespace AssistenteIaApi.Application.Ports.Out;

public interface ITaskExecutor
{
    DomainType DomainType { get; }
    Task<string> ExecuteAsync(string type, string payloadJson, CancellationToken cancellationToken = default);
}

public interface ITaskExecutorResolver
{
    ITaskExecutor Resolve(DomainType domainType);
}
