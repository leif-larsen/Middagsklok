using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Create;

public interface IDishRepository
{
    Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct = default);
}
