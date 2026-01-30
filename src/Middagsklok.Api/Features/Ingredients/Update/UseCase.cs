using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.Update;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the ingredient update workflow.
    public async Task<UseCaseResult> Execute(string id, Request request, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(id, request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalidResult = new UseCaseResult(UpdateOutcome.Invalid, null, validation.Errors);
            return invalidResult;
        }

        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == validation.Candidate.Id, cancellationToken);

        if (ingredient is null)
        {
            var notFoundError = new ValidationError(ToFieldName(nameof(Ingredient.Id)), "Ingredient not found.");
            var notFoundResult = new UseCaseResult(
                UpdateOutcome.NotFound,
                null,
                new[] { notFoundError });
            return notFoundResult;
        }

        var normalizedName = NormalizeName(validation.Candidate.Name);
        var hasDuplicate = await _dbContext.Ingredients
            .AsNoTracking()
            .AnyAsync(
                existing => existing.Id != ingredient.Id
                    && existing.Name.ToUpper() == normalizedName,
                cancellationToken);

        if (hasDuplicate)
        {
            var conflictError = new ValidationError(
                ToFieldName(nameof(Request.Name)),
                $"Ingredient name '{validation.Candidate.Name}' already exists.");
            var conflictResult = new UseCaseResult(
                UpdateOutcome.Conflict,
                null,
                new[] { conflictError });

            return conflictResult;
        }

        ingredient.Update(
            validation.Candidate.Name,
            validation.Candidate.Category,
            validation.Candidate.DefaultUnit);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var usedIn = await LoadUsageCount(ingredient.Id, cancellationToken);
        var response = MapIngredient(ingredient, usedIn);
        var result = new UseCaseResult(UpdateOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Loads how many dishes use the ingredient.
    private async Task<int> LoadUsageCount(Guid ingredientId, CancellationToken cancellationToken)
    {
        var count = await _dbContext.Dishes
            .AsNoTracking()
            .SelectMany(
                dish => dish.Ingredients
                    .Where(ingredient => ingredient.IngredientId == ingredientId)
                    .Select(ingredient => dish.Id))
            .Distinct()
            .CountAsync(cancellationToken);

        return count;
    }

    // Maps an ingredient entity to the update response.
    private static Response MapIngredient(Ingredient ingredient, int usedIn) =>
        new(
            ingredient.Id.ToString("D"),
            ingredient.Name,
            ingredient.Category.ToString(),
            ingredient.DefaultUnit.ToString(),
            usedIn);

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

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
    Response? Ingredient,
    IReadOnlyList<ValidationError> Errors);
