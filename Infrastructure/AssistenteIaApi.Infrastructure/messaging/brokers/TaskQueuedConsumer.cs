using AssistenteIaApi.Application.Ports.Out;
using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.ValueObjects;
using AssistenteIaApi.Infrastructure.Messaging.Executors;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssistenteIaApi.Infrastructure.Messaging.Brokers;

public class TaskQueuedConsumer : IConsumer<TaskQueued>
{
    private readonly AssistenteIaApiDbContext _db;
    private readonly ITaskExecutor _executor;
    private readonly ILogger<TaskQueuedConsumer> _logger;

    public TaskQueuedConsumer(
        AssistenteIaApiDbContext db,
        ITaskExecutor executor,
        ILogger<TaskQueuedConsumer> logger)
    {
        _db = db;
        _executor = executor;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskQueued> ctx)
    {
        var msg = ctx.Message;
        var workerId = Environment.MachineName;
        var now = DateTimeOffset.UtcNow;
        var lease = now.AddMinutes(10);
        _logger.LogInformation("Consuming task message. TaskId: {TaskId}, Attempt: {Attempt}, Worker: {WorkerId}", msg.TaskId, msg.Attempt, workerId);

        var claimed = await _db.Tasks
            .Where(t => t.Id == msg.TaskId
                     && t.Status == AiTaskStatus.Queued
                     && (t.LockedUntil == null || t.LockedUntil < now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, AiTaskStatus.Running)
                .SetProperty(t => t.LockedBy, workerId)
                .SetProperty(t => t.LockedUntil, lease)
                .SetProperty(t => t.AttemptCount, t => t.AttemptCount + 1)
                .SetProperty(t => t.UpdatedAt, now), ctx.CancellationToken);

        if (claimed == 0)
        {
            _logger.LogInformation("Task {TaskId} not claimed (already running or done).", msg.TaskId);
            return;
        }

        var taskEntity = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == msg.TaskId, ctx.CancellationToken);
        if (taskEntity is null)
        {
            return;
        }

        var attempt = new TaskAttempt(taskEntity.Id, taskEntity.AttemptCount);
        _db.TaskAttempts.Add(attempt);
        var start = DateTimeOffset.UtcNow;

        try
        {
            var result = await _executor.ExecuteAsync(msg.Type, taskEntity.PayloadJson, ctx.CancellationToken);
            var latency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

            attempt.CompleteSuccess("mock-executor", 0, 0, 0, latency);
            taskEntity.MarkSucceeded();
            _db.TaskArtifacts.Add(new TaskArtifact(taskEntity.Id, "text", null, result));

            await _db.SaveChangesAsync(ctx.CancellationToken);
            _logger.LogInformation("Task {TaskId} succeeded in {LatencyMs}ms.", msg.TaskId, latency);
        }
        catch (TransientAiException ex)
        {
            var latency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            attempt.CompleteFailure("AI_TRANSIENT_ERROR", ex.Message, latency);
            await MarkRetryAsync(msg.TaskId, ex.Message, ctx.CancellationToken);
            await _db.SaveChangesAsync(ctx.CancellationToken);
            _logger.LogWarning(ex, "Transient failure for task {TaskId}. Re-queued after {LatencyMs}ms.", msg.TaskId, latency);
            throw;
        }
        catch (Exception ex)
        {
            var latency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            attempt.CompleteFailure("AI_EXEC_ERROR", ex.Message, latency);
            await MarkFailedAsync(msg.TaskId, ex.ToString(), ctx.CancellationToken);
            await _db.SaveChangesAsync(ctx.CancellationToken);
            _logger.LogError(ex, "Permanent failure for task {TaskId} after {LatencyMs}ms.", msg.TaskId, latency);
            throw;
        }
    }

    private async Task MarkRetryAsync(Guid taskId, string error, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.Tasks.Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, AiTaskStatus.Queued)
                .SetProperty(t => t.LastError, error)
                .SetProperty(t => t.LockedUntil, (DateTimeOffset?)null)
                .SetProperty(t => t.LockedBy, (string?)null)
                .SetProperty(t => t.UpdatedAt, now), ct);
    }

    private async Task MarkFailedAsync(Guid taskId, string error, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.Tasks.Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, AiTaskStatus.Failed)
                .SetProperty(t => t.LastError, error)
                .SetProperty(t => t.LockedUntil, (DateTimeOffset?)null)
                .SetProperty(t => t.LockedBy, (string?)null)
                .SetProperty(t => t.UpdatedAt, now), ct);
    }
}
