using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using POIneer.Server.Api.Endpoints;

namespace POIneer.Server.IntegrationTests.Api;

public sealed class HealthEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsBuildVersionAndPreservesResponseShape()
    {
        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(new[] { "service", "status", "timestampUtc", "version" },
            root.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal("POIneer.Server", root.GetProperty("service").GetString());
        Assert.True(root.GetProperty("timestampUtc").TryGetDateTimeOffset(out _));
        var expectedVersion = typeof(HealthEndpoints).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion.Split('+')[0];
        Assert.Equal(expectedVersion, root.GetProperty("version").GetString());
    }
}
