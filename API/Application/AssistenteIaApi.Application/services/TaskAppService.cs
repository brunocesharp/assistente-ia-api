using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Application.Ports.Out;
using AssistenteIaApi.Application.Validation;
using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.Repositories;
using AssistenteIaApi.Domain.ValueObjects;

namespace AssistenteIaApi.Application.Services;

public class TaskAppService : ITaskAppService
{
    private readonly IAiTaskRepository _taskRepository;
    private readonly ITaskQueuePublisher _queuePublisher;

    public TaskAppService(IAiTaskRepository taskRepository, ITaskQueuePublisher queuePublisher)
    {
        _taskRepository = taskRepository;
        _queuePublisher = queuePublisher;
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        TaskRequestValidator.ValidateCreate(request);

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
            ParseRequiredDomainType(request.DomainType),
            ParseRequiredCapabilityType(request.CapabilityType),
            ParseRequiredTaskExecutionType(request.TaskExecutionType),
            request.Priority,
            request.PayloadJson,
            request.IdempotencyKey,
            request.ScheduledAt,
            request.MaxAttempts);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        await _queuePublisher.PublishAsync(
            new TaskQueued(task.Id, task.CapabilityType.ToString(), 0, Guid.NewGuid().ToString("N")),
            cancellationToken);

        return ToResponse(task);
    }

    public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return task is null ? null : ToResponse(task);
    }

    public async Task<PagedTasksResponse> ListAsync(ListTasksQuery query, CancellationToken cancellationToken = default)
    {
        TaskRequestValidator.ValidateList(query);

        var page = query.Page;
        var pageSize = query.PageSize;
        var parsedStatus = ParseStatus(query.Status);
        var parsedDomainType = ParseDomainType(query.DomainType);
        var parsedCapabilityType = ParseCapabilityType(query.CapabilityType ?? query.Type);
        var parsedTaskExecutionType = ParseTaskExecutionType(query.TaskExecutionType);

        var result = await _taskRepository.ListAsync(
            parsedStatus,
            parsedDomainType,
            parsedCapabilityType,
            parsedTaskExecutionType,
            page,
            pageSize,
            cancellationToken);

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

    private static DomainType? ParseDomainType(string? domainType)
    {
        if (string.IsNullOrWhiteSpace(domainType))
        {
            return null;
        }

        return Enum.TryParse<DomainType>(domainType, true, out var parsed) ? parsed : null;
    }

    private static DomainType ParseRequiredDomainType(string? domainType)
    {
        var parsed = ParseDomainType(domainType);
        if (!parsed.HasValue)
        {
            throw new ArgumentException("DomainType is required and must be valid.");
        }

        return parsed.Value;
    }

    private static CapabilityType? ParseCapabilityType(string? capabilityType)
    {
        if (string.IsNullOrWhiteSpace(capabilityType))
        {
            return null;
        }

        return Enum.TryParse<CapabilityType>(capabilityType, true, out var parsed) ? parsed : null;
    }

    private static CapabilityType ParseRequiredCapabilityType(string? capabilityType)
    {
        var parsed = ParseCapabilityType(capabilityType);
        if (!parsed.HasValue)
        {
            throw new ArgumentException("CapabilityType is required and must be valid.");
        }

        return parsed.Value;
    }

    private static TaskExecutionType? ParseTaskExecutionType(string? taskExecutionType)
    {
        if (string.IsNullOrWhiteSpace(taskExecutionType))
        {
            return null;
        }

        return Enum.TryParse<TaskExecutionType>(taskExecutionType, true, out var parsed) ? parsed : null;
    }

    private static TaskExecutionType ParseRequiredTaskExecutionType(string? taskExecutionType)
    {
        var parsed = ParseTaskExecutionType(taskExecutionType);
        if (!parsed.HasValue)
        {
            throw new ArgumentException("TaskExecutionType is required and must be valid.");
        }

        return parsed.Value;
    }

    private static TaskResponse ToResponse(AiTask task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            TenantId = task.TenantId,
            DomainType = task.DomainType.ToString(),
            CapabilityType = task.CapabilityType.ToString(),
            TaskExecutionType = task.TaskExecutionType.ToString(),
            Status = task.Status.ToString(),
            AttemptCount = task.AttemptCount,
            MaxAttempts = task.MaxAttempts,
            LastError = task.LastError,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
