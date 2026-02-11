using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Settings;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.Generate;

internal sealed class UseCase(AppDbContext dbContext, IRandomSource? randomSource = null)
{
    private const int DaysPerWeek = 7;

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IRandomSource _randomSource = randomSource ?? SharedRandomSource.Instance;

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

        var plan = await _dbContext.WeeklyPlans
            .Include(existing => existing.Days)
            .FirstOrDefaultAsync(
                existing => existing.StartDate == validation.StartDate,
                cancellationToken);

        if (plan is not null)
        {
            var isMarkedAsEaten = await IsPlanMarkedAsEaten(plan.Id, cancellationToken);
            if (isMarkedAsEaten)
            {
                var conflictResult = new UseCaseResult(
                    GenerateOutcome.Conflict,
                    null,
                    Array.Empty<ValidationError>());
                return conflictResult;
            }
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
        var daysBetween = settings?.DaysBetween ?? 14;
        var lastEatenDates = await LoadLastEatenDates(dishes, cancellationToken);
        var generation = BuildDays(validation.StartDate, dishes, seafoodPerWeek, daysBetween, lastEatenDates, _randomSource);

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
        var dishEntities = await _dbContext.Dishes
            .AsNoTracking()
            .OrderBy(dish => dish.Name)
            .ToListAsync(cancellationToken);

        var dishes = dishEntities
            .Select(dish => new DishCandidate(dish.Id, dish.IsSeafood, dish.Cuisine, dish.VibeTags.ToArray()))
            .ToArray();

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

    // Loads the most recent consumption date for each dish.
    private async Task<IReadOnlyDictionary<Guid, DateOnly>> LoadLastEatenDates(
        IReadOnlyList<DishCandidate> dishes,
        CancellationToken cancellationToken)
    {
        if (dishes.Count == 0)
        {
            return new Dictionary<Guid, DateOnly>();
        }

        var dishIds = dishes
            .Select(dish => dish.Id)
            .Distinct()
            .ToArray();

        var lastEaten = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .Where(evt => dishIds.Contains(evt.DishId))
            .GroupBy(evt => evt.DishId)
            .Select(group => new
            {
                DishId = group.Key,
                LastEaten = group.Max(evt => evt.EatenOn)
            })
            .ToListAsync(cancellationToken);

        var lookup = lastEaten.ToDictionary(entry => entry.DishId, entry => entry.LastEaten);

        return lookup;
    }

    // Checks whether a weekly plan has already been marked as eaten.
    private async Task<bool> IsPlanMarkedAsEaten(Guid planId, CancellationToken cancellationToken)
    {
        var isMarkedAsEaten = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .AnyAsync(evt => evt.WeeklyPlanId == planId, cancellationToken);

        return isMarkedAsEaten;
    }

    // Builds the planned days while applying the seafood target rule.
    private static GenerationResult BuildDays(
        DateOnly startDate,
        IReadOnlyList<DishCandidate> dishes,
        int seafoodPerWeek,
        int daysBetween,
        IReadOnlyDictionary<Guid, DateOnly> lastEatenDates,
        IRandomSource randomSource)
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
        var typeLookup = dishes.ToDictionary(dish => dish.Id, dish => dish.Cuisine);
        var vibeTagLookup = dishes.ToDictionary(dish => dish.Id, dish => dish.VibeTags);
        var seafoodRequirements = BuildSeafoodRequirements(requestedSeafoodCount, randomSource);
        var notes = new List<string>();
        var days = new PlannedDay[DaysPerWeek];
        var usedDishIds = new HashSet<Guid>();
        var usedSeafoodFallback = false;
        var usedNonSeafoodFallback = false;
        var usedUniquenessRelaxation = false;
        var actualSeafoodCount = 0;

        for (var offset = 0; offset < DaysPerWeek; offset++)
        {
            var dayDate = startDate.AddDays(offset);
            var requiresSeafood = seafoodRequirements[offset];
            var pick = requiresSeafood
                ? PickDishIdWithFallback(
                    seafoodDishIds,
                    allDishIds,
                    usedDishIds,
                    dayDate,
                    daysBetween,
                    lastEatenDates,
                    typeLookup,
                    vibeTagLookup,
                    randomSource)
                : PickDishIdWithFallback(
                    nonSeafoodDishIds,
                    allDishIds,
                    usedDishIds,
                    dayDate,
                    daysBetween,
                    lastEatenDates,
                    typeLookup,
                    vibeTagLookup,
                    randomSource);

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
                dayDate,
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
    private static bool[] BuildSeafoodRequirements(int requestedSeafoodCount, IRandomSource randomSource)
    {
        var requirements = new bool[DaysPerWeek];
        for (var index = 0; index < requestedSeafoodCount; index++)
        {
            requirements[index] = true;
        }

        Shuffle(requirements, randomSource);

        return requirements;
    }

    // Shuffles an array in-place using Fisher-Yates.
    private static void Shuffle(bool[] values, IRandomSource randomSource)
    {
        for (var index = values.Length - 1; index > 0; index--)
        {
            var swapIndex = randomSource.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    // Picks a dish from a preferred pool and falls back to all dishes when needed.
    private static DishPick PickDishIdWithFallback(
        IReadOnlyList<Guid> preferredDishIds,
        IReadOnlyList<Guid> fallbackDishIds,
        IReadOnlySet<Guid> usedDishIds,
        DateOnly dayDate,
        int daysBetween,
        IReadOnlyDictionary<Guid, DateOnly> lastEatenDates,
        IReadOnlyDictionary<Guid, CuisineType> typeLookup,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> vibeTagLookup,
        IRandomSource randomSource)
    {
        var preferredUnusedDishIds = preferredDishIds
            .Where(dishId => !usedDishIds.Contains(dishId))
            .ToArray();
        if (preferredUnusedDishIds.Length > 0)
        {
            var preferredId = PickDishId(preferredUnusedDishIds, dayDate, daysBetween, lastEatenDates, typeLookup, vibeTagLookup, randomSource);
            return new DishPick(preferredId, false, false);
        }

        var fallbackUnusedDishIds = fallbackDishIds
            .Where(dishId => !usedDishIds.Contains(dishId))
            .ToArray();
        if (fallbackUnusedDishIds.Length > 0)
        {
            var fallbackUnusedId = PickDishId(fallbackUnusedDishIds, dayDate, daysBetween, lastEatenDates, typeLookup, vibeTagLookup, randomSource);
            return new DishPick(fallbackUnusedId, true, false);
        }

        if (preferredDishIds.Count > 0)
        {
            var preferredDuplicateId = PickDishId(preferredDishIds, dayDate, daysBetween, lastEatenDates, typeLookup, vibeTagLookup, randomSource);
            return new DishPick(preferredDuplicateId, false, true);
        }

        var fallbackDuplicateId = PickDishId(fallbackDishIds, dayDate, daysBetween, lastEatenDates, typeLookup, vibeTagLookup, randomSource);
        return new DishPick(fallbackDuplicateId, true, true);
    }

    // Picks a dish identifier based on recency scoring.
    private static Guid PickDishId(
        IReadOnlyList<Guid> dishIds,
        DateOnly dayDate,
        int daysBetween,
        IReadOnlyDictionary<Guid, DateOnly> lastEatenDates,
        IReadOnlyDictionary<Guid, CuisineType> typeLookup,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> vibeTagLookup,
        IRandomSource randomSource)
    {
        if (dishIds.Count == 1)
        {
            return dishIds[0];
        }

        var totalScore = 0.0;
        var scores = new double[dishIds.Count];

        for (var index = 0; index < dishIds.Count; index++)
        {
            var dishId = dishIds[index];
            var score = CalculateRecencyScore(dishId, dayDate, daysBetween, lastEatenDates);
            var typeWeight = CalculateTypeWeight(dishId, dayDate, typeLookup);
            var vibeWeight = CalculateVibeWeight(dishId, dayDate, vibeTagLookup);
            var weightedScore = score * typeWeight * vibeWeight;
            scores[index] = weightedScore;
            totalScore += weightedScore;
        }

        if (totalScore <= 0)
        {
            return dishIds[randomSource.Next(dishIds.Count)];
        }

        var roll = randomSource.NextDouble() * totalScore;
        var cumulative = 0.0;

        for (var index = 0; index < scores.Length; index++)
        {
            cumulative += scores[index];
            if (roll < cumulative)
            {
                return dishIds[index];
            }
        }

        return dishIds[^1];
    }

    // Calculates the planner weight for the dish type on the given day.
    private static double CalculateTypeWeight(
        Guid dishId,
        DateOnly dayDate,
        IReadOnlyDictionary<Guid, CuisineType> typeLookup)
    {
        if (!typeLookup.TryGetValue(dishId, out var dishType))
        {
            return 1.0;
        }

        var weight = DishTaxonomy.GetDefaultWeight(dishType, dayDate.DayOfWeek);
        return weight <= 0 ? 0.1 : weight;
    }

    // Calculates the combined planner multiplier for all selected dish vibe tags.
    private static double CalculateVibeWeight(
        Guid dishId,
        DateOnly dayDate,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> vibeTagLookup)
    {
        if (!vibeTagLookup.TryGetValue(dishId, out var vibeTags) || vibeTags.Count == 0)
        {
            return 1.0;
        }

        var weight = 1.0;
        for (var index = 0; index < vibeTags.Count; index++)
        {
            var multiplier = DishTaxonomy.GetVibeWeightMultiplier(vibeTags[index], dayDate.DayOfWeek);
            weight *= multiplier;
        }

        return weight <= 0 ? 0.1 : weight;
    }

    // Calculates a recency score for weighted selection.
    private static int CalculateRecencyScore(
        Guid dishId,
        DateOnly dayDate,
        int daysBetween,
        IReadOnlyDictionary<Guid, DateOnly> lastEatenDates)
    {
        if (daysBetween <= 0)
        {
            return 1;
        }

        if (!lastEatenDates.TryGetValue(dishId, out var lastEaten))
        {
            return daysBetween + 1;
        }

        var daysSince = dayDate.DayNumber - lastEaten.DayNumber;
        if (daysSince <= 0)
        {
            return 1;
        }

        if (daysSince <= daysBetween)
        {
            return daysSince;
        }

        return daysBetween + 1;
    }

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

internal sealed record DishCandidate(Guid Id, bool IsSeafood, CuisineType Cuisine, IReadOnlyList<string> VibeTags);

internal sealed record DishPick(Guid DishId, bool UsedFallbackCategory, bool UsedDuplicate);

internal sealed record GenerationResult(
    IReadOnlyList<PlannedDay> Days,
    IReadOnlyList<string> Notes);

internal enum GenerateOutcome
{
    Success,
    Invalid,
    Conflict
}

internal sealed record UseCaseResult(
    GenerateOutcome Outcome,
    Response? Plan,
    IReadOnlyList<ValidationError> Errors);

internal interface IRandomSource
{
    // Returns a non-negative random integer less than the specified maximum.
    int Next(int maxValue);

    // Returns a random floating-point value in [0.0, 1.0).
    double NextDouble();
}

internal sealed class SharedRandomSource : IRandomSource
{
    public static SharedRandomSource Instance { get; } = new();

    // Prevents additional instances.
    private SharedRandomSource()
    {
    }

    // Returns a non-negative random integer less than maxValue.
    public int Next(int maxValue) => Random.Shared.Next(maxValue);

    // Returns a random floating-point value in [0.0, 1.0).
    public double NextDouble() => Random.Shared.NextDouble();
}
