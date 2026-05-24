using POIneer.Server.Application.Abstractions;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.Infrastructure.Providers;

public sealed class HardcodedDatasetMetadataProvider
    : IDatasetMetadataProvider
{
    private static readonly IReadOnlyCollection<DatasetMetadata> Datasets =
    [
        new(
            RegionId: "berlin",
            RegionName: "Berlin",
            FileName: "poi.sqlite",
            SizeBytes: 0,
            UpdatedAtUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Version: "0.1.0")
    ];

    public Task<IReadOnlyCollection<DatasetMetadata>> GetDatasetsAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Datasets);
    }
}
