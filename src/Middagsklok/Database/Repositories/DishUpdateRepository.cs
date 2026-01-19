using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;
using Middagsklok.Features.Dishes.UpdateDish;

namespace Middagsklok.Database.Repositories;

public class DishUpdateRepository : IDishUpdateRepository
{
    private readonly MiddagsklokDbContext _context;

    public DishUpdateRepository(MiddagsklokDbContext context)
    {
        _context = context;
    }

    public async Task<bool> DishExists(Guid dishId, CancellationToken ct = default)
    {
        return await _context.Dishes.AnyAsync(d => d.Id == dishId, ct);
    }

    public async Task UpdateDish(UpdateDishCommand command, CancellationToken ct = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // Update dish entity
            var dishEntity = await _context.Dishes
                .FirstOrDefaultAsync(d => d.Id == command.DishId, ct);

            if (dishEntity is null)
            {
                throw new InvalidOperationException($"Dish {command.DishId} not found");
            }

            dishEntity.Name = command.Name.Trim();
            dishEntity.ActiveMinutes = command.ActiveMinutes;
            dishEntity.TotalMinutes = command.TotalMinutes;
            dishEntity.KidRating = command.KidRating;
            dishEntity.FamilyRating = command.FamilyRating;
            dishEntity.IsPescetarian = command.IsPescetarian;
            dishEntity.HasOptionalMeatVariant = command.HasOptionalMeatVariant;

            // Delete existing dish_ingredient entries
            var existingIngredients = await _context.DishIngredients
                .Where(di => di.DishId == command.DishId)
                .ToListAsync(ct);

            _context.DishIngredients.RemoveRange(existingIngredients);

            // Aggregate duplicate ingredients within this dish
            var aggregatedIngredients = command.Ingredients
                .GroupBy(i => new { 
                    Name = i.Name.ToLowerInvariant().Trim(), 
                    Unit = i.Unit.ToLowerInvariant().Trim(), 
                    i.Optional 
                })
                .Select(g => new {
                    Name = g.First().Name,
                    Category = g.First().Category,
                    Amount = g.Sum(x => x.Amount),
                    Unit = g.First().Unit,
                    Optional = g.Key.Optional
                })
                .ToList();

            // Upsert ingredients and create dish_ingredient joins
            foreach (var ingredientItem in aggregatedIngredients)
            {
                var ingredientId = await UpsertIngredient(
                    ingredientItem.Name,
                    ingredientItem.Category,
                    ingredientItem.Unit,
                    ct);

                var dishIngredient = new DishIngredientEntity
                {
                    DishId = command.DishId,
                    IngredientId = ingredientId,
                    Amount = ingredientItem.Amount,
                    Unit = ingredientItem.Unit,
                    Optional = ingredientItem.Optional
                };

                _context.DishIngredients.Add(dishIngredient);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Guid> UpsertIngredient(
        string name,
        string category,
        string defaultUnit,
        CancellationToken ct)
    {
        var normalized = name.ToLowerInvariant().Trim();
        
        var existing = await _context.Ingredients
            .FirstOrDefaultAsync(i => i.Name.ToLower() == normalized, ct);

        if (existing is not null)
            return existing.Id;

        var ingredient = new IngredientEntity
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Category = category,
            DefaultUnit = defaultUnit
        };

        _context.Ingredients.Add(ingredient);
        return ingredient.Id;
    }
}
