using System.Text.Json.Serialization;

namespace POIneer.Server.Contracts.Datasets;

public sealed record DatasetMetadata
{
    [JsonPropertyName("regionId")]
    public required string RegionId { get; init; }

    [JsonPropertyName("datasetVersion")]
    public required string DatasetVersion { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("fileSizeBytes")]
    public required long FileSizeBytes { get; init; }

    [JsonPropertyName("lastUpdatedUtc")]
    public required DateTimeOffset LastUpdatedUtc { get; init; }
}
