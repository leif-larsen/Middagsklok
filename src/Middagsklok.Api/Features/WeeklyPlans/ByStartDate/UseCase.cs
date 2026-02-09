using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.ByStartDate;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the weekly plan retrieval workflow.
    public async Task<UseCaseResult> Execute(string? startDate, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(startDate);

        if (!validation.IsValid)
        {
            var invalidResult = new UseCaseResult(FetchOutcome.Invalid, null, validation.Errors);
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
                FetchOutcome.NotFound,
                null,
                Array.Empty<ValidationError>());
            return notFoundResult;
        }

        var isMarkedAsEaten = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .AnyAsync(evt => evt.WeeklyPlanId == plan.Id, cancellationToken);

        var response = MapPlan(plan, isMarkedAsEaten);
        var result = new UseCaseResult(FetchOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Maps a weekly plan entity to the response.
    private static Response MapPlan(WeeklyPlan plan, bool isMarkedAsEaten)
    {
        var days = plan.Days
            .OrderBy(day => day.Date)
            .Select(MapDay)
            .ToArray();

        var response = new Response(
            plan.Id.ToString("D"),
            FormatDate(plan.StartDate),
            days,
            isMarkedAsEaten);

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

internal enum FetchOutcome
{
    Success,
    NotFound,
    Invalid
}

internal sealed record UseCaseResult(
    FetchOutcome Outcome,
    Response? Plan,
    IReadOnlyList<ValidationError> Errors);
