namespace Middagsklok.Domain;

public record Dish(
    Guid Id,
    string Name,
    List<DishIngredient> Ingredients);
