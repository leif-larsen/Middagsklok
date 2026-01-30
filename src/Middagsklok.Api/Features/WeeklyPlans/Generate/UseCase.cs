using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.Generate;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the weekly plan generation workflow.
    public async Task<UseCaseResult> Execute(string? startDateValue, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(startDateValue);

        if (!validation.IsValid)
        {
            var invalidResult = new UseCaseResult(GenerateOutcome.Invalid, null, validation.Errors);
            return invalidResult;
        }

        var dishIds = await LoadDishIds(cancellationToken);
        if (dishIds.Count == 0)
        {
            var errors = new[]
            {
                new ValidationError(string.Empty, "At least one dish is required to generate a weekly plan.")
            };
            var invalidResult = new UseCaseResult(GenerateOutcome.Invalid, null, errors);

            return invalidResult;
        }

        var days = BuildDays(validation.StartDate, dishIds);

        var plan = await _dbContext.WeeklyPlans
            .Include(existing => existing.Days)
            .FirstOrDefaultAsync(
                existing => existing.StartDate == validation.StartDate,
                cancellationToken);

        if (plan is null)
        {
            plan = new WeeklyPlan(validation.StartDate, days);
            _dbContext.WeeklyPlans.Add(plan);
        }
        else
        {
            plan.Update(validation.StartDate, days);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapPlan(plan);
        var result = new UseCaseResult(GenerateOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Loads the available dish identifiers for selection.
    private async Task<IReadOnlyList<Guid>> LoadDishIds(CancellationToken cancellationToken)
    {
        var dishIds = await _dbContext.Dishes
            .AsNoTracking()
            .Select(dish => dish.Id)
            .ToListAsync(cancellationToken);

        return dishIds;
    }

    // Builds the planned days for the requested week.
    private static IReadOnlyList<PlannedDay> BuildDays(DateOnly startDate, IReadOnlyList<Guid> dishIds)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset => new PlannedDay(
                startDate.AddDays(offset),
                new DishSelection(DishSelectionType.Dish, PickDishId(dishIds))))
            .ToArray();

        return days;
    }

    // Picks a random dish identifier from the available list.
    private static Guid PickDishId(IReadOnlyList<Guid> dishIds) =>
        dishIds[Random.Shared.Next(dishIds.Count)];

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

internal enum GenerateOutcome
{
    Success,
    Invalid
}

internal sealed record UseCaseResult(
    GenerateOutcome Outcome,
    Response? Plan,
    IReadOnlyList<ValidationError> Errors);
