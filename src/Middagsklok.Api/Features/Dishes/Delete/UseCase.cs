using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;

namespace Middagsklok.Api.Features.Dishes.Delete;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the dish deletion workflow.
    public async Task<UseCaseResult> Execute(string id, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(id);

        if (!validation.IsValid)
        {
            var invalidResult = new UseCaseResult(DeleteOutcome.Invalid, validation.Errors);
            return invalidResult;
        }

        var dish = await _dbContext.Dishes
            .FirstOrDefaultAsync(d => d.Id == validation.DishId, cancellationToken);

        if (dish is null)
        {
            var notFoundError = new ValidationError("id", "Dish not found.");
            var notFoundResult = new UseCaseResult(DeleteOutcome.NotFound, new[] { notFoundError });
            return notFoundResult;
        }

        _dbContext.Dishes.Remove(dish);
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
