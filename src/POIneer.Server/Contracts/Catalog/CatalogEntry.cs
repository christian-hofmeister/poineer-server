using System.Text.Json.Serialization;

namespace POIneer.Server.Contracts.Catalog;

public sealed record CatalogEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("bounds")] CatalogBounds? Bounds,
    [property: JsonPropertyName("dataset")] CatalogDataset Dataset);

public sealed record CatalogBounds(
    [property: JsonPropertyName("minLat")] double MinLat,
    [property: JsonPropertyName("minLon")] double MinLon,
    [property: JsonPropertyName("maxLat")] double MaxLat,
    [property: JsonPropertyName("maxLon")] double MaxLon);

public sealed record CatalogDataset(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("artifacts")] IReadOnlyCollection<CatalogArtifact> Artifacts);

public sealed record CatalogArtifact(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("artifactVersion")] string ArtifactVersion,
    [property: JsonPropertyName("sizeBytes")] long SizeBytes,
    [property: JsonPropertyName("sha256Checksum")] string Sha256Checksum,
    [property: JsonPropertyName("downloadUrl")] string DownloadUrl);
