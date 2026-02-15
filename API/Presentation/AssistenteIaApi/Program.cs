using AssistenteIaApi.Application;
using AssistenteIaApi.Infrastructure;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddTaskQueuePublisher(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
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
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Assistente IA API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();
