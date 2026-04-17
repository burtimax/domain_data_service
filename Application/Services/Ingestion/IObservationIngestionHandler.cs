using Shared.Contracts.Ingestion;

namespace Application.Services.Ingestion;

public interface IObservationIngestionHandler
{
    Task HandleAsync(ObservationIngestionMessage message, IngestionSource source, CancellationToken cancellationToken);
}
