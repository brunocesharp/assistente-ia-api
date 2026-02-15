using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Domain.ValueObjects;

namespace AssistenteIaApi.Application.Validation;

internal static class TaskRequestValidator
{
    public static void ValidateCreate(CreateTaskRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            throw new ArgumentException("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ArgumentException("Idempotency-Key header is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            throw new ArgumentException("Payload is required.");
        }

        if (request.MaxAttempts is < 1 or > 10)
        {
            throw new ArgumentException("MaxAttempts must be between 1 and 10.");
        }
    }

    public static void ValidateList(ListTasksQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1)
        {
            throw new ArgumentException("Page must be greater than or equal to 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        ValidateOptionalEnum<AiTaskStatus>(query.Status, "Status");
        ValidateOptionalEnum<DomainType>(query.DomainType, "DomainType");
        ValidateOptionalEnum<CapabilityType>(query.CapabilityType ?? query.Type, "CapabilityType");
        ValidateOptionalEnum<TaskExecutionType>(query.TaskExecutionType, "TaskExecutionType");
    }

    private static void ValidateOptionalEnum<TEnum>(string? rawValue, string fieldName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        if (!Enum.TryParse<TEnum>(rawValue, true, out _))
        {
            throw new ArgumentException($"{fieldName} is invalid.");
        }
    }
}
