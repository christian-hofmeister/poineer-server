using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Contracts.Catalog;

namespace POIneer.Server.IntegrationTests.Api;

public sealed class CatalogEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public CatalogEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetDatasets_MapsDevelopmentFixture()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/datasets");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = await response.Content.ReadFromJsonAsync<CatalogEntry[]>();
        var berlin = Assert.Single(entries!);
        Assert.Equal("geofabrik/europe/germany/berlin", berlin.Id);
        Assert.Equal("Berlin", berlin.Name);
        Assert.Equal("4-d790344f01234567", berlin.Dataset.Version);
        Assert.Equal(187654321, berlin.Dataset.SizeBytes);
        Assert.Null(berlin.Bounds);
        Assert.Equal("/api/datasets/geofabrik/europe/germany/berlin/latest", berlin.Dataset.DownloadUrl);
    }

    [Fact]
    public async Task GetDatasets_ProductionDoesNotAdvertiseSyntheticFixtures()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/datasets")).StatusCode);
    }

    [Fact]
    public async Task GetDatasets_SourceFailureReturns503Problem()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<ICatalogSource>(new FailingSource())));
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/datasets");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("private-path", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetDatasets_EmptySourceReturnsEmptyArray()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<ICatalogSource>(new EmptySource())));
        using var client = factory.CreateClient();
        Assert.Empty((await client.GetFromJsonAsync<CatalogEntry[]>("/api/datasets"))!);
    }

    [Fact]
    public async Task OpenApi_DescribesCatalogAndSourceFailure()
    {
        using var client = _factory.CreateClient();
        using var document = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var responses = document.RootElement.GetProperty("paths").GetProperty("/api/datasets")
            .GetProperty("get").GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("503", out _));
    }

    private sealed class FailingSource : ICatalogSource
    {
        public Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            throw new IOException("private-path");
    }
    private sealed class EmptySource : ICatalogSource
    {
        public Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new CatalogSnapshot([], []));
    }
}
