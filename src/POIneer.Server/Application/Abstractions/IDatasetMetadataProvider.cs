using POIneer.Server.Domain.Models;

namespace POIneer.Server.Application.Abstractions;

public interface IDatasetMetadataProvider
{
    Task<IReadOnlyCollection<DatasetMetadata>> GetDatasetsAsync(
        CancellationToken cancellationToken = default);
}
