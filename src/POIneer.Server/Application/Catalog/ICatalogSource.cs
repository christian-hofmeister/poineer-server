using POIneer.Server.Domain.Models;

namespace POIneer.Server.Application.Catalog;

// Implementations discover current manifests and validate the producer contract.
public interface ICatalogSource
{
    Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken);
}

public sealed record CatalogSnapshot(IReadOnlyCollection<CatalogRegion> Regions,
    IReadOnlyCollection<PublishedDataset> Datasets);

public interface IRegionBoundsSource
{
    RegionBounds? GetBounds(string regionId);
}
