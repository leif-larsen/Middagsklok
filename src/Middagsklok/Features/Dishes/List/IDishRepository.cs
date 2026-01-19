using Middagsklok.Domain;

namespace Middagsklok.Features.Dishes.List;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllWithIngredients(CancellationToken ct = default);
    Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct = default);
}
