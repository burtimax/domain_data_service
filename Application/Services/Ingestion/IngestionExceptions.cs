namespace Application.Services.Ingestion;

public abstract class IngestionException : Exception
{
    protected IngestionException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}

public sealed class RetryableIngestionException : IngestionException
{
    public RetryableIngestionException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}

public sealed class NonRetryableIngestionException : IngestionException
{
    public NonRetryableIngestionException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
