using AssistenteIaApi.Domain.Repositories;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using AssistenteIaApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistenteIaApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");

        services.AddDbContext<AssistenteIaApiDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAiTaskRepository, AiTaskRepository>();

        return services;
    }
}
