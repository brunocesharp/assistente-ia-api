namespace AssistenteIaApi.Domain.ValueObjects;

public enum AiTaskStatus
{
    Created = 0,
    Queued = 1,
    Reserved = 2,
    Running = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6,
    DeadLetter = 7
}
