namespace POIneer.Server.Domain.Models;

public sealed record CatalogRegion(string Id, string Name, string? Country, string? Category);

public sealed record RegionBounds(double MinLat, double MinLon, double MaxLat, double MaxLon);

public sealed record PublishedDataset(string RegionId, string ReleaseVersion,
    string ArtifactVersion, long SizeBytes, string Sha256);
