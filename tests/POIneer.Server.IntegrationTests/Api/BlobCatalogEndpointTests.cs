using System.Net;
using System.Net.Http.Json;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Contracts.Catalog;
using POIneer.Server.Infrastructure.Catalog;

namespace POIneer.Server.IntegrationTests.Api;

public sealed class BlobCatalogEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string RegionId = "geofabrik/europe/germany/berlin";
    private const string Manifest = RegionId + "/manifest.json";
    private const string Regions = """[{"Id":"geofabrik/europe/germany/berlin","Name":"Berlin"}]""";

    [Fact]
    public async Task Catalog_reads_current_manifests_across_pages_and_ignores_invalid_metadata()
    {
        var valid = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "berlin-with-pmtiles.json"));
        using var http = new HttpClient(new BlobHandler(uri => uri.AbsolutePath switch
        {
            "/metadata/regions.json" => Ok(Regions),
            "/metadata" when uri.Query.Contains("marker=next") => Page("", Manifest),
            "/metadata" => Page("next", RegionId + "/archived.json", "geofabrik/broken/manifest.json"),
            "/metadata/" + Manifest => Ok(valid),
            "/metadata/geofabrik/broken/manifest.json" => Ok("{}"),
            _ => throw new InvalidOperationException($"Unexpected blob request: {uri}")
        }));
        using var host = WithBlobSource(http);
        using var client = host.CreateClient();

        var berlin = Assert.Single((await client.GetFromJsonAsync<CatalogEntry[]>("/api/v1/datasets"))!);
        Assert.Equal(RegionId, berlin.Id);
        Assert.Equal("4-abcdef0123456789", berlin.Dataset.Version);
        Assert.Equal(2, berlin.Dataset.Artifacts.Count);
    }

    [Theory]
    [InlineData("", HttpStatusCode.OK)]
    [InlineData("/metadata/regions.json", HttpStatusCode.NotFound)]
    [InlineData("/metadata", HttpStatusCode.Forbidden)]
    [InlineData("/metadata/" + Manifest, HttpStatusCode.ServiceUnavailable)]
    public async Task Catalog_distinguishes_storage_failures_from_empty_discovery(string failingPath,
        HttpStatusCode storageStatus)
    {
        using var http = new HttpClient(new BlobHandler(uri =>
            uri.AbsolutePath == failingPath ? new HttpResponseMessage(storageStatus) :
            uri.AbsolutePath switch
            {
                "/metadata/regions.json" => Ok(Regions),
                "/metadata" => failingPath == "" ? Page("") : Page("", Manifest),
                _ => throw new InvalidOperationException($"Unexpected blob request: {uri}")
            }));
        using var host = WithBlobSource(http);
        using var client = host.CreateClient();

        var response = await client.GetAsync("/api/v1/datasets");
        if (failingPath == "")
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty((await response.Content.ReadFromJsonAsync<CatalogEntry[]>())!);
        }
        else
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    private WebApplicationFactory<Program> WithBlobSource(HttpClient http)
    {
        var options = new BlobClientOptions { Transport = new HttpClientTransport(http) };
        options.Retry.MaxRetries = 0;
        var container = new BlobContainerClient(new Uri("https://example.blob.core.windows.net/metadata"), options);
        return factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<ICatalogSource>(provider =>
                new BlobCatalogSource(container, provider.GetRequiredService<ILogger<BlobCatalogSource>>()))));
    }

    private static HttpResponseMessage Ok(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content)
    };

    private static HttpResponseMessage Page(string next, params string[] names) =>
        Ok($"""
            <EnumerationResults><Blobs>{string.Concat(names.Select(name => $"<Blob><Name>{name}</Name></Blob>"))}</Blobs><NextMarker>{next}</NextMarker></EnumerationResults>
            """);

    private sealed class BlobHandler(Func<Uri, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            if (uri.AbsolutePath == "/metadata")
                Assert.Contains("prefix=geofabrik/", Uri.UnescapeDataString(uri.Query), StringComparison.Ordinal);
            return Task.FromResult(respond(uri));
        }
    }
}
