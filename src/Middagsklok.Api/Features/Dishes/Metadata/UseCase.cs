using Middagsklok.Api.Domain.Dish;

namespace Middagsklok.Api.Features.Dishes.Metadata;

internal sealed class UseCase
{
    // Executes the metadata query for dish cuisines.
    public Task<Response> Execute(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var cuisines = Enum.GetValues<CuisineType>()
            .Select(cuisine => new CuisineMetadata(
                cuisine.ToString(),
                FormatCuisineLabel(cuisine),
                GetOrder(cuisine),
                cuisine is not CuisineType.None))
            .OrderBy(cuisine => cuisine.Order)
            .ThenBy(cuisine => cuisine.Label)
            .ToArray();

        var response = new Response(cuisines);

        return Task.FromResult(response);
    }

    // Formats cuisine enum values into display labels.
    private static string FormatCuisineLabel(CuisineType cuisine) =>
        cuisine switch
        {
            CuisineType.None => "Unspecified",
            CuisineType.MiddleEastern => "Middle Eastern",
            _ => cuisine.ToString()
        };

    // Defines stable ordering for cuisine metadata in clients.
    private static int GetOrder(CuisineType cuisine) =>
        cuisine switch
        {
            CuisineType.Other => 900,
            CuisineType.None => 1000,
            _ => 100
        };
}
