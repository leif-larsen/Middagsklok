using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;
using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlans.Get;
using Middagsklok.Features.WeeklyPlans.Create;
using Middagsklok.Features.WeeklyPlans.Generate;
using Middagsklok.Features.ShoppingList.GenerateForWeek;

namespace Middagsklok.Database.Repositories;

public class WeeklyPlanRepository :
    Features.WeeklyPlans.Get.IWeeklyPlanRepository,
    Features.WeeklyPlans.Create.IWeeklyPlanRepository,
    Features.WeeklyPlans.Generate.IWeeklyPlanRepository,
    Features.ShoppingList.GenerateForWeek.IWeeklyPlanRepository
{
    private readonly MiddagsklokDbContext _context;

    public WeeklyPlanRepository(MiddagsklokDbContext context)
    {
        _context = context;
    }

    public async Task<WeeklyPlan?> GetByWeekStartDate(DateOnly weekStart, CancellationToken ct = default)
    {
        var entity = await _context.WeeklyPlans
            .AsNoTracking()
            .Include(p => p.Items.OrderBy(i => i.DayIndex))
                .ThenInclude(i => i.Dish)
                    .ThenInclude(d => d.DishIngredients)
                        .ThenInclude(di => di.Ingredient)
            .FirstOrDefaultAsync(p => p.WeekStartDate == weekStart, ct);

        if (entity is null)
            return null;

        return MapToDomain(entity);
    }

    public async Task<WeeklyPlan> CreateOrReplace(WeeklyPlan plan, CancellationToken ct = default)
    {
        var existingPlan = await _context.WeeklyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.WeekStartDate == plan.WeekStartDate, ct);

        if (existingPlan is not null)
        {
            _context.WeeklyPlanItems.RemoveRange(existingPlan.Items);
            _context.WeeklyPlans.Remove(existingPlan);
        }

        var entity = new WeeklyPlanEntity
        {
            Id = plan.Id,
            WeekStartDate = plan.WeekStartDate,
            CreatedAt = plan.CreatedAt,
            Items = plan.Items.Select(i => new WeeklyPlanItemEntity
            {
                PlanId = plan.Id,
                DayIndex = i.DayIndex,
                DishId = i.Dish.Id
            }).ToList()
        };

        _context.WeeklyPlans.Add(entity);
        await _context.SaveChangesAsync(ct);

        // Reload with full dish data
        return (await GetByWeekStartDate(plan.WeekStartDate, ct))!;
    }

    private static WeeklyPlan MapToDomain(WeeklyPlanEntity entity)
    {
        var items = entity.Items
            .OrderBy(i => i.DayIndex)
            .Select(MapItemToDomain)
            .ToList();

        return new WeeklyPlan(
            Id: entity.Id,
            WeekStartDate: entity.WeekStartDate,
            CreatedAt: entity.CreatedAt,
            Items: items);
    }

    private static WeeklyPlanItem MapItemToDomain(WeeklyPlanItemEntity entity)
    {
        var dishIngredients = entity.Dish.DishIngredients
            .OrderBy(di => di.Ingredient.Name)
            .Select(di => new DishIngredient(
                Ingredient: new Ingredient(
                    Id: di.Ingredient.Id,
                    Name: di.Ingredient.Name,
                    Category: di.Ingredient.Category,
                    DefaultUnit: di.Ingredient.DefaultUnit),
                Amount: di.Amount,
                Unit: di.Unit,
                Optional: di.Optional))
            .ToList();

        var dish = new Dish(
            Id: entity.Dish.Id,
            Name: entity.Dish.Name,
            ActiveMinutes: entity.Dish.ActiveMinutes,
            TotalMinutes: entity.Dish.TotalMinutes,
            KidRating: entity.Dish.KidRating,
            FamilyRating: entity.Dish.FamilyRating,
            IsPescetarian: entity.Dish.IsPescetarian,
            HasOptionalMeatVariant: entity.Dish.HasOptionalMeatVariant,
            Ingredients: dishIngredients);

        return new WeeklyPlanItem(
            DayIndex: entity.DayIndex,
            Dish: dish);
    }
}
