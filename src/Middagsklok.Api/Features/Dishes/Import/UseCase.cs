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
        var ingredientByMatchKey = await LoadIngredientsByMatchKey(cancellationToken);
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

            var duplicateFailures = FindNearDuplicateIngredients(
                validation.Candidate,
                ingredientByName,
                ingredientByMatchKey);

            if (duplicateFailures.Count > 0)
            {
                failed++;
                failures.AddRange(duplicateFailures);
                continue;
            }

            normalizedDishName = NormalizeName(validation.Candidate.Name);
            seenDishNames.Add(normalizedDishName);

            var isSeafood = IsSeafoodDish(validation.Candidate);
            var isVegetarian = IsVegetarianDish(validation.Candidate);
            var isVegan = IsVeganDish(validation.Candidate);

            var ingredients = new List<DishIngredient>();
            foreach (var ingredientCandidate in validation.Candidate.Ingredients)
            {
                var ingredient = GetOrCreateIngredient(ingredientCandidate, ingredientByName, ingredientByMatchKey);
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
                validation.Candidate.DishType,
                validation.Candidate.PrepTimeMinutes,
                validation.Candidate.CookTimeMinutes,
                validation.Candidate.Servings,
                null,
                isSeafood,
                isVegetarian,
                isVegan,
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

    // Loads every existing ingredient keyed by its loose match key, for near-duplicate detection.
    private async Task<Dictionary<string, Ingredient>> LoadIngredientsByMatchKey(CancellationToken cancellationToken)
    {
        var items = await _dbContext.Ingredients
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var lookup = new Dictionary<string, Ingredient>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            lookup.TryAdd(IngredientNameMatching.MatchKey(item.Name), item);
        }

        return lookup;
    }

    // Reports imported ingredient names that denote a product already held under different wording.
    private static IReadOnlyList<Failure> FindNearDuplicateIngredients(
        DishCandidate candidate,
        IDictionary<string, Ingredient> ingredientByName,
        IDictionary<string, Ingredient> ingredientByMatchKey)
    {
        var failures = new List<Failure>();
        var newNamesByMatchKey = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var ingredient in candidate.Ingredients)
        {
            // An exact name match already resolves to the existing record, so it is never a duplicate.
            if (ingredientByName.ContainsKey(NormalizeName(ingredient.Name)))
            {
                continue;
            }

            var matchKey = IngredientNameMatching.MatchKey(ingredient.Name);

            if (matchKey.Length == 0)
            {
                continue;
            }

            if (ingredientByMatchKey.TryGetValue(matchKey, out var existing))
            {
                failures.Add(new Failure(
                    candidate.Name,
                    $"Ingredient looks like the existing ingredient '{existing.Name}'. Reuse that name, or rename it if it is a different product.",
                    ingredient.Name));
                continue;
            }

            if (newNamesByMatchKey.TryGetValue(matchKey, out var earlier))
            {
                failures.Add(new Failure(
                    candidate.Name,
                    $"Ingredient looks like '{earlier}' in the same dish. Use one name per product.",
                    ingredient.Name));
                continue;
            }

            newNamesByMatchKey[matchKey] = ingredient.Name;
        }

        return failures;
    }

    // Gets an existing ingredient or creates a new one from a candidate.
    private Ingredient GetOrCreateIngredient(
        IngredientCandidate candidate,
        IDictionary<string, Ingredient> ingredientByName,
        IDictionary<string, Ingredient> ingredientByMatchKey)
    {
        var normalizedName = NormalizeName(candidate.Name);
        if (ingredientByName.TryGetValue(normalizedName, out var existing))
        {
            return existing;
        }

        var ingredient = new Ingredient(candidate.Name, candidate.Category, candidate.Unit);
        ingredientByName[normalizedName] = ingredient;
        ingredientByMatchKey.TryAdd(IngredientNameMatching.MatchKey(candidate.Name), ingredient);
        _dbContext.Ingredients.Add(ingredient);

        return ingredient;
    }

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    // Determines if an imported dish should be marked as seafood.
    private static bool IsSeafoodDish(DishCandidate candidate) =>
        candidate.Ingredients.Any(ingredient => ingredient.Category == IngredientCategory.Seafood);

    // Determines if an imported dish should be marked as vegetarian.
    private static bool IsVegetarianDish(DishCandidate candidate) =>
        candidate.Ingredients.All(ingredient =>
            ingredient.Category is not (
                IngredientCategory.Meat
                or IngredientCategory.Poultry
                or IngredientCategory.Seafood));

    // Determines if an imported dish should be marked as vegan.
    private static bool IsVeganDish(DishCandidate candidate) =>
        candidate.Ingredients.All(ingredient =>
            ingredient.Category is not (
                IngredientCategory.Meat
                or IngredientCategory.Poultry
                or IngredientCategory.Seafood
                or IngredientCategory.DairyAndEggs));
}
