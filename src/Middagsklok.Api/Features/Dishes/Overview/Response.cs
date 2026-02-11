namespace Middagsklok.Api.Features.Dishes.Overview;

public sealed record Response(IEnumerable<DishOverview> Dishes);

public sealed record DishOverview(
    string Id,
    string Name,
    string Cuisine,
    int PrepMinutes,
    int CookMinutes,
    int Serves,
    string? Instructions,
    bool IsSeafood,
    bool IsVegetarian,
    bool IsVegan,
    IReadOnlyList<string> VibeTags,
    IEnumerable<DishIngredientOverview> Ingredients);

public sealed record DishIngredientOverview(
    string Id,
    string IngredientId,
    double Amount,
    string Label);
