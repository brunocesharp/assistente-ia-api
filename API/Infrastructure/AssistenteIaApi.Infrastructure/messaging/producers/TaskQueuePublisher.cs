using AssistenteIaApi.Application.Ports.Out;
using MassTransit;

namespace AssistenteIaApi.Infrastructure.Messaging.Producers;

public class TaskQueuePublisher : ITaskQueuePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public TaskQueuePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync(TaskQueued message, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.Publish(message, cancellationToken);
    }
}
