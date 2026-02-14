using AssistenteIaApi.Application;
using AssistenteIaApi.Application.Dto;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Infrastructure;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using Microsoft.EntityFrameworkCore;

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
app.MapPost("/tasks", async (CreateTaskRequest request, ITaskAppService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/tasks/{created.Id}", created);
});

app.MapGet("/tasks/{id:guid}", async (Guid id, ITaskAppService service, CancellationToken cancellationToken) =>
{
    var task = await service.GetByIdAsync(id, cancellationToken);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

app.Run();
