namespace POIneer.Server.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/health")
            .WithTags("Health");

        group.MapGet("", GetHealthAsync)
            .WithName("GetHealth")
            .WithSummary("Returns the health status of the API.")
            .WithDescription("Simple health endpoint for monitoring and uptime checks.")
            .Produces<HealthResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static Task<IResult> GetHealthAsync(
        CancellationToken cancellationToken)
    {
        var response = new HealthResponse(
            Status: "Healthy",
            Service: "POIneer.Server",
            TimestampUtc: DateTimeOffset.UtcNow,
            Version: "1.0.0");

        return Task.FromResult(Results.Ok(response));
    }

    private sealed record HealthResponse(
        string Status,
        string Service,
        DateTimeOffset TimestampUtc,
        string Version);
}