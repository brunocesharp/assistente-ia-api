using AssistenteIaApi.Application;
using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Infrastructure;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AssistenteIaApiDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "API no ar");
app.MapPost("/api/v1/tasks", async (
    CreateTaskHttpRequest request,
    HttpContext httpContext,
    ITaskAppService service,
    CancellationToken cancellationToken) =>
{
    if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) || string.IsNullOrWhiteSpace(idempotencyKey))
    {
        return Problem(400, "Idempotency-Key header is required.", httpContext);
    }

    var tenantId = httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader)
        ? tenantHeader.ToString()
        : "default";

    var command = new CreateTaskRequest
    {
        TenantId = tenantId,
        Type = request.Type,
        Priority = request.Priority,
        PayloadJson = JsonSerializer.Serialize(request.Payload),
        IdempotencyKey = idempotencyKey.ToString(),
        ScheduledAt = request.ScheduledAt,
        MaxAttempts = request.MaxAttempts
    };

    var created = await service.CreateAsync(command, cancellationToken);
    return Results.Accepted($"/api/v1/tasks/{created.Id}", created);
});

app.MapGet("/api/v1/tasks/{id:guid}", async (Guid id, HttpContext httpContext, ITaskAppService service, CancellationToken cancellationToken) =>
{
    var task = await service.GetByIdAsync(id, cancellationToken);
    return task is null ? Problem(404, "Task not found.", httpContext) : Results.Ok(task);
});

app.MapGet("/api/v1/tasks", async (
    HttpContext httpContext,
    ITaskAppService service,
    string? status,
    string? type,
    int? page,
    int? pageSize,
    CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(new ListTasksQuery
    {
        Status = status,
        Type = type,
        Page = page ?? 1,
        PageSize = pageSize ?? 20
    }, cancellationToken);

    httpContext.Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
    return Results.Ok(result.Items);
});

app.MapPost("/api/v1/tasks/{id:guid}/cancel", async (Guid id, HttpContext httpContext, ITaskAppService service, CancellationToken cancellationToken) =>
{
    var task = await service.CancelAsync(id, cancellationToken);
    return task is null
        ? Problem(404, "Task not found.", httpContext)
        : Results.Accepted($"/api/v1/tasks/{id}", task);
});

app.MapGet("/api/v1/tasks/{id:guid}/attempts", async (Guid id, HttpContext httpContext, ITaskAppService service, CancellationToken cancellationToken) =>
{
    var task = await service.GetByIdAsync(id, cancellationToken);
    if (task is null)
    {
        return Problem(404, "Task not found.", httpContext);
    }

    var attempts = await service.ListAttemptsAsync(id, cancellationToken);
    return Results.Ok(attempts);
});

app.MapGet("/api/v1/tasks/{id:guid}/artifacts", async (Guid id, HttpContext httpContext, ITaskAppService service, CancellationToken cancellationToken) =>
{
    var task = await service.GetByIdAsync(id, cancellationToken);
    if (task is null)
    {
        return Problem(404, "Task not found.", httpContext);
    }

    var artifacts = await service.ListArtifactsAsync(id, cancellationToken);
    return Results.Ok(artifacts);
});

app.Run();

static IResult Problem(int statusCode, string detail, HttpContext context)
{
    return Results.Problem(
        statusCode: statusCode,
        title: "Request could not be processed",
        detail: detail,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = context.TraceIdentifier
        });
}

public class CreateTaskHttpRequest
{
    public string Type { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public JsonElement Payload { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public int MaxAttempts { get; set; } = 3;
}
