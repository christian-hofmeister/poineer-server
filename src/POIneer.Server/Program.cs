using POIneer.Server.Api.Endpoints;
using POIneer.Server.Application.Abstractions;
using POIneer.Server.Application.Regions;
using POIneer.Server.Infrastructure.Providers;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Infrastructure.Catalog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDatasetMetadataProvider, HardcodedDatasetMetadataProvider>();
builder.Services.AddSingleton<IRegionProvider, HardcodedRegionProvider>();
builder.Services.AddSingleton<IRegionBoundsSource, ConfiguredRegionBoundsSource>();
builder.Services.AddSingleton<DatasetCatalog>();
builder.Services.AddSingleton<ICatalogSource>(services =>
{
    var root = builder.Configuration["Catalog:MetadataRoot"];
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
    api.MapRegionEndpoints();
    api.MapCatalogEndpoints();
}
