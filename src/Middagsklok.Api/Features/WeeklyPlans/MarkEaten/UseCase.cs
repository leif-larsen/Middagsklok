using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.DishHistory;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.MarkEaten;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the weekly plan mark-as-eaten workflow.
    public async Task<UseCaseResult> Execute(string? startDate, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(startDate);

        if (!validation.IsValid)
        {
            var invalidResult = new UseCaseResult(MarkEatenOutcome.Invalid, false, validation.Errors);
            return invalidResult;
        }

        var plan = await _dbContext.WeeklyPlans
            .AsNoTracking()
            .Include(existing => existing.Days)
            .FirstOrDefaultAsync(
                existing => existing.StartDate == validation.StartDate,
                cancellationToken);

        if (plan is null)
        {
            var notFoundResult = new UseCaseResult(
                MarkEatenOutcome.NotFound,
                false,
                Array.Empty<ValidationError>());
            return notFoundResult;
        }

        var alreadyMarked = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .AnyAsync(evt => evt.WeeklyPlanId == plan.Id, cancellationToken);

        if (alreadyMarked)
        {
            var alreadyMarkedResult = new UseCaseResult(
                MarkEatenOutcome.AlreadyMarked,
                false,
                Array.Empty<ValidationError>());
            return alreadyMarkedResult;
        }

        var candidates = BuildCandidates(plan);
        if (candidates.Count == 0)
        {
            var emptyResult = new UseCaseResult(
                MarkEatenOutcome.Success,
                false,
                Array.Empty<ValidationError>());
            return emptyResult;
        }

        var existingKeys = await LoadExistingKeys(candidates, cancellationToken);

        var newEvents = candidates
            .Where(candidate => !existingKeys.Contains(new DishDateKey(candidate.DishId, candidate.EatenOn)))
            .Select(candidate => new DishConsumptionEvent(
                candidate.DishId,
                candidate.EatenOn,
                DishHistorySource.WeeklyPlan,
                plan.Id))
            .ToList();

        if (newEvents.Count == 0)
        {
            var noNewResult = new UseCaseResult(
                MarkEatenOutcome.Success,
                false,
                Array.Empty<ValidationError>());
            return noNewResult;
        }

        _dbContext.DishConsumptionEvents.AddRange(newEvents);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new UseCaseResult(MarkEatenOutcome.Success, true, Array.Empty<ValidationError>());
        return result;
    }

    // Builds dish consumption candidates from the weekly plan days.
    private static IReadOnlyList<DishCandidate> BuildCandidates(WeeklyPlan plan)
    {
        var candidates = plan.Days
            .Where(day => day.Selection.Type == DishSelectionType.Dish && day.Selection.DishId is not null)
            .Select(day => new DishCandidate(day.Selection.DishId!.Value, day.Date))
            .ToArray();

        return candidates;
    }

    // Loads existing dish consumption keys for the candidate set.
    private async Task<HashSet<DishDateKey>> LoadExistingKeys(
        IReadOnlyList<DishCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var dishIds = candidates
            .Select(candidate => candidate.DishId)
            .Distinct()
            .ToArray();

        var dates = candidates
            .Select(candidate => candidate.EatenOn)
            .Distinct()
            .ToArray();

        var existingKeys = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .Where(evt => dishIds.Contains(evt.DishId) && dates.Contains(evt.EatenOn))
            .Select(evt => new DishDateKey(evt.DishId, evt.EatenOn))
            .ToListAsync(cancellationToken);

        var existingSet = existingKeys.ToHashSet();

        return existingSet;
    }

    private sealed record DishCandidate(Guid DishId, DateOnly EatenOn);

    private readonly record struct DishDateKey(Guid DishId, DateOnly EatenOn);
}

internal enum MarkEatenOutcome
{
    Success,
    NotFound,
    Invalid,
    AlreadyMarked
}

internal sealed record UseCaseResult(
    MarkEatenOutcome Outcome,
    bool Created,
    IReadOnlyList<ValidationError> Errors);
