using AssistenteIaApi.Domain.ValueObjects;

namespace AssistenteIaApi.Domain.Entities;

public class TaskAttempt : Entity
{
    public Guid TaskId { get; private set; }
    public int AttemptNo { get; private set; }
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; private set; }
    public TaskAttemptStatus Status { get; private set; } = TaskAttemptStatus.Running;
    public string? Model { get; private set; }
    public int? TokensIn { get; private set; }
    public int? TokensOut { get; private set; }
    public decimal? Cost { get; private set; }
    public int? LatencyMs { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorDetail { get; private set; }

    public AiTask? Task { get; private set; }

    private TaskAttempt()
    {
    }

    public TaskAttempt(Guid taskId, int attemptNo)
    {
        TaskId = taskId;
        AttemptNo = attemptNo;
    }

    public void CompleteSuccess(string model, int tokensIn, int tokensOut, decimal cost, int latencyMs)
    {
        Model = model;
        TokensIn = tokensIn;
        TokensOut = tokensOut;
        Cost = cost;
        LatencyMs = latencyMs;
        Status = TaskAttemptStatus.Succeeded;
        EndedAt = DateTimeOffset.UtcNow;
    }

    public void CompleteFailure(string errorCode, string errorDetail, int latencyMs)
    {
        ErrorCode = errorCode;
        ErrorDetail = errorDetail;
        LatencyMs = latencyMs;
        Status = TaskAttemptStatus.Failed;
        EndedAt = DateTimeOffset.UtcNow;
    }
}
