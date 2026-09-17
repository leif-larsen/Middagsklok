using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;

namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Delete;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the workflow that clears an ingredient's Oda mapping.
    public async Task<UseCaseResult> Execute(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var ingredientId))
        {
            var invalidError = new ValidationError("id", "Ingredient id is invalid.");
            var invalidResult = new UseCaseResult(DeleteOutcome.Invalid, new[] { invalidError });
            return invalidResult;
        }

        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == ingredientId, cancellationToken);

        if (ingredient is null)
        {
            var notFoundError = new ValidationError("id", "Ingredient not found.");
            var notFoundResult = new UseCaseResult(DeleteOutcome.NotFound, new[] { notFoundError });
            return notFoundResult;
        }

        if (ingredient.OdaMapping is null)
        {
            var noMappingError = new ValidationError("id", "Ingredient has no Oda mapping.");
            var noMappingResult = new UseCaseResult(DeleteOutcome.NotFound, new[] { noMappingError });
            return noMappingResult;
        }

        ingredient.ClearOdaMapping();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new UseCaseResult(DeleteOutcome.Success, Array.Empty<ValidationError>());

        return result;
    }
}

internal enum DeleteOutcome
{
    Success,
    Invalid,
    NotFound
}

internal sealed record UseCaseResult(
    DeleteOutcome Outcome,
    IReadOnlyList<ValidationError> Errors);
