namespace POIneer.Server.Domain.Models;

public sealed record CatalogRegion(string Id, string Name, string? Country, string? Category);

public sealed record RegionBounds(double MinLat, double MinLon, double MaxLat, double MaxLon);

public sealed record PublishedDataset(string RegionId, string ReleaseVersion,
    IReadOnlyCollection<PublishedArtifact> Artifacts);

public sealed record PublishedArtifact(string Type, string ArtifactVersion, long SizeBytes, string Sha256);
