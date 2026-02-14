using Microsoft.Extensions.DependencyInjection;
using AssistenteIaApi.Application.Ports.In;
using AssistenteIaApi.Application.Services;

namespace AssistenteIaApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaskAppService, TaskAppService>();

        return services;
    }
}
