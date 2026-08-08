using System.Text.Json;
using POIneer.Server.Contracts.Regions;

namespace POIneer.Server.ContractTests.Regions;

public sealed class RegionResponseTests
{
    [Fact]
    public void Serialize_ShouldUseExpectedJsonPropertyNames()
    {
        // Arrange
        var response = new RegionResponse
        {
            Id = "berlin",
            Name = "Berlin",
            Country = "DE",
            Category = "city"
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        // Assert
        Assert.Equal("berlin", root.GetProperty("id").GetString());
        Assert.Equal("Berlin", root.GetProperty("name").GetString());
        Assert.Equal("DE", root.GetProperty("country").GetString());
        Assert.Equal("city", root.GetProperty("category").GetString());
    }

    [Fact]
    public void Deserialize_ShouldMapExpectedJsonProperties()
    {
        // Arrange
        const string json =
            """
            {
              "id": "berlin",
              "name": "Berlin",
              "country": "DE",
              "category": "city"
            }
            """;

        // Act
        var response = JsonSerializer.Deserialize<RegionResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("berlin", response.Id);
        Assert.Equal("Berlin", response.Name);
        Assert.Equal("DE", response.Country);
        Assert.Equal("city", response.Category);
    }
}