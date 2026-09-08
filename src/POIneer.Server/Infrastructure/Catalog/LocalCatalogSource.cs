using System.Text.Json;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.Infrastructure.Catalog;

public sealed class LocalCatalogSource(string metadataRoot, ILogger<LocalCatalogSource> logger) : ICatalogSource
{
    public async Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(metadataRoot, "regions.json"), cancellationToken);
        var regions = JsonSerializer.Deserialize<CatalogRegion[]>(json)
            ?? throw new JsonException("Missing regions snapshot.");
        if (regions.Any(region => region is null || string.IsNullOrWhiteSpace(region.Id)
                || !ManifestReader.IsRegionId(region.Id) || string.IsNullOrWhiteSpace(region.Name))
            || regions.Select(region => region.Id).Distinct(StringComparer.Ordinal).Count() != regions.Length)
            throw new JsonException("Invalid or duplicate region metadata.");

        var datasets = new List<PublishedDataset>();
        var discoveryRoot = Path.Combine(metadataRoot, "geofabrik");
        // A valid snapshot with no current manifests represents an empty catalog.
        if (!Directory.Exists(discoveryRoot)) return new CatalogSnapshot(regions, datasets);
        foreach (var path in Directory.EnumerateFiles(discoveryRoot, "manifest.json", SearchOption.AllDirectories))
        {
            var key = Path.GetRelativePath(metadataRoot, path).Replace('\\', '/');
            var manifest = await File.ReadAllTextAsync(path, cancellationToken);
            try { datasets.Add(ManifestReader.Read(key, manifest)); }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Rejected manifest {ManifestKey}", key);
            }
        }
        return new CatalogSnapshot(regions, datasets);
    }
}

public sealed class UnconfiguredCatalogSource : ICatalogSource
{
    public Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken) =>
        throw new IOException("No catalog metadata source is configured.");
}
