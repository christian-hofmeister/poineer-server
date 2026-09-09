using Microsoft.Extensions.Configuration;
using POIneer.Server.Infrastructure.Catalog;

namespace POIneer.Server.UnitTests.Infrastructure.Catalog;

public sealed class ConfiguredRegionBoundsSourceTests
{
    [Fact]
    public void GetBounds_UsesExactIdAndLeavesUnknownGeographyNull()
    {
        var source = Create("10", "20", "11", "21");
        Assert.Equal(10, source.GetBounds("example/region")!.MinLat);
        Assert.Null(source.GetBounds("example/REGION"));
        Assert.Null(source.GetBounds("missing"));
    }

    [Theory]
    [InlineData("-91", "20", "11", "21")]
    [InlineData("10", "20", "91", "21")]
    [InlineData("10", "-181", "11", "21")]
    [InlineData("10", "20", "11", "181")]
    [InlineData("12", "20", "11", "21")]
    [InlineData("10", "22", "11", "21")]
    [InlineData("10", "20", "10", "21")]
    [InlineData("NaN", "20", "11", "21")]
    [InlineData(null, "20", "11", "21")]
    public void Configuration_RejectsInvalidBounds(string? minLat, string minLon, string maxLat, string maxLon) =>
        Assert.Throws<InvalidOperationException>(() => Create(minLat, minLon, maxLat, maxLon));

    private static ConfiguredRegionBoundsSource Create(string? minLat, string minLon, string maxLat, string maxLon)
    {
        var values = new Dictionary<string, string?>
        {
            ["Catalog:RegionBounds:0:RegionId"] = "example/region",
            ["Catalog:RegionBounds:0:MinLat"] = minLat,
            ["Catalog:RegionBounds:0:MinLon"] = minLon,
            ["Catalog:RegionBounds:0:MaxLat"] = maxLat,
            ["Catalog:RegionBounds:0:MaxLon"] = maxLon
        };
        return new(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }
}
