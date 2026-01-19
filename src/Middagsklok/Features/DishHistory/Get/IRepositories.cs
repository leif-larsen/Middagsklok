using Middagsklok.Domain;

namespace Middagsklok.Features.DishHistory.Get;

public interface IDishRepository
{
    Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct = default);
}

public interface IDishHistoryRepository
{
    Task<IReadOnlyList<DishHistoryEntry>> GetForDish(Guid dishId, CancellationToken ct = default);
}
