using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database;

public class DbBootstrapper
{
    private readonly MiddagsklokDbContext _context;

    public DbBootstrapper(MiddagsklokDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _context.Database.EnsureCreatedAsync(ct);

        if (!await _context.Dishes.AnyAsync(ct))
        {
            await SeedDataAsync(ct);
        }
    }

    private async Task SeedDataAsync(CancellationToken ct)
    {
        // Deterministic GUIDs for stable tests
        var tomatoId = new Guid("11111111-1111-1111-1111-111111111001");
        var onionId = new Guid("11111111-1111-1111-1111-111111111002");
        var garlicId = new Guid("11111111-1111-1111-1111-111111111003");
        var pastaId = new Guid("11111111-1111-1111-1111-111111111004");
        var salmonId = new Guid("11111111-1111-1111-1111-111111111005");
        var parmesanId = new Guid("11111111-1111-1111-1111-111111111006");

        var tomatSauceId = new Guid("22222222-2222-2222-2222-222222222001");
        var salmonPastaId = new Guid("22222222-2222-2222-2222-222222222002");

        var ingredients = new List<IngredientEntity>
        {
            new() { Id = tomatoId, Name = "Tomat", Category = "Grønnsaker", DefaultUnit = "stk" },
            new() { Id = onionId, Name = "Løk", Category = "Grønnsaker", DefaultUnit = "stk" },
            new() { Id = garlicId, Name = "Hvitløk", Category = "Grønnsaker", DefaultUnit = "fedd" },
            new() { Id = pastaId, Name = "Pasta", Category = "Tørrvarer", DefaultUnit = "g" },
            new() { Id = salmonId, Name = "Laks", Category = "Fisk", DefaultUnit = "g" },
            new() { Id = parmesanId, Name = "Parmesan", Category = "Meieri", DefaultUnit = "g" }
        };

        var dishes = new List<DishEntity>
        {
            new()
            {
                Id = tomatSauceId,
                Name = "Pasta med tomatsaus",
                ActiveMinutes = 20,
                TotalMinutes = 40,
                KidRating = 4,
                FamilyRating = 4,
                IsPescetarian = true,
                HasOptionalMeatVariant = true
            },
            new()
            {
                Id = salmonPastaId,
                Name = "Laksepasta",
                ActiveMinutes = 15,
                TotalMinutes = 25,
                KidRating = 3,
                FamilyRating = 5,
                IsPescetarian = true,
                HasOptionalMeatVariant = false
            }
        };

        var dishIngredients = new List<DishIngredientEntity>
        {
            // Pasta med tomatsaus: tomat, løk, hvitløk, pasta, parmesan (optional)
            new() { DishId = tomatSauceId, IngredientId = tomatoId, Amount = 4, Unit = "stk", Optional = false },
            new() { DishId = tomatSauceId, IngredientId = onionId, Amount = 1, Unit = "stk", Optional = false },
            new() { DishId = tomatSauceId, IngredientId = garlicId, Amount = 2, Unit = "fedd", Optional = false },
            new() { DishId = tomatSauceId, IngredientId = pastaId, Amount = 400, Unit = "g", Optional = false },
            new() { DishId = tomatSauceId, IngredientId = parmesanId, Amount = 50, Unit = "g", Optional = true },
            
            // Laksepasta: laks, pasta, løk (shared), hvitløk (shared), parmesan (shared)
            new() { DishId = salmonPastaId, IngredientId = salmonId, Amount = 400, Unit = "g", Optional = false },
            new() { DishId = salmonPastaId, IngredientId = pastaId, Amount = 400, Unit = "g", Optional = false },
            new() { DishId = salmonPastaId, IngredientId = onionId, Amount = 1, Unit = "stk", Optional = false },
            new() { DishId = salmonPastaId, IngredientId = garlicId, Amount = 1, Unit = "fedd", Optional = true }
        };

        _context.Ingredients.AddRange(ingredients);
        _context.Dishes.AddRange(dishes);
        _context.DishIngredients.AddRange(dishIngredients);

        await _context.SaveChangesAsync(ct);
    }
}
