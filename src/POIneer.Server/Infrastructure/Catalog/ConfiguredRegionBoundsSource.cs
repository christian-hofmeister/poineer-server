using POIneer.Server.Application.Catalog;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.Infrastructure.Catalog;

// Server-owned enrichment, separate from the producer's regions.json contract.
public sealed class ConfiguredRegionBoundsSource : IRegionBoundsSource
{
    private readonly Dictionary<string, RegionBounds> _bounds = new(StringComparer.Ordinal);

    public ConfiguredRegionBoundsSource(IConfiguration configuration)
    {
        foreach (var entry in configuration.GetSection("Catalog:RegionBounds").GetChildren())
        {
            var id = entry["RegionId"];
            var bounds = entry.Get<RegionBounds>();
            if (id is null || !ManifestReader.IsRegionId(id) || bounds is null
                || new[] { "MinLat", "MinLon", "MaxLat", "MaxLon" }.Any(field => entry[field] is null)
                || !double.IsFinite(bounds.MinLat) || !double.IsFinite(bounds.MaxLat)
                || !double.IsFinite(bounds.MinLon) || !double.IsFinite(bounds.MaxLon)
                || bounds.MinLat < -90 || bounds.MaxLat > 90 || bounds.MinLat >= bounds.MaxLat
                || bounds.MinLon < -180 || bounds.MaxLon > 180 || bounds.MinLon >= bounds.MaxLon
                || !_bounds.TryAdd(id, bounds))
                throw new InvalidOperationException("Invalid or duplicate Catalog:RegionBounds entry.");
        }
    }

    public RegionBounds? GetBounds(string regionId) => _bounds.GetValueOrDefault(regionId);
}
