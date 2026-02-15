namespace AssistenteIaApi.Infrastructure.Messaging.Executors;

public sealed class TransientAiException : Exception
{
    public TransientAiException(string message) : base(message)
    {
    }
}
