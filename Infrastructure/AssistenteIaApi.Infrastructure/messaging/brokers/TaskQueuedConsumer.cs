using AssistenteIaApi.Application.Ports.Out;
using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.ValueObjects;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AssistenteIaApi.Infrastructure.Messaging.Brokers;

public class TaskQueuedConsumer : IConsumer<TaskQueued>
{
    private readonly AssistenteIaApiDbContext _dbContext;

    public TaskQueuedConsumer(AssistenteIaApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<TaskQueued> context)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == context.Message.TaskId, context.CancellationToken);
        if (task is null)
        {
            return;
        }

        if (!task.TryStartRunning($"worker-{Environment.MachineName}"))
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            return;
        }

        var attemptNo = task.AttemptCount;
        var attemptAlreadyExists = await _dbContext.TaskAttempts.AnyAsync(
            x => x.TaskId == task.Id && x.AttemptNo == attemptNo,
            context.CancellationToken);

        if (attemptAlreadyExists)
        {
            return;
        }

        var attempt = new TaskAttempt(task.Id, attemptNo);
        _dbContext.TaskAttempts.Add(attempt);

        var start = DateTimeOffset.UtcNow;
        await Task.Delay(200, context.CancellationToken);

        var forceFail = task.PayloadJson.Contains("\"forceFail\":true", StringComparison.OrdinalIgnoreCase);

        if (forceFail)
        {
            var latency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            attempt.CompleteFailure("AI_EXEC_ERROR", "Execution failed by forceFail payload flag.", latency);
            task.MarkFailed("Execution failed by forceFail payload flag.");

            if (task.Status == AiTaskStatus.Queued)
            {
                var backoffSeconds = Math.Min(30, (int)Math.Pow(2, context.Message.Attempt + 1));
                await _dbContext.SaveChangesAsync(context.CancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), context.CancellationToken);

                await context.Publish(
                    new TaskQueued(task.Id, context.Message.Type, context.Message.Attempt + 1, context.Message.CorrelationId),
                    context.CancellationToken);

                return;
            }

            await _dbContext.SaveChangesAsync(context.CancellationToken);
            return;
        }

        var successLatency = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
        attempt.CompleteSuccess("mock-llm", 120, 340, 0.0007m, successLatency);
        task.MarkSucceeded();

        _dbContext.TaskArtifacts.Add(new TaskArtifact(
            task.Id,
            "text",
            null,
            $"Task {task.Id} processed successfully at {DateTimeOffset.UtcNow:O}."));

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
