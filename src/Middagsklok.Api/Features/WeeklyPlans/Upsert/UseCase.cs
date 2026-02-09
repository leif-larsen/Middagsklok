using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.Upsert;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the weekly plan upsert workflow.
    public async Task<UseCaseResult> Execute(
        string? startDate,
        Request request,
        CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(startDate, request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalidResult = new UseCaseResult(UpsertOutcome.Invalid, null, validation.Errors);
            return invalidResult;
        }

        var plan = await _dbContext.WeeklyPlans
            .Include(existing => existing.Days)
            .FirstOrDefaultAsync(
                existing => existing.StartDate == validation.Candidate.StartDate,
                cancellationToken);

        if (plan is not null)
        {
            var isMarkedAsEaten = await IsPlanMarkedAsEaten(plan.Id, cancellationToken);
            if (isMarkedAsEaten)
            {
                var conflictResult = new UseCaseResult(
                    UpsertOutcome.Conflict,
                    null,
                    Array.Empty<ValidationError>());
                return conflictResult;
            }
        }

        var missingDishErrors = await ValidateDishIds(validation.Candidate.Days, cancellationToken);
        if (missingDishErrors.Count > 0)
        {
            var invalidResult = new UseCaseResult(UpsertOutcome.Invalid, null, missingDishErrors);
            return invalidResult;
        }

        var days = validation.Candidate.Days
            .OrderBy(day => day.Date)
            .Select(day => new PlannedDay(
                day.Date,
                new DishSelection(day.SelectionType, day.DishId)))
            .ToArray();

        if (plan is null)
        {
            plan = new WeeklyPlan(validation.Candidate.StartDate, days);
            _dbContext.WeeklyPlans.Add(plan);
        }
        else
        {
            plan.Update(validation.Candidate.StartDate, days);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapPlan(plan);
        var result = new UseCaseResult(UpsertOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Validates that referenced dish ids exist.
    private async Task<IReadOnlyList<ValidationError>> ValidateDishIds(
        IReadOnlyList<PlannedDayCandidate> days,
        CancellationToken cancellationToken)
    {
        var dishIds = days
            .Where(day => day.SelectionType == DishSelectionType.Dish && day.DishId is not null)
            .Select(day => day.DishId!.Value)
            .Distinct()
            .ToArray();

        if (dishIds.Length == 0)
        {
            return Array.Empty<ValidationError>();
        }

        var existingIds = await _dbContext.Dishes
            .AsNoTracking()
            .Where(dish => dishIds.Contains(dish.Id))
            .Select(dish => dish.Id)
            .ToListAsync(cancellationToken);

        var missingIds = dishIds
            .Except(existingIds)
            .ToHashSet();

        if (missingIds.Count == 0)
        {
            return Array.Empty<ValidationError>();
        }

        var errors = days
            .Where(day => day.DishId is not null && missingIds.Contains(day.DishId.Value))
            .Select(day => new ValidationError(
                Validator.BuildDayField(day.Index, nameof(DishSelectionInput.DishId)),
                "Dish not found."))
            .ToArray();

        return errors;
    }

    // Checks whether a weekly plan has already been marked as eaten.
    private async Task<bool> IsPlanMarkedAsEaten(Guid planId, CancellationToken cancellationToken)
    {
        var isMarkedAsEaten = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .AnyAsync(evt => evt.WeeklyPlanId == planId, cancellationToken);

        return isMarkedAsEaten;
    }

    // Maps a weekly plan entity to the response.
    private static Response MapPlan(WeeklyPlan plan)
    {
        var days = plan.Days
            .OrderBy(day => day.Date)
            .Select(MapDay)
            .ToArray();

        var response = new Response(
            plan.Id.ToString("D"),
            FormatDate(plan.StartDate),
            days);

        return response;
    }

    // Maps a planned day entity to the response.
    private static PlannedDayResponse MapDay(PlannedDay day) =>
        new(
            FormatDate(day.Date),
            new DishSelectionResponse(
                FormatSelectionType(day.Selection.Type),
                day.Selection.DishId?.ToString("D")));

    // Formats selection types for the API response.
    private static string FormatSelectionType(DishSelectionType type) =>
        type.ToString().ToUpperInvariant();

    // Formats date values for the API response.
    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

internal enum UpsertOutcome
{
    Success,
    Invalid,
    Conflict
}

internal sealed record UseCaseResult(
    UpsertOutcome Outcome,
    Response? Plan,
    IReadOnlyList<ValidationError> Errors);
