using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Save;

public interface IDishRepository
{
    Task<Dish?> GetById(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Dish>> GetByIds(IEnumerable<Guid> ids, CancellationToken ct = default);
}
