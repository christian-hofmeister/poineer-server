using System.Text.Json;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Contracts.Catalog;

namespace POIneer.Server.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/datasets", GetCatalogAsync)
            .WithName("GetDatasetCatalog")
            .WithTags("Datasets")
            .WithSummary("Get currently published offline datasets")
            .WithDescription("Returns validated current releases with SQLite and optional PMTiles artifacts joined to region metadata. Each artifact includes its type, version, size, checksum and download URL. Bounds are null until a geographic source is available. Download URLs reserve the server routes for the separate download transport implementation.")
            .Produces<IReadOnlyCollection<CatalogEntry>>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        return endpoints;
    }

    private static async Task<IResult> GetCatalogAsync(DatasetCatalog catalog,
        ILogger<DatasetCatalog> logger, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await catalog.GetAsync(cancellationToken)); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogError(exception, "Catalog metadata is unavailable");
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Dataset catalog is temporarily unavailable");
        }
    }
}
