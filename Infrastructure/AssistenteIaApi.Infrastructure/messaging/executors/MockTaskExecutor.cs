using AssistenteIaApi.Application.Ports.Out;

namespace AssistenteIaApi.Infrastructure.Messaging.Executors;

public sealed class MockTaskExecutor : ITaskExecutor
{
    public Task<string> ExecuteAsync(string type, string payloadJson, CancellationToken cancellationToken = default)
    {
        if (payloadJson.Contains("\"forceTransientFail\":true", StringComparison.OrdinalIgnoreCase))
        {
            throw new TransientAiException("Temporary AI provider error.");
        }

        if (payloadJson.Contains("\"forceFail\":true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Execution failed by forceFail payload flag.");
        }

        var result = $"Task type {type} processed successfully at {DateTimeOffset.UtcNow:O}.";
        return Task.FromResult(result);
    }
}
