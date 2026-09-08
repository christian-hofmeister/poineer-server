using Microsoft.Extensions.Logging.Abstractions;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.UnitTests.Application.Catalog;

public sealed class DatasetCatalogTests
{
    [Fact]
    public async Task GetAsync_JoinsExactIdsAndMapsReleaseAndArtifactVersionsSeparately()
    {
        var source = new Source(new CatalogSnapshot(
            [new("geofabrik/berlin", "Berlin", null, "City"), new("unpublished", "Other", null, null)],
            [new("geofabrik/berlin", "4-1111111111111111", "3-2222222222222222", 42, new string('a', 64)),
             new("GEOFABRIK/BERLIN", "ignored", "ignored", 1, "ignored")]));
        var sut = new DatasetCatalog(source, new BoundsSource(), NullLogger<DatasetCatalog>.Instance);
        using var cancellation = new CancellationTokenSource();

        var entry = Assert.Single(await sut.GetAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, source.Token);
        Assert.Equal("geofabrik/berlin", entry.Id);
        Assert.Equal("Berlin", entry.Name);
        Assert.Null(entry.Country);
        Assert.Equal("City", entry.Category);
        Assert.Equal(52, entry.Bounds!.MinLat);
        Assert.Equal(14, entry.Bounds.MaxLon);
        Assert.Equal("4-1111111111111111", entry.Dataset.Version);
        Assert.Equal("3-2222222222222222", entry.Dataset.ArtifactVersion);
        Assert.Equal(42, entry.Dataset.SizeBytes);
        Assert.Equal(new string('a', 64), entry.Dataset.Sha256Checksum);
        Assert.Equal("/api/datasets/geofabrik/berlin/latest", entry.Dataset.DownloadUrl);
    }

    private sealed class Source(CatalogSnapshot snapshot) : ICatalogSource
    {
        public CancellationToken Token { get; private set; }
        public Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class BoundsSource : IRegionBoundsSource
    {
        public RegionBounds? GetBounds(string regionId) => new(52, 13, 53, 14);
    }
}
