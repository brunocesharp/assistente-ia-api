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
    private readonly ITaskExecutorResolver _executorResolver;
    private readonly ILogger<TaskQueuedConsumer> _logger;

    public TaskQueuedConsumer(
        AssistenteIaApiDbContext db,
        ITaskExecutorResolver executorResolver,
        ILogger<TaskQueuedConsumer> logger)
    {
        _db = db;
        _executorResolver = executorResolver;
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
            var executor = _executorResolver.Resolve(taskEntity.DomainType);
            var result = await executor.ExecuteAsync(msg.Type, taskEntity.PayloadJson, ctx.CancellationToken);
            var latency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

            attempt.CompleteSuccess(executor.GetType().Name, 0, 0, 0, latency);
            taskEntity.MarkSucceeded();
            _db.TaskArtifacts.Add(new TaskArtifact(taskEntity.Id, "text", null, result));

            await _db.SaveChangesAsync(ctx.CancellationToken);
            _logger.LogInformation("Task {TaskId} succeeded in {LatencyMs}ms.", msg.TaskId, latency);
        }
        catch (TransientAiException ex)
        {
            var latency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            attempt.CompleteFailure("AI_TRANSIENT_ERROR", ex.Message, latency);
            taskEntity.MarkFailed(ex.Message);
            await _db.SaveChangesAsync(ctx.CancellationToken);

            if (taskEntity.Status == AiTaskStatus.Queued)
            {
                await ctx.Publish(new TaskQueued(taskEntity.Id, msg.Type, taskEntity.AttemptCount, msg.CorrelationId), ctx.CancellationToken);
                _logger.LogWarning(
                    ex,
                    "Transient failure for task {TaskId}. Re-queued (attempt {AttemptCount}/{MaxAttempts}) after {LatencyMs}ms.",
                    msg.TaskId,
                    taskEntity.AttemptCount,
                    taskEntity.MaxAttempts,
                    latency);
                return;
            }

            _logger.LogWarning(
                ex,
                "Transient failure for task {TaskId}. Max attempts reached ({AttemptCount}/{MaxAttempts}); task moved to dead-letter after {LatencyMs}ms.",
                msg.TaskId,
                taskEntity.AttemptCount,
                taskEntity.MaxAttempts,
                latency);
        }
        catch (Exception ex)
        {
            var latency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            attempt.CompleteFailure("AI_EXEC_ERROR", ex.Message, latency);
            taskEntity.MarkPermanentFailure(ex.ToString());
            await _db.SaveChangesAsync(ctx.CancellationToken);
            _logger.LogError(ex, "Permanent failure for task {TaskId} after {LatencyMs}ms.", msg.TaskId, latency);
        }
    }
}
