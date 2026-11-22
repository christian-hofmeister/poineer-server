using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace POIneer.Server.IntegrationTests.Api;

public sealed class OpenApiEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Document_preserves_api_paths_and_catalog_schema(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = host.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        Assert.Equal(new[] { "/api/datasets", "/api/health", "/api/v1/datasets" },
            paths.EnumerateObject().Select(property => property.Name).Order());
        Assert.True(paths.GetProperty("/api/health").GetProperty("get")
            .GetProperty("responses").TryGetProperty("200", out _));

        var canonical = paths.GetProperty("/api/v1/datasets").GetProperty("get");
        var alias = paths.GetProperty("/api/datasets").GetProperty("get");
        Assert.Equal("GetDatasetCatalogV1", canonical.GetProperty("operationId").GetString());
        Assert.Equal("GetDatasetCatalog", alias.GetProperty("operationId").GetString());
        Assert.Contains("Compatibility alias for GET /api/v1/datasets", alias.GetProperty("description").GetString());
        var responses = canonical.GetProperty("responses");
        Assert.Equal(responses.GetRawText(), alias.GetProperty("responses").GetRawText());
        var collection = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        var entry = ResolveSchema(root, collection.GetProperty("items"));
        AssertProperties(entry, "id", "name", "country", "category", "bounds", "dataset");
        var dataset = ResolveSchema(root, entry.GetProperty("properties").GetProperty("dataset"));
        AssertProperties(dataset, "version", "artifacts");
        var artifact = ResolveSchema(root, dataset.GetProperty("properties")
            .GetProperty("artifacts").GetProperty("items"));
        AssertProperties(artifact, "type", "artifactVersion", "sizeBytes", "sha256Checksum", "downloadUrl");

        var unavailable = responses.GetProperty("503");
        Assert.Contains("insufficient read permissions", unavailable.GetProperty("description").GetString());
        var problem = ResolveSchema(root, unavailable.GetProperty("content")
            .GetProperty("application/problem+json").GetProperty("schema"));
        Assert.True(problem.GetProperty("properties").TryGetProperty("status", out _));
        Assert.True(problem.GetProperty("properties").TryGetProperty("title", out _));
    }

    [Fact]
    public async Task Development_ui_loads_canonical_document_and_legacy_alias_uses_same_generator()
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = host.CreateClient();
        var page = await client.GetAsync("/swagger");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("text/html", page.Content.Headers.ContentType?.MediaType);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("index.js", html);
        var script = await client.GetStringAsync("/swagger/index.js");
        var configuration = Regex.Match(script, """var configObject = (?<config>\{[^\r\n]+\});""");
        Assert.True(configuration.Success, "Swagger UI must expose its document configuration.");
        using var config = JsonDocument.Parse(configuration.Groups["config"].Value);
        var url = Assert.Single(config.RootElement.GetProperty("urls").EnumerateArray())
            .GetProperty("url").GetString();
        Assert.Equal("/openapi/v1.json", url);
        var canonical = await client.GetStringAsync(url);
        using var document = JsonDocument.Parse(canonical);
        Assert.True(document.RootElement.TryGetProperty("openapi", out _));
        Assert.Equal(canonical, await client.GetStringAsync("/swagger/v1/swagger.json"));
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Swagger_ui_assets_and_legacy_alias_are_unavailable_outside_development(string environment)
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = host.CreateClient();
        foreach (var path in new[] { "/swagger", "/swagger/index.html", "/swagger/index.js", "/swagger/v1/swagger.json" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema) =>
        schema.TryGetProperty("$ref", out var reference)
            ? document.GetProperty("components").GetProperty("schemas")
                .GetProperty(reference.GetString()!.Split('/')[^1])
            : schema;

    private static void AssertProperties(JsonElement schema, params string[] expected) =>
        Assert.Equal(expected.Order(), schema.GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).Order());
}
