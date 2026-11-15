namespace Middagsklok.Api.Features.Recipes.ScaledIngredients;

internal sealed class Validator
{
    public const int MinServings = 1;
    public const int MaxServings = 30;

    // Validates the dish id and the optional servings count.
    public ValidationResult Validate(string? dishIdValue, int? servings)
    {
        var failures = new List<ValidationError>();

        if (!Guid.TryParse(dishIdValue, out var dishId))
        {
            failures.Add(new ValidationError("dishId", "Dish id is invalid."));
        }

        if (servings is < MinServings or > MaxServings)
        {
            failures.Add(new ValidationError(
                "servings",
                $"Servings must be between {MinServings} and {MaxServings}."));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, Guid.Empty, failures);
        }

        return new ValidationResult(true, dishId, Array.Empty<ValidationError>());
    }
}

internal sealed record ValidationResult(
    bool IsValid,
    Guid DishId,
    IReadOnlyList<ValidationError> Errors);
