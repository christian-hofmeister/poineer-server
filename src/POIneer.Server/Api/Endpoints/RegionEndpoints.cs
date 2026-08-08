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
            .WithTags("Regions")
            .Produces<IReadOnlyCollection<RegionResponse>>(StatusCodes.Status200OK);

        return endpoints;
    }
}