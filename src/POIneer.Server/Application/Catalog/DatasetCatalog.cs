using POIneer.Server.Contracts.Catalog;

namespace POIneer.Server.Application.Catalog;

public sealed class DatasetCatalog(ICatalogSource source, IRegionBoundsSource boundsSource,
    ILogger<DatasetCatalog> logger)
{
    public async Task<IReadOnlyCollection<CatalogEntry>> GetAsync(CancellationToken cancellationToken)
    {
        var snapshot = await source.ReadAsync(cancellationToken);
        var regions = snapshot.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var entries = new List<CatalogEntry>();
        foreach (var dataset in snapshot.Datasets.OrderBy(dataset => dataset.RegionId, StringComparer.Ordinal))
        {
            if (!regions.TryGetValue(dataset.RegionId, out var region))
            {
                logger.LogWarning("Missing catalog metadata for region {RegionId}", dataset.RegionId);
                continue;
            }

            var bounds = boundsSource.GetBounds(region.Id);
            entries.Add(new CatalogEntry(region.Id, region.Name, region.Country, region.Category,
                bounds is null ? null : new CatalogBounds(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon),
                new CatalogDataset(dataset.ReleaseVersion, dataset.ArtifactVersion, dataset.SizeBytes,
                    dataset.Sha256, $"/api/datasets/{region.Id}/latest")));
        }

        return entries;
    }
}
