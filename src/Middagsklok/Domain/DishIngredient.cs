namespace Middagsklok.Domain;

public record DishIngredient(
    Ingredient Ingredient,
    decimal Amount,
    string Unit,
    bool Optional);
