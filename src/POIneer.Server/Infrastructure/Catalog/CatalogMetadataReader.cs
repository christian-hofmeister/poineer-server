using System.Text.Json;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.Infrastructure.Catalog;

internal static class CatalogMetadataReader
{
    public static CatalogRegion[] ReadRegions(string json)
    {
        var regions = JsonSerializer.Deserialize<CatalogRegion[]>(json)
            ?? throw new JsonException("Missing regions snapshot.");
        if (regions.Any(region => region is null || string.IsNullOrWhiteSpace(region.Id)
                || !ManifestReader.IsRegionId(region.Id) || string.IsNullOrWhiteSpace(region.Name))
            || regions.Select(region => region.Id).Distinct(StringComparer.Ordinal).Count() != regions.Length)
            throw new JsonException("Invalid or duplicate region metadata.");
        return regions;
    }

    public static void AddManifest(List<PublishedDataset> datasets, string key, string json, ILogger logger)
    {
        try { datasets.Add(ManifestReader.Read(key, json)); }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Rejected manifest {ManifestKey}", key);
        }
    }
}
