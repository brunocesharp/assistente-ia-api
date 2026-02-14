using AssistenteIaApi.Application.Dto;

namespace AssistenteIaApi.Application.Ports.In;

public interface ITaskAppService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
