using POIneer.Server.Api.Endpoints;
using POIneer.Server.Application.Abstractions;
using POIneer.Server.Application.Regions;
using POIneer.Server.Infrastructure.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDatasetMetadataProvider, HardcodedDatasetMetadataProvider>();
builder.Services.AddSingleton<IRegionProvider, HardcodedRegionProvider>();

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
}