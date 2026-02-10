using Middagsklok.Api.Domain.Dish;

namespace Middagsklok.Api.Features.Dishes.Metadata;

internal sealed class UseCase
{
    // Executes the metadata query for planner-facing dish taxonomy.
    public Task<Response> Execute(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var cuisines = DishTaxonomy.GetDishTypes()
            .Select(type => new CuisineMetadata(
                type.Value.ToString(),
                type.Label,
                type.DisplayOrder,
                true,
                type.DefaultWeightWeekday,
                type.DefaultWeightWeekend,
                type.IsFallback))
            .OrderBy(type => type.Order)
            .ThenBy(type => type.Label)
            .ToArray();
        var vibeTags = DishTaxonomy.GetVibeTags()
            .Select(tag => new VibeTagMetadata(
                tag.Value,
                tag.Label,
                tag.DisplayOrder,
                tag.WeightMultiplierWeekday,
                tag.WeightMultiplierWeekend))
            .OrderBy(tag => tag.Order)
            .ThenBy(tag => tag.Label)
            .ToArray();

        var response = new Response(cuisines, vibeTags);

        return Task.FromResult(response);
    }
}
