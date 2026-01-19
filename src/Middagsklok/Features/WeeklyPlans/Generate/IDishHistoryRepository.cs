using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Generate;

public interface IDishHistoryRepository
{
    Task<Dictionary<Guid, DateOnly>> GetLastEatenByDish(CancellationToken ct = default);
}
