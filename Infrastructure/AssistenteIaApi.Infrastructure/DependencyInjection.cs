using AssistenteIaApi.Application.Ports.Out;
using AssistenteIaApi.Domain.Repositories;
using AssistenteIaApi.Infrastructure.Config;
using AssistenteIaApi.Infrastructure.Messaging.Brokers;
using AssistenteIaApi.Infrastructure.Messaging.Producers;
using AssistenteIaApi.Infrastructure.Persistence.Orm;
using AssistenteIaApi.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistenteIaApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");

        services.AddDbContext<AssistenteIaApiDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAiTaskRepository, AiTaskRepository>();

        return services;
    }

    public static IServiceCollection AddTaskQueuePublisher(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitOptions = configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddScoped<ITaskQueuePublisher, TaskQueuePublisher>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((_, cfg) =>
            {
                cfg.Host(rabbitOptions.Host, rabbitOptions.VirtualHost, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });
            });
        });

        return services;
    }

    public static IServiceCollection AddTaskQueueConsumer(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitOptions = configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TaskQueuedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitOptions.Host, rabbitOptions.VirtualHost, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });

                cfg.ReceiveEndpoint(rabbitOptions.QueueName, e =>
                {
                    e.ConfigureConsumer<TaskQueuedConsumer>(context);
                    e.PrefetchCount = 16;
                    e.ConcurrentMessageLimit = 1;
                });
            });
        });

        return services;
    }
}
