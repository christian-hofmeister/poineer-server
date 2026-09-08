using System.Text.Json;
using POIneer.Server.Contracts.Catalog;

namespace POIneer.Server.ContractTests.Datasets;

public sealed class CatalogEntryTests
{
    [Fact]
    public void Serialize_ExposesStableCatalogFieldsWithoutProducerStorageDetails()
    {
        var entry = new CatalogEntry("geofabrik/berlin", "Berlin", null, "City", null,
            new CatalogDataset("release", "artifact", 42, new string('a', 64), "/api/datasets/geofabrik/berlin/latest"));
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(entry));
        var root = document.RootElement;
        Assert.Equal(new[] { "id", "name", "country", "category", "bounds", "dataset" },
            root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(JsonValueKind.Null, root.GetProperty("bounds").ValueKind);
        Assert.Equal(new[] { "version", "artifactVersion", "sizeBytes", "sha256Checksum", "downloadUrl" },
            root.GetProperty("dataset").EnumerateObject().Select(property => property.Name));
        Assert.Equal(entry, JsonSerializer.Deserialize<CatalogEntry>(root.GetRawText()));
    }

    [Fact]
    public void Serialize_BoundsUseExplicitCoordinates()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new CatalogBounds(1, 2, 3, 4)));
        Assert.Equal(new[] { "minLat", "minLon", "maxLat", "maxLon" },
            document.RootElement.EnumerateObject().Select(property => property.Name));
    }
}
