using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using POIneer.Server.Infrastructure.Catalog;

namespace POIneer.Server.UnitTests.Infrastructure.Catalog;

public sealed class LocalCatalogSourceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "poineer-catalog-" + Guid.NewGuid());
    private const string Region = "geofabrik/europe/germany/berlin";

    public LocalCatalogSourceTests() => Directory.CreateDirectory(_root);

    [Fact]
    public async Task ReadAsync_DiscoversOnlyCurrentManifestsAndRejectsInvalidRelease()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "regions.json"),
            """[{"Id":"geofabrik/europe/germany/berlin","Name":"Berlin","PbfUrl":"https://example.com/source"}]""");
        var directory = Directory.CreateDirectory(Path.Combine(_root, Region)).FullName;
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "contracts", "region-manifest", "v1", "examples", "berlin.json");
        File.Copy(fixture, Path.Combine(directory, "manifest.json"));
        await File.WriteAllTextAsync(Path.Combine(directory, "old-manifest.json"), "broken");
        var invalid = Directory.CreateDirectory(Path.Combine(_root, "geofabrik/invalid")).FullName;
        await File.WriteAllTextAsync(Path.Combine(invalid, "manifest.json"), "{}");

        var result = await CreateSource().ReadAsync(default);

        Assert.Equal(Region, Assert.Single(result.Datasets).RegionId);
        Assert.Null(Assert.Single(result.Regions).Country);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("[{\"Id\":\"x\",\"Name\":\"X\"},{\"Id\":\"x\",\"Name\":\"Y\"}]")]
    public async Task ReadAsync_RejectsMalformedSnapshot(string json)
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "regions.json"), json);
        await Assert.ThrowsAsync<JsonException>(() => CreateSource().ReadAsync(default));
    }

    [Fact]
    public async Task ReadAsync_MissingSourceIsNotAnEmptyCatalog() =>
        await Assert.ThrowsAsync<FileNotFoundException>(() => CreateSource().ReadAsync(default));

    [Fact]
    public async Task ReadAsync_RegionWithoutManifestIsNotAvailable()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "regions.json"), """[{"Id":"geofabrik/berlin","Name":"Berlin"}]""");
        Assert.Empty((await CreateSource().ReadAsync(default)).Datasets);
    }

    private LocalCatalogSource CreateSource() => new(_root, NullLogger<LocalCatalogSource>.Instance);
    public void Dispose() => Directory.Delete(_root, recursive: true);
}
