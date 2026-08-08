namespace POIneer.Server.Contracts.Regions;

// <summary>
// Represents a response containing information about a region.
public sealed record RegionResponse(
    string Id,
    string Name,
    string Country,
    string Category);