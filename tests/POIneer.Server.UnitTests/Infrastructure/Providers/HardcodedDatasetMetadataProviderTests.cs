using POIneer.Server.Infrastructure.Providers;

namespace POIneer.Server.UnitTests.Infrastructure.Providers;

public sealed class HardcodedDatasetMetadataProviderTests
{
    [Fact]
    public async Task GetDatasetsAsync_ReturnsBerlinDataset()
    {
        // Arrange
        var provider = new HardcodedDatasetMetadataProvider();

        // Act
        var datasets = await provider.GetDatasetsAsync();
        var dataset = Assert.Single(datasets);

        // Assert
        Assert.Equal("berlin", dataset.RegionId);
        Assert.Equal("Berlin", dataset.RegionName);
        Assert.Equal("poi.sqlite", dataset.FileName);
        Assert.Equal(0, dataset.SizeBytes);
        Assert.Equal("0.1.0", dataset.Version);
        Assert.NotEqual(default, dataset.UpdatedAtUtc);
    }
}
