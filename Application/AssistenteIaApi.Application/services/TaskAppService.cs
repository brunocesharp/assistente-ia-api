using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.Repositories;

namespace AssistenteIaApi.Application.Services;

public class TaskAppService : ITaskAppService
{
    private readonly IAiTaskRepository _taskRepository;

    public TaskAppService(IAiTaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = new AiTask(
            request.TenantId,
            request.Type,
            request.Priority,
            request.PayloadJson,
            request.IdempotencyKey,
            request.ScheduledAt,
            request.MaxAttempts);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(task);
    }

    public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return task is null ? null : ToResponse(task);
    }

    private static TaskResponse ToResponse(AiTask task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            TenantId = task.TenantId,
            Type = task.Type,
            Status = task.Status.ToString(),
            AttemptCount = task.AttemptCount,
            MaxAttempts = task.MaxAttempts,
            LastError = task.LastError,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
