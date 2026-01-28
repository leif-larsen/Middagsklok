using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Dishes.Import;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the import and returns the summary with failures.
    public async Task<Response> Execute(Request request, CancellationToken cancellationToken)
    {
        var dishes = request.Dishes ?? Array.Empty<DishInput>();
        var attempted = dishes.Count;
        if (attempted == 0)
        {
            var emptyResponse = new Response(0, 0, 0, 0, Array.Empty<Failure>());
            return emptyResponse;
        }

        var validator = new Validator();
        var failures = new List<Failure>();
        var skipped = 0;
        var failed = 0;

        var normalizedDishNames = dishes
            .Select(d => NormalizeName(d?.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToArray();

        var existingDishNames = await LoadExistingDishNames(normalizedDishNames, cancellationToken);
        var seenDishNames = new HashSet<string>(
            existingDishNames.Select(NormalizeName),
            StringComparer.OrdinalIgnoreCase);

        var allIngredientNames = dishes
            .SelectMany(d => d?.Ingredients ?? Array.Empty<IngredientInput>())
            .Select(i => NormalizeName(i?.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToArray();

        var ingredientByName = await LoadExistingIngredients(allIngredientNames, cancellationToken);
        var dishesToAdd = new List<Dish>();

        foreach (var dish in dishes)
        {
            var normalizedDishName = NormalizeName(dish?.Name);
            if (!string.IsNullOrWhiteSpace(normalizedDishName) && seenDishNames.Contains(normalizedDishName))
            {
                skipped++;
                continue;
            }

            var validation = validator.Validate(dish);
            if (!validation.IsValid || validation.Candidate is null)
            {
                failed++;
                failures.AddRange(validation.Failures);
                continue;
            }

            normalizedDishName = NormalizeName(validation.Candidate.Name);
            seenDishNames.Add(normalizedDishName);

            var ingredients = new List<DishIngredient>();
            foreach (var ingredientCandidate in validation.Candidate.Ingredients)
            {
                var ingredient = GetOrCreateIngredient(ingredientCandidate, ingredientByName);
                var dishIngredient = new DishIngredient(
                    ingredient.Id,
                    ingredientCandidate.Amount,
                    ingredientCandidate.Unit,
                    null,
                    ingredientCandidate.SortOrder);

                ingredients.Add(dishIngredient);
            }

            var dishEntity = new Dish(
                validation.Candidate.Name,
                validation.Candidate.Cuisine,
                validation.Candidate.PrepTimeMinutes,
                validation.Candidate.CookTimeMinutes,
                validation.Candidate.Servings,
                ingredients);

            dishesToAdd.Add(dishEntity);
        }

        if (dishesToAdd.Count > 0)
        {
            _dbContext.Dishes.AddRange(dishesToAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var failureSnapshot = Array.AsReadOnly(failures.ToArray());
        var response = new Response(
            attempted,
            dishesToAdd.Count,
            skipped,
            failed,
            failureSnapshot);

        return response;
    }

    // Loads existing dish names by normalized values.
    private async Task<IReadOnlyList<string>> LoadExistingDishNames(
        IReadOnlyList<string> normalizedDishNames,
        CancellationToken cancellationToken)
    {
        if (normalizedDishNames.Count == 0)
        {
            return Array.Empty<string>();
        }

        var names = await _dbContext.Dishes
            .Where(d => normalizedDishNames.Contains(d.Name.ToUpper()))
            .Select(d => d.Name)
            .ToListAsync(cancellationToken);

        return names;
    }

    // Loads existing ingredients into a lookup keyed by normalized name.
    private async Task<Dictionary<string, Ingredient>> LoadExistingIngredients(
        IReadOnlyList<string> normalizedIngredientNames,
        CancellationToken cancellationToken)
    {
        if (normalizedIngredientNames.Count == 0)
        {
            return new Dictionary<string, Ingredient>(StringComparer.OrdinalIgnoreCase);
        }

        var ingredients = await _dbContext.Ingredients
            .Where(i => normalizedIngredientNames.Contains(i.Name.ToUpper()))
            .ToListAsync(cancellationToken);

        var lookup = ingredients.ToDictionary(
            i => NormalizeName(i.Name),
            i => i,
            StringComparer.OrdinalIgnoreCase);

        return lookup;
    }

    // Gets an existing ingredient or creates a new one from a candidate.
    private Ingredient GetOrCreateIngredient(
        IngredientCandidate candidate,
        IDictionary<string, Ingredient> ingredientByName)
    {
        var normalizedName = NormalizeName(candidate.Name);
        if (ingredientByName.TryGetValue(normalizedName, out var existing))
        {
            return existing;
        }

        var ingredient = new Ingredient(candidate.Name, candidate.Category, candidate.Unit);
        ingredientByName[normalizedName] = ingredient;
        _dbContext.Ingredients.Add(ingredient);

        return ingredient;
    }

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
}
