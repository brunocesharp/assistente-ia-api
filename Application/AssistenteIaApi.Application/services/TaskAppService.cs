using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.Repositories;
using AssistenteIaApi.Domain.ValueObjects;

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
        var existing = await _taskRepository.GetByTenantAndIdempotencyAsync(
            request.TenantId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return ToResponse(existing);
        }

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

    public async Task<PagedTasksResponse> ListAsync(ListTasksQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;
        var parsedStatus = ParseStatus(query.Status);

        var result = await _taskRepository.ListAsync(parsedStatus, query.Type, page, pageSize, cancellationToken);

        return new PagedTasksResponse
        {
            Items = result.Items.Select(ToResponse).ToList(),
            TotalCount = result.TotalCount
        };
    }

    public async Task<TaskResponse?> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetTrackedByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.TryCancel();
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(task);
    }

    public async Task<IReadOnlyList<TaskAttemptResponse>> ListAttemptsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var attempts = await _taskRepository.ListAttemptsByTaskIdAsync(taskId, cancellationToken);
        return attempts.Select(x => new TaskAttemptResponse
        {
            Id = x.Id,
            AttemptNo = x.AttemptNo,
            StartedAt = x.StartedAt,
            EndedAt = x.EndedAt,
            Status = x.Status.ToString(),
            Model = x.Model,
            TokensIn = x.TokensIn,
            TokensOut = x.TokensOut,
            Cost = x.Cost,
            LatencyMs = x.LatencyMs,
            ErrorCode = x.ErrorCode,
            ErrorDetail = x.ErrorDetail
        }).ToList();
    }

    public async Task<IReadOnlyList<TaskArtifactResponse>> ListArtifactsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var artifacts = await _taskRepository.ListArtifactsByTaskIdAsync(taskId, cancellationToken);
        return artifacts.Select(x => new TaskArtifactResponse
        {
            Id = x.Id,
            Kind = x.Kind,
            Uri = x.Uri,
            Content = x.Content,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    private static AiTaskStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse<AiTaskStatus>(status, true, out var parsed) ? parsed : null;
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
