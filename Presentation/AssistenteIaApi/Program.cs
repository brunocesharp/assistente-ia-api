using AssistenteIaApi.Application;
using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Infrastructure;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddTaskQueuePublisher(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assistente IA API",
        Version = "v1",
        Description = "API para orquestracao de tarefas de IA."
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AssistenteIaApiDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Assistente IA API v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => "API no ar")
    .WithSummary("Health check simples da API")
    .WithOpenApi();

var tasksGroup = app.MapGroup("/api/v1/tasks")
    .WithTags("Tasks");

tasksGroup.MapPost("", async (
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
        DomainType = request.DomainType,
        CapabilityType = string.IsNullOrWhiteSpace(request.CapabilityType) ? request.Type ?? string.Empty : request.CapabilityType,
        TaskExecutionType = request.TaskExecutionType,
        Priority = request.Priority,
        PayloadJson = JsonSerializer.Serialize(request.Payload),
        IdempotencyKey = idempotencyKey.ToString(),
        ScheduledAt = request.ScheduledAt,
        MaxAttempts = request.MaxAttempts
    };

    try
    {
        var created = await service.CreateAsync(command, cancellationToken);
        return Results.Accepted($"/api/v1/tasks/{created.Id}", created);
    }
    catch (ArgumentException ex)
    {
        return Problem(400, ex.Message, httpContext);
    }
})
.WithName("CreateTask")
.WithSummary("Cria uma nova task")
.WithDescription("Cria uma task de IA com DomainType, CapabilityType e TaskExecutionType, com idempotencia por tenant.")
.Accepts<CreateTaskHttpRequest>("application/json")
.Produces<TaskResponse>(StatusCodes.Status202Accepted)
.ProducesProblem(StatusCodes.Status400BadRequest)
.WithOpenApi(operation =>
{
    operation.Parameters.Add(new OpenApiParameter
    {
        Name = "Idempotency-Key",
        In = ParameterLocation.Header,
        Required = true,
        Description = "Chave de idempotencia da requisicao.",
        Schema = new OpenApiSchema { Type = "string" }
    });

    operation.Parameters.Add(new OpenApiParameter
    {
        Name = "X-Tenant-Id",
        In = ParameterLocation.Header,
        Required = false,
        Description = "Identificador do tenant (padrao: default).",
        Schema = new OpenApiSchema { Type = "string" }
    });

    return operation;
});

tasksGroup.MapGet("/{id:guid}", async (
    Guid id,
    HttpContext httpContext,
    ITaskAppService service,
    CancellationToken cancellationToken) =>
{
    var task = await service.GetByIdAsync(id, cancellationToken);
    return task is null ? Problem(404, "Task not found.", httpContext) : Results.Ok(task);
})
.WithName("GetTaskById")
.WithSummary("Consulta task por id")
.Produces<TaskResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

tasksGroup.MapGet("", async (
    HttpContext httpContext,
    ITaskAppService service,
    string? status,
    string? domainType,
    string? capabilityType,
    string? taskExecutionType,
    string? type,
    int? page,
    int? pageSize,
    CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(new ListTasksQuery
    {
        Status = status,
        DomainType = domainType,
        CapabilityType = capabilityType,
        TaskExecutionType = taskExecutionType,
        Type = type,
        Page = page ?? 1,
        PageSize = pageSize ?? 20
    }, cancellationToken);

    httpContext.Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
    return Results.Ok(result.Items);
})
.WithName("ListTasks")
.WithSummary("Lista tasks")
.WithDescription("Lista tasks com filtros de status, domainType, capabilityType, taskExecutionType e paginacao.")
.Produces<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK)
.WithOpenApi(operation =>
{
    if (operation.Responses.TryGetValue("200", out var response))
    {
        response.Headers ??= new Dictionary<string, OpenApiHeader>();
        response.Headers["X-Total-Count"] = new OpenApiHeader
        {
            Description = "Quantidade total de registros para a consulta.",
            Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
        };
    }

    return operation;
});

tasksGroup.MapPost("/{id:guid}/cancel", async (
    Guid id,
    HttpContext httpContext,
    ITaskAppService service,
    CancellationToken cancellationToken) =>
{
    var task = await service.CancelAsync(id, cancellationToken);
    return task is null
        ? Problem(404, "Task not found.", httpContext)
        : Results.Accepted($"/api/v1/tasks/{id}", task);
})
.WithName("CancelTask")
.WithSummary("Cancela uma task")
.Produces<TaskResponse>(StatusCodes.Status202Accepted)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

tasksGroup.MapGet("/{id:guid}/attempts", async (
    Guid id,
    HttpContext httpContext,
    ITaskAppService service,
    CancellationToken cancellationToken) =>
{
    var task = await service.GetByIdAsync(id, cancellationToken);
    if (task is null)
    {
        return Problem(404, "Task not found.", httpContext);
    }

    var attempts = await service.ListAttemptsAsync(id, cancellationToken);
    return Results.Ok(attempts);
})
.WithName("ListTaskAttempts")
.WithSummary("Lista tentativas de execucao da task")
.Produces<IReadOnlyList<TaskAttemptResponse>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

tasksGroup.MapGet("/{id:guid}/artifacts", async (
    Guid id,
    HttpContext httpContext,
    ITaskAppService service,
    CancellationToken cancellationToken) =>
{
    var task = await service.GetByIdAsync(id, cancellationToken);
    if (task is null)
    {
        return Problem(404, "Task not found.", httpContext);
    }

    var artifacts = await service.ListArtifactsAsync(id, cancellationToken);
    return Results.Ok(artifacts);
})
.WithName("ListTaskArtifacts")
.WithSummary("Lista artefatos de saida da task")
.Produces<IReadOnlyList<TaskArtifactResponse>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

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
    public string? Type { get; set; }
    public string DomainType { get; set; } = string.Empty;
    public string CapabilityType { get; set; } = string.Empty;
    public string TaskExecutionType { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public JsonElement Payload { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public int MaxAttempts { get; set; } = 3;
}
