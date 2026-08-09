using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;

namespace Middagsklok.Api.Features.Dishes.Retire;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Retires or un-retires a dish.
    public async Task<UseCaseResult> Execute(string id, bool retire, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var dishId))
        {
            var invalidError = new ValidationError("id", "Invalid dish id.");
            return new UseCaseResult(RetireOutcome.Invalid, [invalidError]);
        }

        var dish = await _dbContext.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId, cancellationToken);

        if (dish is null)
        {
            var notFoundError = new ValidationError("id", "Dish not found.");
            return new UseCaseResult(RetireOutcome.NotFound, [notFoundError]);
        }

        if (retire)
        {
            dish.Retire();
        }
        else
        {
            dish.Unretire();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UseCaseResult(RetireOutcome.Success, []);
    }
}

internal enum RetireOutcome
{
    Success,
    Invalid,
    NotFound
}

internal sealed record UseCaseResult(
    RetireOutcome Outcome,
    IReadOnlyList<ValidationError> Errors);

internal sealed record ValidationError(string Field, string Message);
