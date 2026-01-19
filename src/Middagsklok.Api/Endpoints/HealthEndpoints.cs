using Middagsklok.Contracts.Health;

namespace Middagsklok.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => new HealthResponse("ok"));
    }
}
