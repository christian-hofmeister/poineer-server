using POIneer.Server.Infrastructure.Providers;

namespace POIneer.Server.UnitTests.Infrastructure.Providers;

public class HardcodedRegionProviderTests
{
    [Fact]
    public void GetRegions_ReturnsBerlinRegion()
    {
        // Arrange
        var sut = new HardcodedRegionProvider();

        // Act
        var regions = sut.GetRegions();

        // Assert
        var berlin = Assert.Single(regions, region => region.Id == "berlin");

        Assert.Equal("Berlin", berlin.Name);
    }
}