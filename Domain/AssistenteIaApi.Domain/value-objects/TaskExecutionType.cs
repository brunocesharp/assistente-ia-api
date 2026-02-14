namespace AssistenteIaApi.Domain.ValueObjects;

public enum TaskExecutionType
{
    Sync = 0,
    Async = 1,
    Saga = 2,
    HumanInLoop = 3,
    Batch = 4,
    EventDriven = 5
}
