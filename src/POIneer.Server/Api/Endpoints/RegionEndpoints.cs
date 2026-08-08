using POIneer.Server.Application.Regions;
using POIneer.Server.Contracts.Regions;

namespace POIneer.Server.Api.Endpoints;

public static class RegionEndpoints
{
    public static IEndpointRouteBuilder MapRegionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/regions",
            (IRegionProvider regionProvider) =>
                Results.Ok(regionProvider.GetRegions()))
            .WithName("GetRegions")
            .WithSummary("Get available regions")
            .WithDescription(
                "Returns all regions currently available for offline datasets.")
            .WithTags("Regions")
            .Produces<IReadOnlyCollection<RegionResponse>>(StatusCodes.Status200OK);

        return endpoints;
    }
}