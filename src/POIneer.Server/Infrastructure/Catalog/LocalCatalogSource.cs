using POIneer.Server.Application.Catalog;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.Infrastructure.Catalog;

public sealed class LocalCatalogSource(string metadataRoot, ILogger<LocalCatalogSource> logger) : ICatalogSource
{
    public async Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(metadataRoot, "regions.json"), cancellationToken);
        var regions = CatalogMetadataReader.ReadRegions(json);

        var datasets = new List<PublishedDataset>();
        var discoveryRoot = Path.Combine(metadataRoot, "geofabrik");
        // A valid snapshot with no current manifests represents an empty catalog.
        if (!Directory.Exists(discoveryRoot)) return new CatalogSnapshot(regions, datasets);
        foreach (var path in Directory.EnumerateFiles(discoveryRoot, "manifest.json", SearchOption.AllDirectories))
        {
            var key = Path.GetRelativePath(metadataRoot, path).Replace('\\', '/');
            var manifest = await File.ReadAllTextAsync(path, cancellationToken);
            CatalogMetadataReader.AddManifest(datasets, key, manifest, logger);
        }
        return new CatalogSnapshot(regions, datasets);
    }
}

public sealed class UnconfiguredCatalogSource : ICatalogSource
{
    public Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken) =>
        throw new IOException("No catalog metadata source is configured.");
}
