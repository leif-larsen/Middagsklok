using Microsoft.EntityFrameworkCore.Storage;
using Middagsklok.Database;
using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlans.Generate;

namespace Middagsklok.Features.WeeklyPlans.Save;

/// <summary>
/// Feature that saves a weekly meal plan and creates corresponding dish history entries.
/// This feature validates the plan, persists it, and creates history records in a transaction.
/// </summary>
public class SaveWeeklyPlanFeature
{
    private const string PlannedMealNotes = "Planned meal";
    
    private readonly IDishRepository _dishRepository;
    private readonly IWeeklyPlanRepository _planRepository;
    private readonly IDishHistoryRepository _historyRepository;
    private readonly WeeklyPlanRulesValidator _validator;
    private readonly MiddagsklokDbContext _context;
    private readonly PlanningRules _defaultRules;

    public SaveWeeklyPlanFeature(
        IDishRepository dishRepository,
        IWeeklyPlanRepository planRepository,
        IDishHistoryRepository historyRepository,
        WeeklyPlanRulesValidator validator,
        MiddagsklokDbContext context,
        PlanningRules defaultRules)
    {
        _dishRepository = dishRepository;
        _planRepository = planRepository;
        _historyRepository = historyRepository;
        _validator = validator;
        _context = context;
        _defaultRules = defaultRules;
    }

    public async Task<SavedWeeklyPlanResult> Execute(
        SaveWeeklyPlanRequest request,
        CancellationToken ct = default)
    {
        // Step 1: Load dishes
        var dishIds = request.Items.Select(i => i.DishId).ToList();
        var dishes = await _dishRepository.GetByIds(dishIds, ct);

        // Validate all dishes exist
        var dishDict = dishes.ToDictionary(d => d.Id);
        var missingDishIds = dishIds.Where(id => !dishDict.ContainsKey(id)).ToList();
        if (missingDishIds.Any())
        {
            throw new ArgumentException(
                $"The following dish IDs were not found: {string.Join(", ", missingDishIds)}");
        }

        // Step 2: Build plan
        var items = request.Items
            .Select(i => new WeeklyPlanItem(i.DayIndex, dishDict[i.DishId]))
            .ToList();

        var plan = new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: request.WeekStartDate,
            CreatedAt: DateTimeOffset.UtcNow,
            Items: items);

        // Step 3: Validate plan
        var violations = _validator.Validate(plan, _defaultRules);
        if (violations.Count > 0)
        {
            // Return validation errors without saving
            return new SavedWeeklyPlanResult(
                Plan: plan,
                Status: "validation_failed",
                Violations: violations);
        }

        // Step 4: Save plan and create history entries in a transaction
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Save the weekly plan
            var savedPlan = await _planRepository.CreateOrReplace(plan, ct);

            // Create dish history entries for each day
            var historyEntries = new List<DishHistoryEntry>();
            for (int dayIndex = 0; dayIndex < 7; dayIndex++)
            {
                var item = savedPlan.Items.FirstOrDefault(i => i.DayIndex == dayIndex);
                if (item is not null)
                {
                    var date = savedPlan.WeekStartDate.AddDays(dayIndex);
                    var entry = new DishHistoryEntry(
                        Id: Guid.NewGuid(),
                        DishId: item.Dish.Id,
                        Date: date,
                        RatingOverride: null,
                        Notes: PlannedMealNotes);
                    
                    historyEntries.Add(entry);
                }
            }

            // Batch insert history entries (will ignore duplicates due to unique index)
            await _historyRepository.AddBatch(historyEntries, ct);

            await transaction.CommitAsync(ct);

            return new SavedWeeklyPlanResult(
                Plan: savedPlan,
                Status: "saved",
                Violations: Array.Empty<RuleViolation>());
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
