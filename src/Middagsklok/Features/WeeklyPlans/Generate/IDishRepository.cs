using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Generate;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllWithIngredients(CancellationToken ct = default);
}
