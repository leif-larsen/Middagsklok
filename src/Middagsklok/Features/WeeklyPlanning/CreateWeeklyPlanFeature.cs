using Middagsklok.Database.Repositories;
using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlanning;

public record CreateWeeklyPlanRequest(DateOnly WeekStartDate, IReadOnlyList<Guid> DishIds);

public class CreateWeeklyPlanFeature
{
    private readonly IDishRepository _dishRepository;
    private readonly IWeeklyPlanRepository _weeklyPlanRepository;

    public CreateWeeklyPlanFeature(IDishRepository dishRepository, IWeeklyPlanRepository weeklyPlanRepository)
    {
        _dishRepository = dishRepository;
        _weeklyPlanRepository = weeklyPlanRepository;
    }

    public async Task<WeeklyPlan> Execute(CreateWeeklyPlanRequest request, CancellationToken ct = default)
    {
        if (request.DishIds.Count != 7)
            throw new ArgumentException("A weekly plan must have exactly 7 dishes (one per day).", nameof(request));

        var items = new List<WeeklyPlanItem>();

        for (var dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            var dishId = request.DishIds[dayIndex];
            var dish = await _dishRepository.GetByIdWithIngredients(dishId, ct)
                ?? throw new ArgumentException($"Dish with id {dishId} not found.", nameof(request));

            items.Add(new WeeklyPlanItem(dayIndex, dish));
        }

        var plan = new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: request.WeekStartDate,
            CreatedAt: DateTimeOffset.UtcNow,
            Items: items);

        return await _weeklyPlanRepository.CreateOrReplace(plan, ct);
    }
}
