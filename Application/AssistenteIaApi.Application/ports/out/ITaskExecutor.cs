namespace AssistenteIaApi.Application.Ports.Out;

public interface ITaskExecutor
{
    Task<string> ExecuteAsync(string type, string payloadJson, CancellationToken cancellationToken = default);
}
