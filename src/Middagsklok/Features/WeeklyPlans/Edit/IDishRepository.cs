using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Edit;

public interface IDishRepository
{
    Task<Dish?> GetById(Guid dishId, CancellationToken ct = default);
    Task<IReadOnlyList<Dish>> GetByIds(IReadOnlyList<Guid> dishIds, CancellationToken ct = default);
}
