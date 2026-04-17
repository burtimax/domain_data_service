namespace Application.Services.Ingestion;

public sealed class IngestionProcessingResult
{
    public static IngestionProcessingResult Success() => new(true, null);
    public static IngestionProcessingResult RetryableFailure(string error) => new(false, error);
    public static IngestionProcessingResult NonRetryableFailure(string error) => new(false, error);

    private IngestionProcessingResult(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public string? Error { get; }
}
