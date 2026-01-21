using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Save;

/// <summary>
/// Repository interface for saving weekly plans and creating dish history.
/// </summary>
public interface IDishHistoryRepository
{
    /// <summary>
    /// Adds multiple dish history entries in a batch.
    /// If a duplicate (dish_id, date) entry exists, it will be ignored (due to unique index).
    /// </summary>
    Task AddBatch(IEnumerable<DishHistoryEntry> entries, CancellationToken ct = default);
}
