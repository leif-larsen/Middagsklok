using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;
using Middagsklok.Features.Dishes.Import;

namespace Middagsklok.Database.Repositories;

public class DishImportRepository : IDishImportRepository
{
    private readonly MiddagsklokDbContext _context;

    public DishImportRepository(MiddagsklokDbContext context)
    {
        _context = context;
    }

    public async Task<Guid?> FindDishIdByName(string name, CancellationToken ct = default)
    {
        var normalized = name.ToLowerInvariant().Trim();
        var entity = await _context.Dishes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Name.ToLower() == normalized, ct);
        
        return entity?.Id;
    }

    public async Task<Guid> InsertDish(AddDishCommand cmd, CancellationToken ct = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        
        try
        {
            var dishId = Guid.NewGuid();
            
            // Create dish entity
            var dishEntity = new DishEntity
            {
                Id = dishId,
                Name = cmd.Name.Trim(),
                ActiveMinutes = cmd.ActiveMinutes,
                TotalMinutes = cmd.TotalMinutes,
                KidRating = cmd.KidRating,
                FamilyRating = cmd.FamilyRating,
                IsPescetarian = cmd.IsPescetarian,
                HasOptionalMeatVariant = cmd.HasOptionalMeatVariant
            };

            _context.Dishes.Add(dishEntity);
            
            // Aggregate duplicate ingredients within this dish
            var aggregatedIngredients = cmd.Ingredients
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
                    DishId = dishId,
                    IngredientId = ingredientId,
                    Amount = ingredientItem.Amount,
                    Unit = ingredientItem.Unit,
                    Optional = ingredientItem.Optional
                };

                _context.DishIngredients.Add(dishIngredient);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            
            return dishId;
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
