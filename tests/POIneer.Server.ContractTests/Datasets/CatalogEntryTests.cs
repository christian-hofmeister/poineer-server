using System.Text.Json;
using POIneer.Server.Contracts.Catalog;

namespace POIneer.Server.ContractTests.Datasets;

public sealed class CatalogEntryTests
{
    [Fact]
    public void Serialize_ExposesStableCatalogFieldsWithoutProducerStorageDetails()
    {
        var entry = new CatalogEntry("geofabrik/berlin", "Berlin", null, "City", null,
            new CatalogDataset("release",
                [new CatalogArtifact("sqlite", "artifact", 42, new string('a', 64), "/api/datasets/geofabrik/berlin/latest/sqlite"),
                 new CatalogArtifact("pmtiles", "tiles-version", 84, new string('b', 64), "/api/datasets/geofabrik/berlin/latest/pmtiles")]));
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(entry));
        var root = document.RootElement;
        Assert.True(root.EnumerateObject().Select(property => property.Name).ToHashSet().SetEquals(
            new[] { "id", "name", "country", "category", "bounds", "dataset" }));
        Assert.Equal(JsonValueKind.Null, root.GetProperty("bounds").ValueKind);
        Assert.True(root.GetProperty("dataset").EnumerateObject().Select(property => property.Name).ToHashSet().SetEquals(
            new[] { "version", "artifacts" }));
        foreach (var artifact in root.GetProperty("dataset").GetProperty("artifacts").EnumerateArray())
            Assert.True(artifact.EnumerateObject().Select(property => property.Name).ToHashSet().SetEquals(
                new[] { "type", "artifactVersion", "sizeBytes", "sha256Checksum", "downloadUrl" }));
        var restored = JsonSerializer.Deserialize<CatalogEntry>(root.GetRawText())!;
        Assert.Equal(entry.Id, restored.Id);
        Assert.Equal(entry.Dataset.Version, restored.Dataset.Version);
        Assert.Equal(entry.Dataset.Artifacts.ToArray(), restored.Dataset.Artifacts.ToArray());
    }

    [Fact]
    public void Serialize_BoundsUseExplicitCoordinates()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new CatalogBounds(1, 2, 3, 4)));
        Assert.Equal(new[] { "minLat", "minLon", "maxLat", "maxLon" },
            document.RootElement.EnumerateObject().Select(property => property.Name));
    }
}
