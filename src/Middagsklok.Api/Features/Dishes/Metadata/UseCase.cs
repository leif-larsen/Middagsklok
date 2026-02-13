using Middagsklok.Api.Domain.Dish;

namespace Middagsklok.Api.Features.Dishes.Metadata;

internal sealed class UseCase
{
    // Executes the metadata query for planner-facing dish taxonomy.
    public Task<Response> Execute(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var dishTypes = DishTaxonomy.GetDishTypes()
            .Select(type => new DishTypeMetadata(
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

        var response = new Response(dishTypes, vibeTags);

        return Task.FromResult(response);
    }
}
