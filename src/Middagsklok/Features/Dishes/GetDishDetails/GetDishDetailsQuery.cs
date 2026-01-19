using Middagsklok.Domain;

namespace Middagsklok.Features.Dishes.GetDishDetails;

public record GetDishDetailsQuery(Guid DishId);

public interface IDishDetailsRepository
{
    Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct = default);
}
