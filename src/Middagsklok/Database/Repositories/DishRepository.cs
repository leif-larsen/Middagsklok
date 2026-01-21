using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;
using Middagsklok.Domain;
using Middagsklok.Features.Dishes.List;
using Middagsklok.Features.Dishes.GetDishDetails;
using Middagsklok.Features.DishHistory.Log;
using Middagsklok.Features.DishHistory.Get;
using Middagsklok.Features.WeeklyPlans.Create;
using Middagsklok.Features.WeeklyPlans.Generate;
using Middagsklok.Features.WeeklyPlans.Edit;

namespace Middagsklok.Database.Repositories;

public class DishRepository :
    Features.Dishes.List.IDishRepository,
    Features.Dishes.GetDishDetails.IDishDetailsRepository,
    Features.DishHistory.Log.IDishRepository,
    Features.DishHistory.Get.IDishRepository,
    Features.WeeklyPlans.Create.IDishRepository,
    Features.WeeklyPlans.Generate.IDishRepository,
    Features.WeeklyPlans.Edit.IDishRepository,
    Features.WeeklyPlans.Save.IDishRepository
{
    private readonly MiddagsklokDbContext _context;

    public DishRepository(MiddagsklokDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Dish>> GetAllWithIngredients(CancellationToken ct = default)
    {
        var entities = await _context.Dishes
            .AsNoTracking()
            .Include(d => d.DishIngredients)
                .ThenInclude(di => di.Ingredient)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        return entities
            .Select(MapToDomain)
            .ToList();
    }

    public async Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct = default)
    {
        var entity = await _context.Dishes
            .AsNoTracking()
            .Include(d => d.DishIngredients)
                .ThenInclude(di => di.Ingredient)
            .FirstOrDefaultAsync(d => d.Id == dishId, ct);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Dish?> GetById(Guid dishId, CancellationToken ct = default)
    {
        return await GetByIdWithIngredients(dishId, ct);
    }

    public async Task<IReadOnlyList<Dish>> GetByIds(IReadOnlyList<Guid> dishIds, CancellationToken ct = default)
    {
        var entities = await _context.Dishes
            .AsNoTracking()
            .Include(d => d.DishIngredients)
                .ThenInclude(di => di.Ingredient)
            .Where(d => dishIds.Contains(d.Id))
            .ToListAsync(ct);

        return entities
            .Select(MapToDomain)
            .ToList();
    }

    // Overload for IEnumerable (used by Save feature)
    public async Task<IReadOnlyList<Dish>> GetByIds(IEnumerable<Guid> dishIds, CancellationToken ct = default)
    {
        var dishIdList = dishIds.ToList();
        var entities = await _context.Dishes
            .AsNoTracking()
            .Include(d => d.DishIngredients)
                .ThenInclude(di => di.Ingredient)
            .Where(d => dishIdList.Contains(d.Id))
            .ToListAsync(ct);

        return entities
            .Select(MapToDomain)
            .ToList();
    }

    private static Dish MapToDomain(DishEntity entity)
    {
        var ingredients = entity.DishIngredients
            .OrderBy(di => di.Ingredient.Name)
            .Select(MapToDomain)
            .ToList();

        return new Dish(
            Id: entity.Id,
            Name: entity.Name,
            ActiveMinutes: entity.ActiveMinutes,
            TotalMinutes: entity.TotalMinutes,
            KidRating: entity.KidRating,
            FamilyRating: entity.FamilyRating,
            IsPescetarian: entity.IsPescetarian,
            HasOptionalMeatVariant: entity.HasOptionalMeatVariant,
            Ingredients: ingredients);
    }

    private static DishIngredient MapToDomain(DishIngredientEntity entity)
    {
        var ingredient = new Ingredient(
            Id: entity.Ingredient.Id,
            Name: entity.Ingredient.Name,
            Category: entity.Ingredient.Category,
            DefaultUnit: entity.Ingredient.DefaultUnit);

        return new DishIngredient(
            Ingredient: ingredient,
            Amount: entity.Amount,
            Unit: entity.Unit,
            Optional: entity.Optional);
    }
}
