using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.Create;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the ingredient creation workflow.
    public async Task<UseCaseResult> Execute(Request request, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalidResult = new UseCaseResult(false, null, validation.Errors);
            return invalidResult;
        }

        var normalizedName = NormalizeName(validation.Candidate.Name);
        var existing = await _dbContext.Ingredients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ingredient => ingredient.Name.ToUpper() == normalizedName,
                cancellationToken);

        if (existing is not null)
        {
            var usedIn = await LoadUsageCount(existing.Id, cancellationToken);
            var existingResponse = MapIngredient(existing, usedIn);
            var existingResult = new UseCaseResult(true, existingResponse, Array.Empty<ValidationError>());

            return existingResult;
        }

        var ingredient = new Ingredient(
            validation.Candidate.Name,
            validation.Candidate.Category,
            validation.Candidate.DefaultUnit);

        _dbContext.Ingredients.Add(ingredient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var createdResponse = MapIngredient(ingredient, 0);
        var result = new UseCaseResult(true, createdResponse, Array.Empty<ValidationError>());

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

    // Maps an ingredient entity to the create response.
    private static Response MapIngredient(Ingredient ingredient, int usedIn) =>
        new(
            ingredient.Id.ToString("D"),
            ingredient.Name,
            ingredient.Category.ToString(),
            ingredient.DefaultUnit.ToString(),
            usedIn);

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string value) => value.Trim().ToUpperInvariant();
}

internal sealed record UseCaseResult(
    bool IsValid,
    Response? Ingredient,
    IReadOnlyList<ValidationError> Errors);
