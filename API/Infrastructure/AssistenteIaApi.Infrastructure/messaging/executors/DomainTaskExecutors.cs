using AssistenteIaApi.Application.Ports.Out;
using AssistenteIaApi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AssistenteIaApi.Infrastructure.Messaging.Executors;

internal abstract class DomainTaskExecutorBase : ITaskExecutor
{
    private readonly ILogger _logger;

    protected DomainTaskExecutorBase(ILogger logger)
    {
        _logger = logger;
    }

    public abstract DomainType DomainType { get; }

    public Task<string> ExecuteAsync(string type, string payloadJson, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing task. DomainType: {DomainType}, Type: {Type}", DomainType, type);

        if (payloadJson.Contains("\"forceTransientFail\":true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Transient failure requested by payload. DomainType: {DomainType}, Type: {Type}", DomainType, type);
            throw new TransientAiException("Temporary AI provider error.");
        }

        if (payloadJson.Contains("\"forceFail\":true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Permanent failure requested by payload. DomainType: {DomainType}, Type: {Type}", DomainType, type);
            throw new InvalidOperationException("Execution failed by forceFail payload flag.");
        }

        var result = $"Domain {DomainType} processed task type {type} successfully at {DateTimeOffset.UtcNow:O}.";
        _logger.LogInformation("Task execution completed. DomainType: {DomainType}, Type: {Type}", DomainType, type);

        return Task.FromResult(result);
    }
}

internal sealed class DocumentProcessingTaskExecutor : DomainTaskExecutorBase
{
    public DocumentProcessingTaskExecutor(ILogger<DocumentProcessingTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.DocumentProcessing;
}

internal sealed class CustomerSupportTaskExecutor : DomainTaskExecutorBase
{
    public CustomerSupportTaskExecutor(ILogger<CustomerSupportTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.CustomerSupport;
}

internal sealed class ComplianceCheckTaskExecutor : DomainTaskExecutorBase
{
    public ComplianceCheckTaskExecutor(ILogger<ComplianceCheckTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.ComplianceCheck;
}

internal sealed class ContentCreationTaskExecutor : DomainTaskExecutorBase
{
    public ContentCreationTaskExecutor(ILogger<ContentCreationTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.ContentCreation;
}

internal sealed class DataAnalysisTaskExecutor : DomainTaskExecutorBase
{
    public DataAnalysisTaskExecutor(ILogger<DataAnalysisTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.DataAnalysis;
}

internal sealed class CodeAutomationTaskExecutor : DomainTaskExecutorBase
{
    public CodeAutomationTaskExecutor(ILogger<CodeAutomationTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.CodeAutomation;
}

internal sealed class DecisionAutomationTaskExecutor : DomainTaskExecutorBase
{
    public DecisionAutomationTaskExecutor(ILogger<DecisionAutomationTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.DecisionAutomation;
}

internal sealed class MonitoringAlertTaskExecutor : DomainTaskExecutorBase
{
    public MonitoringAlertTaskExecutor(ILogger<MonitoringAlertTaskExecutor> logger) : base(logger)
    {
    }

    public override DomainType DomainType => DomainType.MonitoringAlert;
}
