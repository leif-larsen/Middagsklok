using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;
using Middagsklok.Domain;

namespace Middagsklok.Database.Repositories;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllWithIngredients(CancellationToken ct = default);
}

public class DishRepository : IDishRepository
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
