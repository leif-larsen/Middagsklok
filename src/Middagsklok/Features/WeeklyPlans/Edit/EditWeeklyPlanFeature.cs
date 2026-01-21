using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlans.Generate;

namespace Middagsklok.Features.WeeklyPlans.Edit;

public class EditWeeklyPlanFeature
{
    private readonly IDishRepository _dishRepository;
    private readonly IWeeklyPlanRepository _planRepository;
    private readonly WeeklyPlanRulesValidator _validator;
    private readonly PlanningRules _rules;

    public EditWeeklyPlanFeature(
        IDishRepository dishRepository,
        IWeeklyPlanRepository planRepository,
        WeeklyPlanRulesValidator validator,
        PlanningRules rules)
    {
        _dishRepository = dishRepository;
        _planRepository = planRepository;
        _validator = validator;
        _rules = rules;
    }

    public async Task<(WeeklyPlan Plan, IReadOnlyList<RuleViolation> Violations)> Execute(
        EditWeeklyPlanRequest request,
        CancellationToken ct = default)
    {
        // Validation: Exactly 7 items
        if (request.Items.Count != 7)
        {
            throw new ArgumentException($"Weekly plan must have exactly 7 items, but received {request.Items.Count}");
        }

        // Validation: Unique dayIndex 0-6
        var dayIndices = request.Items.Select(i => i.DayIndex).ToList();
        var expectedDays = Enumerable.Range(0, 7).ToHashSet();
        var actualDays = dayIndices.ToHashSet();

        if (!expectedDays.SetEquals(actualDays))
        {
            var missing = expectedDays.Except(actualDays).OrderBy(d => d).ToList();
            var duplicates = dayIndices.GroupBy(d => d).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            
            if (missing.Any())
            {
                throw new ArgumentException($"Weekly plan is missing days: {string.Join(", ", missing)}");
            }
            
            if (duplicates.Any())
            {
                throw new ArgumentException($"Weekly plan has duplicate day indices: {string.Join(", ", duplicates)}");
            }
        }

        // Validation: No duplicate dishes
        var dishIds = request.Items.Select(i => i.DishId).ToList();
        var duplicateDishes = dishIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        
        if (duplicateDishes.Any())
        {
            throw new ArgumentException($"Weekly plan contains duplicate dishes: {string.Join(", ", duplicateDishes)}");
        }

        // Load dishes from repository
        var dishes = await _dishRepository.GetByIds(dishIds, ct);
        
        if (dishes.Count != dishIds.Count)
        {
            var foundIds = dishes.Select(d => d.Id).ToHashSet();
            var missingIds = dishIds.Where(id => !foundIds.Contains(id)).ToList();
            throw new ArgumentException($"Dishes not found: {string.Join(", ", missingIds)}");
        }

        // Create dish lookup
        var dishLookup = dishes.ToDictionary(d => d.Id);

        // Build WeeklyPlan domain object
        var items = request.Items
            .OrderBy(i => i.DayIndex)
            .Select(i => new WeeklyPlanItem(i.DayIndex, dishLookup[i.DishId]))
            .ToList();

        var plan = new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: request.WeekStartDate,
            CreatedAt: DateTimeOffset.UtcNow,
            Items: items);

        // Validate with WeeklyPlanRulesValidator
        var violations = _validator.Validate(plan, _rules);

        // Save plan if no violations
        if (violations.Count == 0)
        {
            var savedPlan = await _planRepository.CreateOrReplace(plan, ct);
            return (savedPlan, violations);
        }

        // Return plan with violations (don't save)
        return (plan, violations);
    }
}
