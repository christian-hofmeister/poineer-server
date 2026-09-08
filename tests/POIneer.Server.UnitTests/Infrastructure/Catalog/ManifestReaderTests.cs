using System.Text.Json;
using POIneer.Server.Infrastructure.Catalog;

namespace POIneer.Server.UnitTests.Infrastructure.Catalog;

public sealed class ManifestReaderTests
{
    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory,
        "Fixtures", "contracts", "region-manifest", "v1");
    private const string Key = "geofabrik/europe/germany/berlin/manifest.json";

    public static IEnumerable<object[]> Fixtures()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureRoot, "fixtures.json")));
        return inventory.RootElement.EnumerateArray().Select(item => new object[]
        {
            item.GetProperty("file").GetString()!, item.GetProperty("contractValid").GetBoolean()
        }).ToArray();
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Read_MatchesSharedProducerFixtures(string file, bool valid)
    {
        var json = File.ReadAllText(Path.Combine(FixtureRoot, file));
        if (!valid)
        {
            Assert.Throws<JsonException>(() => ManifestReader.Read(Key, json));
            return;
        }
        var dataset = ManifestReader.Read(Key, json);
        Assert.Equal("geofabrik/europe/germany/berlin", dataset.RegionId);
        Assert.True(dataset.SizeBytes > 0);
        Assert.Equal(64, dataset.Sha256.Length);
    }

    [Fact]
    public void Read_RejectsPathMismatch()
    {
        var json = File.ReadAllText(Path.Combine(FixtureRoot, "examples", "berlin.json"));
        Assert.Throws<JsonException>(() => ManifestReader.Read("geofabrik/other/manifest.json", json));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{broken")]
    public void Read_RejectsMalformedDocuments(string json) =>
        Assert.ThrowsAny<JsonException>(() => ManifestReader.Read(Key, json));
}
