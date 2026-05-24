namespace POIneer.Server.Domain.Models;

public sealed record DatasetMetadata(
    string RegionId,
    string RegionName,
    string FileName,
    long SizeBytes,
    DateTimeOffset UpdatedAtUtc,
    string Version);
