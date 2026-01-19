using Middagsklok.Domain;

namespace Middagsklok.Features.DishHistory.Log;

public interface IDishRepository
{
    Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct = default);
}

public interface IDishHistoryRepository
{
    Task Add(DishHistoryEntry entry, CancellationToken ct = default);
}
