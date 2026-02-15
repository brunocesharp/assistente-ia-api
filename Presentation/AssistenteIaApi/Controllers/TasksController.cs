using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Controllers.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AssistenteIaApi.Controllers;

[ApiController]
[Route("api/v1/tasks")]
public sealed class TasksController(ITaskAppService service) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTaskHttpRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Idempotency-Key header is required.");
        }

        var command = new CreateTaskRequest
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId,
            DomainType = request.DomainType,
            CapabilityType = string.IsNullOrWhiteSpace(request.CapabilityType) ? request.Type ?? string.Empty : request.CapabilityType,
            TaskExecutionType = request.TaskExecutionType,
            Priority = request.Priority,
            PayloadJson = JsonSerializer.Serialize(request.Payload),
            IdempotencyKey = idempotencyKey,
            ScheduledAt = request.ScheduledAt,
            MaxAttempts = request.MaxAttempts
        };

        try
        {
            var created = await service.CreateAsync(command, cancellationToken);
            return Accepted($"/api/v1/tasks/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await service.GetByIdAsync(id, cancellationToken);
        return task is null ? ProblemResponse(StatusCodes.Status404NotFound, "Task not found.") : Ok(task);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync([FromQuery] ListTasksHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(new ListTasksQuery
        {
            Status = request.Status,
            DomainType = request.DomainType,
            CapabilityType = request.CapabilityType,
            TaskExecutionType = request.TaskExecutionType,
            Type = request.Type,
            Page = request.Page ?? 1,
            PageSize = request.PageSize ?? 20
        }, cancellationToken);

        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        return Ok(result.Items);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await service.CancelAsync(id, cancellationToken);
        return task is null
            ? ProblemResponse(StatusCodes.Status404NotFound, "Task not found.")
            : Accepted($"/api/v1/tasks/{id}", task);
    }

    [HttpGet("{id:guid}/attempts")]
    [ProducesResponseType<IReadOnlyList<TaskAttemptResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAttemptsAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await service.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return ProblemResponse(StatusCodes.Status404NotFound, "Task not found.");
        }

        var attempts = await service.ListAttemptsAsync(id, cancellationToken);
        return Ok(attempts);
    }

    [HttpGet("{id:guid}/artifacts")]
    [ProducesResponseType<IReadOnlyList<TaskArtifactResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListArtifactsAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await service.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return ProblemResponse(StatusCodes.Status404NotFound, "Task not found.");
        }

        var artifacts = await service.ListArtifactsAsync(id, cancellationToken);
        return Ok(artifacts);
    }

    private ObjectResult ProblemResponse(int statusCode, string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = "Request could not be processed",
            Detail = detail,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        })
        {
            StatusCode = statusCode
        };
    }
}
