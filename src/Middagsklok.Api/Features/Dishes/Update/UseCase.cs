using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Dishes.Update;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the dish update workflow.
    public async Task<UseCaseResult> Execute(string id, Request request, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(id, request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalidResult = new UseCaseResult(UpdateOutcome.Invalid, null, validation.Errors);
            return invalidResult;
        }

        var dish = await _dbContext.Dishes
            .Include(d => d.Ingredients)
            .FirstOrDefaultAsync(d => d.Id == validation.Candidate.Id, cancellationToken);

        if (dish is null)
        {
            var notFoundError = new ValidationError(ToFieldName(nameof(Dish.Id)), "Dish not found.");
            var notFoundResult = new UseCaseResult(UpdateOutcome.NotFound, null, new[] { notFoundError });
            return notFoundResult;
        }

        var normalizedName = NormalizeName(validation.Candidate.Name);
        var hasDuplicate = await _dbContext.Dishes
            .AsNoTracking()
            .AnyAsync(
                existing => existing.Id != dish.Id
                    && existing.Name.ToUpper() == normalizedName,
                cancellationToken);

        if (hasDuplicate)
        {
            var conflictError = new ValidationError(
                ToFieldName(nameof(Request.Name)),
                $"Dish name '{validation.Candidate.Name}' already exists.");
            var conflictResult = new UseCaseResult(UpdateOutcome.Conflict, null, new[] { conflictError });

            return conflictResult;
        }

        var ingredientsById = await LoadIngredientsById(validation.Candidate.Ingredients, cancellationToken);
        var ingredientsByName = await LoadIngredientsByName(validation.Candidate.Ingredients, cancellationToken);
        var missingErrors = ValidateIngredientIds(validation.Candidate.Ingredients, ingredientsById);

        if (missingErrors.Count > 0)
        {
            var invalidResult = new UseCaseResult(UpdateOutcome.Invalid, null, missingErrors);
            return invalidResult;
        }

        var ingredientLookup = new Dictionary<Guid, Ingredient>();
        var dishIngredients = new List<DishIngredient>();

        foreach (var ingredientCandidate in validation.Candidate.Ingredients)
        {
            var ingredient = ResolveIngredient(ingredientCandidate, ingredientsById, ingredientsByName);
            ingredientLookup[ingredient.Id] = ingredient;

            var dishIngredient = new DishIngredient(
                ingredient.Id,
                ingredientCandidate.Amount,
                ingredient.DefaultUnit,
                null,
                ingredientCandidate.SortOrder);

            dishIngredients.Add(dishIngredient);
        }

        dish.Update(
            validation.Candidate.Name,
            validation.Candidate.DishType,
            validation.Candidate.PrepTimeMinutes,
            validation.Candidate.CookTimeMinutes,
            validation.Candidate.Servings,
            validation.Candidate.Instructions,
            validation.Candidate.IsSeafood,
            validation.Candidate.IsVegetarian,
            validation.Candidate.IsVegan,
            dishIngredients,
            validation.Candidate.VibeTags);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapDish(dish, ingredientLookup);
        var result = new UseCaseResult(UpdateOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Loads existing ingredients by id.
    private async Task<Dictionary<Guid, Ingredient>> LoadIngredientsById(
        IReadOnlyList<IngredientCandidate> ingredients,
        CancellationToken cancellationToken)
    {
        var ids = ingredients
            .Where(ingredient => ingredient.Id is not null)
            .Select(ingredient => ingredient.Id!.Value)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, Ingredient>();
        }

        var items = await _dbContext.Ingredients
            .Where(ingredient => ids.Contains(ingredient.Id))
            .ToListAsync(cancellationToken);

        var lookup = items.ToDictionary(ingredient => ingredient.Id);

        return lookup;
    }

    // Loads existing ingredients by normalized name.
    private async Task<Dictionary<string, Ingredient>> LoadIngredientsByName(
        IReadOnlyList<IngredientCandidate> ingredients,
        CancellationToken cancellationToken)
    {
        var names = ingredients
            .Where(ingredient => ingredient.Id is null)
            .Select(ingredient => NormalizeName(ingredient.Name!))
            .Distinct()
            .ToArray();

        if (names.Length == 0)
        {
            return new Dictionary<string, Ingredient>(StringComparer.OrdinalIgnoreCase);
        }

        var items = await _dbContext.Ingredients
            .Where(ingredient => names.Contains(ingredient.Name.ToUpper()))
            .ToListAsync(cancellationToken);

        var lookup = items.ToDictionary(
            ingredient => NormalizeName(ingredient.Name),
            ingredient => ingredient,
            StringComparer.OrdinalIgnoreCase);

        return lookup;
    }

    // Validates that id-based ingredient references exist.
    private static IReadOnlyList<ValidationError> ValidateIngredientIds(
        IReadOnlyList<IngredientCandidate> ingredients,
        IReadOnlyDictionary<Guid, Ingredient> ingredientsById)
    {
        var failures = new List<ValidationError>();

        foreach (var ingredient in ingredients)
        {
            if (ingredient.Id is null)
            {
                continue;
            }

            if (ingredientsById.ContainsKey(ingredient.Id.Value))
            {
                continue;
            }

            failures.Add(new ValidationError(
                BuildIngredientField(ingredient.Index, nameof(IngredientInput.Id)),
                "Ingredient not found."));
        }

        return failures;
    }

    // Resolves a candidate into an existing or new ingredient entity.
    private Ingredient ResolveIngredient(
        IngredientCandidate candidate,
        IDictionary<Guid, Ingredient> ingredientsById,
        IDictionary<string, Ingredient> ingredientsByName)
    {
        if (candidate.Id is not null)
        {
            return ingredientsById[candidate.Id.Value];
        }

        var normalizedName = NormalizeName(candidate.Name!);
        if (ingredientsByName.TryGetValue(normalizedName, out var existing))
        {
            return existing;
        }

        var ingredient = new Ingredient(candidate.Name!, IngredientCategory.Other, Unit.Pcs);
        ingredientsByName[normalizedName] = ingredient;
        _dbContext.Ingredients.Add(ingredient);

        return ingredient;
    }

    // Maps the updated dish to the response.
    private static Response MapDish(Dish dish, IReadOnlyDictionary<Guid, Ingredient> ingredientLookup)
    {
        var ingredients = dish.Ingredients
            .OrderBy(ingredient => ingredient.SortOrder ?? int.MaxValue)
            .Select((ingredient, index) =>
            {
                var ingredientName = ingredientLookup.TryGetValue(ingredient.IngredientId, out var ingredientEntity)
                    ? ingredientEntity.Name
                    : string.Empty;

                var ingredientId = ingredient.IngredientId;
                var label = BuildIngredientLabel(ingredient.Quantity, ingredient.Unit, ingredientName);
                var id = $"{ingredientId:D}-{index + 1}";

                return new DishIngredientResponse(
                    id,
                    ingredientId.ToString("D"),
                    ingredient.Quantity,
                    label);
            })
            .ToArray();

        var response = new Response(
            dish.Id.ToString("D"),
            dish.Name,
            dish.DishType.ToString(),
            dish.PrepTimeMinutes,
            dish.CookTimeMinutes,
            dish.Servings,
            dish.Instructions,
            dish.IsSeafood,
            dish.IsVegetarian,
            dish.IsVegan,
            dish.VibeTags.ToArray(),
            ingredients);

        return response;
    }

    // Builds a human-readable ingredient label.
    private static string BuildIngredientLabel(double quantity, Unit unit, string name)
    {
        var quantityLabel = FormatQuantity(quantity);
        var unitLabel = FormatUnit(unit);
        var trimmedName = name?.Trim() ?? string.Empty;

        var label = string.Join(
            " ",
            new[] { quantityLabel, unitLabel, trimmedName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return label;
    }

    // Formats ingredient quantities for display.
    private static string FormatQuantity(double quantity)
    {
        if (quantity <= 0)
        {
            return string.Empty;
        }

        var hasFraction = Math.Abs(quantity % 1) > 0.0001;
        var format = hasFraction ? "0.##" : "0";
        var formatted = quantity.ToString(format, CultureInfo.InvariantCulture);

        return formatted;
    }

    // Formats unit values for labels.
    private static string FormatUnit(Unit unit) =>
        unit switch
        {
            Unit.G => "g",
            Unit.Kg => "kg",
            Unit.Ml => "ml",
            Unit.L => "l",
            Unit.Pcs => "pcs",
            _ => string.Empty
        };

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

    // Builds the field name for an ingredient property.
    private static string BuildIngredientField(int index, string property) =>
        $"{ToFieldName(nameof(Request.Ingredients))}[{index}].{ToFieldName(property)}";

    // Converts property names to camelCase field names.
    private static string ToFieldName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        if (propertyName.Length == 1)
        {
            return propertyName.ToLowerInvariant();
        }

        var first = char.ToLowerInvariant(propertyName[0]);

        return $"{first}{propertyName[1..]}";
    }
}

internal enum UpdateOutcome
{
    Success,
    Invalid,
    Conflict,
    NotFound
}

internal sealed record UseCaseResult(
    UpdateOutcome Outcome,
    Response? Dish,
    IReadOnlyList<ValidationError> Errors);
