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

        var planReferenceCount = await _dbContext.WeeklyPlans
            .AsNoTracking()
            .SelectMany(plan => plan.Days)
            .CountAsync(day => day.Selection.DishId == dish.Id, cancellationToken);

        var consumptionReferenceCount = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .CountAsync(evt => evt.DishId == dish.Id, cancellationToken);

        if (planReferenceCount > 0 || consumptionReferenceCount > 0)
        {
            var message = $"Dish is referenced by {planReferenceCount} weekly plan day(s) and {consumptionReferenceCount} consumption event(s). Retire the dish instead.";
            var conflictError = new ValidationError("id", message);
            var conflictResult = new UseCaseResult(DeleteOutcome.Conflict, new[] { conflictError });
            return conflictResult;
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
    NotFound,
    Conflict
}

internal sealed record UseCaseResult(
    DeleteOutcome Outcome,
    IReadOnlyList<ValidationError> Errors);
