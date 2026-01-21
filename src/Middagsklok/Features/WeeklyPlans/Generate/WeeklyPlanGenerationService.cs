using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Generate;

/// <summary>
/// Service that encapsulates the core weekly plan generation logic.
/// This service generates meal plans without persisting them to the database.
/// </summary>
public class WeeklyPlanGenerationService : IWeeklyPlanGenerationService
{
    private readonly IDishRepository _dishRepository;
    private readonly IDishHistoryRepository _historyRepository;
    private readonly WeeklyPlanRulesValidator _validator;

    public WeeklyPlanGenerationService(
        IDishRepository dishRepository,
        IDishHistoryRepository historyRepository,
        WeeklyPlanRulesValidator validator)
    {
        _dishRepository = dishRepository;
        _historyRepository = historyRepository;
        _validator = validator;
    }

    public async Task<GeneratedWeeklyPlanResult> Generate(
        GenerateWeeklyPlanRequest request,
        CancellationToken ct = default)
    {
        var rules = request.Rules ?? new PlanningRules();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Step A: Load candidates
        var allDishes = await _dishRepository.GetAllWithIngredients(ct);
        var lastEaten = await _historyRepository.GetLastEatenByDish(ct);

        // Filter candidates by time constraints per day
        var candidatesByDay = new Dictionary<int, List<Dish>>();
        for (int dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            var maxTime = IsWeekday(dayIndex) ? rules.WeekdayMaxTotalMinutes : rules.WeekendMaxTotalMinutes;
            candidatesByDay[dayIndex] = allDishes
                .Where(d => d.TotalMinutes <= maxTime)
                .ToList();
        }

        // Step B: Determine fish dishes
        var fishDishes = allDishes.Where(IsFishDish).ToList();

        // Step C: Calculate scores
        var dishScores = CalculateDishScores(allDishes, lastEaten, today);

        // Step D: Plan construction (deterministic greedy)
        var selectedDishes = new Dictionary<int, Dish>();
        var explanations = new Dictionary<int, PlannedDishExplanation>();

        // First, satisfy fish minimum (2 dishes)
        var fishDaysNeeded = rules.MinFishDinnersPerWeek;
        var fishDaysAssigned = 0;

        // Prefer weekdays for fish (days 0-4), then weekends
        var daysForFish = Enumerable.Range(0, 5).Concat(Enumerable.Range(5, 2)).ToList();
        
        foreach (var dayIndex in daysForFish)
        {
            if (fishDaysAssigned >= fishDaysNeeded) break;

            var availableFish = candidatesByDay[dayIndex]
                .Where(d => IsFishDish(d))
                .Where(d => !selectedDishes.Values.Contains(d))
                .ToList();

            if (availableFish.Count > 0)
            {
                var selectedFish = SelectBestDish(availableFish, dishScores, dayIndex, isWeekday: IsWeekday(dayIndex));
                selectedDishes[dayIndex] = selectedFish;
                explanations[dayIndex] = CreateExplanation(selectedFish, lastEaten, today, isFish: true);
                fishDaysAssigned++;
            }
        }

        // Fill remaining days
        for (int dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            if (selectedDishes.ContainsKey(dayIndex)) continue;

            var availableDishes = candidatesByDay[dayIndex]
                .Where(d => !selectedDishes.Values.Contains(d))
                .ToList();

            if (availableDishes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No available dishes for day {dayIndex} that satisfy time constraints and haven't been used.");
            }

            var selectedDish = SelectBestDish(availableDishes, dishScores, dayIndex, isWeekday: IsWeekday(dayIndex));
            selectedDishes[dayIndex] = selectedDish;
            explanations[dayIndex] = CreateExplanation(selectedDish, lastEaten, today, isFish: IsFishDish(selectedDish));
        }

        // Step E: Build plan
        var items = selectedDishes
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new WeeklyPlanItem(kvp.Key, kvp.Value))
            .ToList();

        var plan = new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: request.WeekStartDate,
            CreatedAt: DateTimeOffset.UtcNow,
            Items: items);

        // Step F: Validate
        var violations = _validator.Validate(plan, rules);
        if (violations.Count > 0)
        {
            var violationMessages = string.Join("; ", violations.Select(v => $"{v.RuleCode}: {v.Message}"));
            throw new InvalidOperationException($"Failed to generate valid plan: {violationMessages}");
        }

        return new GeneratedWeeklyPlanResult(plan, explanations);
    }

    private static bool IsWeekday(int dayIndex) => dayIndex is >= 0 and <= 4;

    private static bool IsFishDish(Dish dish)
    {
        if (dish.IsPescetarian) return true;
        return dish.Ingredients.Any(di => 
            string.Equals(di.Ingredient.Category, "fish", StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<Guid, double> CalculateDishScores(
        IReadOnlyList<Dish> dishes,
        Dictionary<Guid, DateOnly> lastEaten,
        DateOnly today)
    {
        var scores = new Dictionary<Guid, double>();

        foreach (var dish in dishes)
        {
            var daysSinceEaten = lastEaten.TryGetValue(dish.Id, out var lastDate)
                ? Math.Min(today.DayNumber - lastDate.DayNumber, 999)
                : 999;

            var baseScore = 
                (dish.FamilyRating * 2.0) +
                (dish.KidRating * 2.0) +
                (daysSinceEaten / 3.0);

            scores[dish.Id] = baseScore;
        }

        return scores;
    }

    private static Dish SelectBestDish(
        List<Dish> candidates,
        Dictionary<Guid, double> dishScores,
        int dayIndex,
        bool isWeekday)
    {
        // Apply weekday penalty for active time
        var scoredCandidates = candidates
            .Select(d => new
            {
                Dish = d,
                Score = isWeekday 
                    ? dishScores[d.Id] - d.ActiveMinutes 
                    : dishScores[d.Id]
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Dish.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Dish.Id)
            .ToList();

        return scoredCandidates.First().Dish;
    }

    private static PlannedDishExplanation CreateExplanation(
        Dish dish,
        Dictionary<Guid, DateOnly> lastEaten,
        DateOnly today,
        bool isFish)
    {
        var reasons = new List<string>();

        // Priority 1: Fish requirement
        if (isFish)
        {
            reasons.Add("Fish requirement");
        }

        // Priority 2: Not eaten recently (>30 days)
        if (lastEaten.TryGetValue(dish.Id, out var lastDate))
        {
            var daysSince = today.DayNumber - lastDate.DayNumber;
            if (daysSince > 30)
            {
                reasons.Add($"Not eaten recently ({daysSince} days)");
            }
        }
        else
        {
            reasons.Add("Never eaten before");
        }

        // Priority 3: High kid rating (≥4)
        if (dish.KidRating >= 4)
        {
            reasons.Add($"Good kid rating ({dish.KidRating})");
        }

        // Priority 4: Fits time limit
        if (reasons.Count < 3)
        {
            reasons.Add($"Fits time limit ({dish.TotalMinutes} min)");
        }

        // Max 3 reasons
        return new PlannedDishExplanation(dish.Id, reasons.Take(3).ToList());
    }
}
