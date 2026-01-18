using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlanning;

public class WeeklyPlanRulesValidator
{
    public IReadOnlyList<RuleViolation> Validate(WeeklyPlan plan, PlanningRules rules)
    {
        var violations = new List<RuleViolation>();

        violations.AddRange(ValidateDayIndices(plan));
        violations.AddRange(ValidateNoDuplicateDishes(plan));
        violations.AddRange(ValidateTimeLimits(plan, rules));
        violations.AddRange(ValidateFishMinimum(plan, rules));

        return violations;
    }

    private IEnumerable<RuleViolation> ValidateDayIndices(WeeklyPlan plan)
    {
        var dayIndices = plan.Items.Select(i => i.DayIndex).ToList();

        // Check if we have exactly 7 items
        if (dayIndices.Count != 7)
        {
            yield return new RuleViolation(
                "R1_INCOMPLETE_WEEK",
                $"Plan must have exactly 7 items, but has {dayIndices.Count}",
                Array.Empty<int>(),
                Array.Empty<Guid>());
            yield break; // Don't check further if count is wrong
        }

        // Check for missing days
        var expectedDays = Enumerable.Range(0, 7).ToHashSet();
        var actualDays = dayIndices.ToHashSet();
        var missingDays = expectedDays.Except(actualDays).OrderBy(d => d).ToList();

        if (missingDays.Any())
        {
            yield return new RuleViolation(
                "R1_MISSING_DAYS",
                $"Plan is missing days: {string.Join(", ", missingDays)}",
                missingDays,
                Array.Empty<Guid>());
        }

        // Check for duplicate days
        var duplicateDays = dayIndices
            .GroupBy(d => d)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(d => d)
            .ToList();

        if (duplicateDays.Any())
        {
            yield return new RuleViolation(
                "R1_DUPLICATE_DAYS",
                $"Plan has duplicate day indices: {string.Join(", ", duplicateDays)}",
                duplicateDays,
                Array.Empty<Guid>());
        }
    }

    private IEnumerable<RuleViolation> ValidateNoDuplicateDishes(WeeklyPlan plan)
    {
        var duplicateDishes = plan.Items
            .GroupBy(i => i.Dish.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateDishes.Any())
        {
            var dishNames = plan.Items
                .Where(i => duplicateDishes.Contains(i.Dish.Id))
                .Select(i => i.Dish.Name)
                .Distinct()
                .ToList();

            yield return new RuleViolation(
                "R2_DUPLICATE_DISHES",
                $"Plan contains duplicate dishes: {string.Join(", ", dishNames)}",
                Array.Empty<int>(),
                duplicateDishes);
        }
    }

    private IEnumerable<RuleViolation> ValidateTimeLimits(WeeklyPlan plan, PlanningRules rules)
    {
        var weekdayViolations = new List<int>();
        var weekendViolations = new List<int>();
        var violatingDishIds = new List<Guid>();

        foreach (var item in plan.Items)
        {
            var isWeekday = item.DayIndex >= 0 && item.DayIndex <= 4;
            var maxMinutes = isWeekday ? rules.WeekdayMaxTotalMinutes : rules.WeekendMaxTotalMinutes;

            if (item.Dish.TotalMinutes > maxMinutes)
            {
                if (isWeekday)
                    weekdayViolations.Add(item.DayIndex);
                else
                    weekendViolations.Add(item.DayIndex);

                violatingDishIds.Add(item.Dish.Id);
            }
        }

        if (weekdayViolations.Any())
        {
            yield return new RuleViolation(
                "R3_WEEKDAY_TIME_LIMIT",
                $"Weekday dishes exceed {rules.WeekdayMaxTotalMinutes} minutes on days: {string.Join(", ", weekdayViolations.OrderBy(d => d))}",
                weekdayViolations.OrderBy(d => d).ToList(),
                violatingDishIds.Distinct().ToList());
        }

        if (weekendViolations.Any())
        {
            yield return new RuleViolation(
                "R3_WEEKEND_TIME_LIMIT",
                $"Weekend dishes exceed {rules.WeekendMaxTotalMinutes} minutes on days: {string.Join(", ", weekendViolations.OrderBy(d => d))}",
                weekendViolations.OrderBy(d => d).ToList(),
                violatingDishIds.Distinct().ToList());
        }
    }

    private IEnumerable<RuleViolation> ValidateFishMinimum(WeeklyPlan plan, PlanningRules rules)
    {
        var fishDishCount = plan.Items.Count(item => IsFishDish(item.Dish));

        if (fishDishCount < rules.MinFishDinnersPerWeek)
        {
            yield return new RuleViolation(
                "R4_INSUFFICIENT_FISH",
                $"Plan must have at least {rules.MinFishDinnersPerWeek} fish dinners, but has {fishDishCount}",
                Array.Empty<int>(),
                Array.Empty<Guid>());
        }
    }

    private bool IsFishDish(Dish dish)
    {
        // Check if dish is marked as pescetarian
        if (dish.IsPescetarian)
            return true;

        // Check if any ingredient is in the fish category
        return dish.Ingredients.Any(di => 
            string.Equals(di.Ingredient.Category, "fish", StringComparison.OrdinalIgnoreCase));
    }
}
