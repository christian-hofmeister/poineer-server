using Azure.Identity;
using Azure.Storage.Blobs;
using POIneer.Server.Api.Endpoints;
using POIneer.Server.Application.Abstractions;
using POIneer.Server.Infrastructure.Providers;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Infrastructure.Catalog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDatasetMetadataProvider, HardcodedDatasetMetadataProvider>();
builder.Services.AddSingleton<IRegionBoundsSource, ConfiguredRegionBoundsSource>();
builder.Services.AddSingleton<DatasetCatalog>();
builder.Services.AddSingleton<ICatalogSource>(services =>
{
    var root = builder.Configuration["Catalog:MetadataRoot"];
    if (Uri.TryCreate(root, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
    {
        if (uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.UserInfo.Length != 0)
            return new UnconfiguredCatalogSource();

        var clientId = builder.Configuration["Catalog:ManagedIdentityClientId"];
        Azure.Core.TokenCredential credential = string.IsNullOrWhiteSpace(clientId)
            ? new DefaultAzureCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId));
        return new BlobCatalogSource(new BlobContainerClient(uri, credential),
            services.GetRequiredService<ILogger<BlobCatalogSource>>());
    }

    return string.IsNullOrWhiteSpace(root)
        ? new UnconfiguredCatalogSource()
        : new LocalCatalogSource(Path.GetFullPath(root, builder.Environment.ContentRootPath),
            services.GetRequiredService<ILogger<LocalCatalogSource>>());
});

var app = builder.Build();
var api = app.MapGroup("/api");

MapEndpoints(api);
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    //
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

static void MapEndpoints(RouteGroupBuilder api)
{
    api.MapHealthEndpoints();
    api.MapCatalogEndpoints();
}
