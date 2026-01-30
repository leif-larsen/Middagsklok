using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;

namespace Middagsklok.Api.Features.Ingredients.Delete;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the ingredient deletion workflow.
    public async Task<UseCaseResult> Execute(string id, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(id);

        if (!validation.IsValid)
        {
            var invalidResult = new UseCaseResult(DeleteOutcome.Invalid, validation.Errors);
            return invalidResult;
        }

        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == validation.IngredientId, cancellationToken);

        if (ingredient is null)
        {
            var notFoundError = new ValidationError("id", "Ingredient not found.");
            var notFoundResult = new UseCaseResult(DeleteOutcome.NotFound, new[] { notFoundError });
            return notFoundResult;
        }

        var usageCount = await LoadUsageCount(validation.IngredientId, cancellationToken);
        if (usageCount > 0)
        {
            var inUseError = new ValidationError(
                "id",
                $"Ingredient is used in {usageCount} dish(es) and cannot be deleted.");
            var inUseResult = new UseCaseResult(DeleteOutcome.InUse, new[] { inUseError });
            return inUseResult;
        }

        _dbContext.Ingredients.Remove(ingredient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new UseCaseResult(DeleteOutcome.Success, Array.Empty<ValidationError>());

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
                    .Select(_ => dish.Id))
            .Distinct()
            .CountAsync(cancellationToken);

        return count;
    }
}

internal enum DeleteOutcome
{
    Success,
    Invalid,
    NotFound,
    InUse
}

internal sealed record UseCaseResult(
    DeleteOutcome Outcome,
    IReadOnlyList<ValidationError> Errors);
