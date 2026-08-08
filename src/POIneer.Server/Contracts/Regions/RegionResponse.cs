using System.Text.Json.Serialization;

namespace POIneer.Server.Contracts.Regions;

/// <summary>
/// Represents metadata for an offline region available to API clients.
/// </summary>
public sealed class RegionResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("country")]
    public required string Country { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }
}