using AssistenteIaApi.Infrastructure;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddTaskQueueConsumer(builder.Configuration);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AssistenteIaApiDbContext>();
    dbContext.Database.Migrate();
}

host.Run();
