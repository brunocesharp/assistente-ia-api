using AssistenteIaApi.Application;
using AssistenteIaApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", () => "API no ar");

app.Run();
