using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using POIneer.Server.Contracts.Regions;

namespace POIneer.Server.IntegrationTests.Api;

public class RegionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RegionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRegions_ReturnsOkWithAvailableRegions()
    {
        var response = await _client.GetAsync("/api/regions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var regions = await response.Content.ReadFromJsonAsync<List<RegionResponse>>();

        Assert.NotNull(regions);

        var berlin = Assert.Single(regions, region => region.Id == "berlin");

        Assert.Equal("Berlin", berlin.Name);
    }
}