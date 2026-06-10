using System.Text.Json;
using POIneer.Server.Contracts.Datasets;

namespace POIneer.Server.ContractTests.Datasets;

public sealed class DatasetMetadataTests
{
    [Fact]
    public void Serialize_ProducesExpectedJsonPropertyNames()
    {
        var metadata = new DatasetMetadata
        {
            RegionId = "berlin",
            DatasetVersion = "2026.05.17",
            FileName = "berlin.poi.sqlite",
            FileSizeBytes = 12345678,
            LastUpdatedUtc = new DateTimeOffset(2026, 5, 17, 8, 0, 0, TimeSpan.Zero)
        };

        var json = JsonSerializer.Serialize(metadata);

        Assert.Contains("\"regionId\"", json);
        Assert.Contains("\"datasetVersion\"", json);
        Assert.Contains("\"fileName\"", json);
        Assert.Contains("\"fileSizeBytes\"", json);
        Assert.Contains("\"lastUpdatedUtc\"", json);
    }

    [Fact]
    public void Deserialize_RoundTrip_PreservesValues()
    {
        var json = """
            {
                "regionId": "berlin",
                "datasetVersion": "2026.05.17",
                "fileName": "berlin.poi.sqlite",
                "fileSizeBytes": 12345678,
                "lastUpdatedUtc": "2026-05-17T08:00:00Z"
            }
            """;

        var metadata = JsonSerializer.Deserialize<DatasetMetadata>(json);

        Assert.NotNull(metadata);
        Assert.Equal("berlin", metadata.RegionId);
        Assert.Equal("2026.05.17", metadata.DatasetVersion);
        Assert.Equal("berlin.poi.sqlite", metadata.FileName);
        Assert.Equal(12345678L, metadata.FileSizeBytes);
        Assert.Equal(new DateTimeOffset(2026, 5, 17, 8, 0, 0, TimeSpan.Zero), metadata.LastUpdatedUtc);
    }
}
