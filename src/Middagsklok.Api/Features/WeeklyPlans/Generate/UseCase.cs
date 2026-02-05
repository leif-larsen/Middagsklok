using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Settings;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.Generate;

internal sealed class UseCase(AppDbContext dbContext)
{
    private const int DaysPerWeek = 7;

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

        var dishes = await LoadDishes(cancellationToken);
        if (dishes.Count == 0)
        {
            var errors = new[]
            {
                new ValidationError(string.Empty, "At least one dish is required to generate a weekly plan.")
            };
            var invalidResult = new UseCaseResult(GenerateOutcome.Invalid, null, errors);

            return invalidResult;
        }

        var settings = await LoadSettings(cancellationToken);
        var seafoodPerWeek = settings?.SeafoodPerWeek ?? 2;
        var generation = BuildDays(validation.StartDate, dishes, seafoodPerWeek);

        var plan = await _dbContext.WeeklyPlans
            .Include(existing => existing.Days)
            .FirstOrDefaultAsync(
                existing => existing.StartDate == validation.StartDate,
                cancellationToken);

        if (plan is null)
        {
            plan = new WeeklyPlan(validation.StartDate, generation.Days);
            _dbContext.WeeklyPlans.Add(plan);
        }
        else
        {
            plan.Update(validation.StartDate, generation.Days);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapPlan(plan, generation.Notes);
        var result = new UseCaseResult(GenerateOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Loads the available dishes for selection.
    private async Task<IReadOnlyList<DishCandidate>> LoadDishes(CancellationToken cancellationToken)
    {
        var dishes = await _dbContext.Dishes
            .AsNoTracking()
            .Select(dish => new DishCandidate(dish.Id, dish.IsSeafood))
            .ToListAsync(cancellationToken);

        return dishes;
    }

    // Loads planning settings when they exist.
    private async Task<PlanningSettings?> LoadSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PlanningSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return settings;
    }

    // Builds the planned days while applying the seafood target rule.
    private static GenerationResult BuildDays(
        DateOnly startDate,
        IReadOnlyList<DishCandidate> dishes,
        int seafoodPerWeek)
    {
        var requestedSeafoodCount = Math.Clamp(seafoodPerWeek, 0, DaysPerWeek);
        var seafoodDishIds = dishes
            .Where(dish => dish.IsSeafood)
            .Select(dish => dish.Id)
            .ToArray();
        var nonSeafoodDishIds = dishes
            .Where(dish => !dish.IsSeafood)
            .Select(dish => dish.Id)
            .ToArray();
        var allDishIds = dishes.Select(dish => dish.Id).ToArray();
        var seafoodLookup = dishes.ToDictionary(dish => dish.Id, dish => dish.IsSeafood);
        var seafoodRequirements = BuildSeafoodRequirements(requestedSeafoodCount);
        var notes = new List<string>();
        var days = new PlannedDay[DaysPerWeek];
        var usedDishIds = new HashSet<Guid>();
        var usedSeafoodFallback = false;
        var usedNonSeafoodFallback = false;
        var usedUniquenessRelaxation = false;
        var actualSeafoodCount = 0;

        for (var offset = 0; offset < DaysPerWeek; offset++)
        {
            var requiresSeafood = seafoodRequirements[offset];
            var pick = requiresSeafood
                ? PickDishIdWithFallback(seafoodDishIds, allDishIds, usedDishIds)
                : PickDishIdWithFallback(nonSeafoodDishIds, allDishIds, usedDishIds);

            if (requiresSeafood && pick.UsedFallbackCategory)
            {
                usedSeafoodFallback = true;
            }

            if (!requiresSeafood && pick.UsedFallbackCategory)
            {
                usedNonSeafoodFallback = true;
            }

            if (pick.UsedDuplicate)
            {
                usedUniquenessRelaxation = true;
            }

            if (seafoodLookup[pick.DishId])
            {
                actualSeafoodCount++;
            }

            usedDishIds.Add(pick.DishId);

            days[offset] = new PlannedDay(
                startDate.AddDays(offset),
                new DishSelection(DishSelectionType.Dish, pick.DishId));
        }

        if (usedSeafoodFallback)
        {
            notes.Add("Seafood target was relaxed because no seafood dishes are available.");
        }

        if (usedNonSeafoodFallback)
        {
            notes.Add("Seafood target was relaxed because no non-seafood dishes are available.");
        }

        if (usedUniquenessRelaxation)
        {
            notes.Add(
                $"Dish uniqueness was relaxed because only {allDishIds.Length} unique dish(es) are available for {DaysPerWeek} day(s).");
        }

        if (actualSeafoodCount != requestedSeafoodCount)
        {
            notes.Add(
                $"Requested {requestedSeafoodCount} seafood dish(es), generated {actualSeafoodCount}.");
        }

        return new GenerationResult(days, notes.AsReadOnly());
    }

    // Builds and shuffles the seafood requirements for each day.
    private static bool[] BuildSeafoodRequirements(int requestedSeafoodCount)
    {
        var requirements = new bool[DaysPerWeek];
        for (var index = 0; index < requestedSeafoodCount; index++)
        {
            requirements[index] = true;
        }

        Shuffle(requirements);

        return requirements;
    }

    // Shuffles an array in-place using Fisher-Yates.
    private static void Shuffle(bool[] values)
    {
        for (var index = values.Length - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    // Picks a dish from a preferred pool and falls back to all dishes when needed.
    private static DishPick PickDishIdWithFallback(
        IReadOnlyList<Guid> preferredDishIds,
        IReadOnlyList<Guid> fallbackDishIds,
        IReadOnlySet<Guid> usedDishIds)
    {
        var preferredUnusedDishIds = preferredDishIds
            .Where(dishId => !usedDishIds.Contains(dishId))
            .ToArray();
        if (preferredUnusedDishIds.Length > 0)
        {
            var preferredId = PickDishId(preferredUnusedDishIds);
            return new DishPick(preferredId, false, false);
        }

        var fallbackUnusedDishIds = fallbackDishIds
            .Where(dishId => !usedDishIds.Contains(dishId))
            .ToArray();
        if (fallbackUnusedDishIds.Length > 0)
        {
            var fallbackUnusedId = PickDishId(fallbackUnusedDishIds);
            return new DishPick(fallbackUnusedId, true, false);
        }

        if (preferredDishIds.Count > 0)
        {
            var preferredDuplicateId = PickDishId(preferredDishIds);
            return new DishPick(preferredDuplicateId, false, true);
        }

        var fallbackDuplicateId = PickDishId(fallbackDishIds);
        return new DishPick(fallbackDuplicateId, true, true);
    }

    // Picks a random dish identifier from the available list.
    private static Guid PickDishId(IReadOnlyList<Guid> dishIds) =>
        dishIds[Random.Shared.Next(dishIds.Count)];

    // Maps a weekly plan entity to the response.
    private static Response MapPlan(WeeklyPlan plan, IReadOnlyList<string> notes)
    {
        var days = plan.Days
            .OrderBy(day => day.Date)
            .Select(MapDay)
            .ToArray();

        var response = new Response(
            plan.Id.ToString("D"),
            FormatDate(plan.StartDate),
            days,
            notes);

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

internal sealed record DishCandidate(Guid Id, bool IsSeafood);

internal sealed record DishPick(Guid DishId, bool UsedFallbackCategory, bool UsedDuplicate);

internal sealed record GenerationResult(
    IReadOnlyList<PlannedDay> Days,
    IReadOnlyList<string> Notes);

internal enum GenerateOutcome
{
    Success,
    Invalid
}

internal sealed record UseCaseResult(
    GenerateOutcome Outcome,
    Response? Plan,
    IReadOnlyList<ValidationError> Errors);
