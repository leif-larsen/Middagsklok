namespace Middagsklok.Api.Features.WeeklyPlans.PlannedDishByDate;

public sealed record Response(
    string Date,
    DishDetails Dish);

public sealed record DishDetails(
    string Id,
    string Name,
    string DishType,
    int PrepMinutes,
    int CookMinutes,
    int Serves,
    string? Instructions,
    bool IsSeafood,
    bool IsVegetarian,
    bool IsVegan,
    IReadOnlyList<string> VibeTags,
    IEnumerable<IngredientDetails> Ingredients);

public sealed record IngredientDetails(
    string IngredientId,
    string Name,
    double Quantity,
    string Unit,
    string? Note);

public sealed record ErrorResponse(string Message);
