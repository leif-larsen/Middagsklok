using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;
using Middagsklok.Domain;

namespace Middagsklok.Database.Repositories;

public interface IDishHistoryRepository
{
    Task Add(DishHistoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<DishHistoryEntry>> GetForDish(Guid dishId, CancellationToken ct = default);
    Task<IReadOnlyList<DishHistoryEntry>> GetBetween(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Dictionary<Guid, DateOnly>> GetLastEatenByDish(CancellationToken ct = default);
}

public class DishHistoryRepository : IDishHistoryRepository
{
    private readonly MiddagsklokDbContext _context;

    public DishHistoryRepository(MiddagsklokDbContext context)
    {
        _context = context;
    }

    public async Task Add(DishHistoryEntry entry, CancellationToken ct = default)
    {
        var entity = new DishHistoryEntity
        {
            Id = entry.Id,
            DishId = entry.DishId,
            Date = entry.Date,
            RatingOverride = entry.RatingOverride,
            Notes = entry.Notes
        };

        _context.Set<DishHistoryEntity>().Add(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DishHistoryEntry>> GetForDish(Guid dishId, CancellationToken ct = default)
    {
        var entities = await _context.Set<DishHistoryEntity>()
            .AsNoTracking()
            .Where(e => e.DishId == dishId)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<DishHistoryEntry>> GetBetween(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var entities = await _context.Set<DishHistoryEntity>()
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<Dictionary<Guid, DateOnly>> GetLastEatenByDish(CancellationToken ct = default)
    {
        return await _context.Set<DishHistoryEntity>()
            .AsNoTracking()
            .GroupBy(e => e.DishId)
            .Select(g => new { DishId = g.Key, LastDate = g.Max(e => e.Date) })
            .ToDictionaryAsync(x => x.DishId, x => x.LastDate, ct);
    }

    private static DishHistoryEntry MapToDomain(DishHistoryEntity entity)
    {
        return new DishHistoryEntry(
            Id: entity.Id,
            DishId: entity.DishId,
            Date: entity.Date,
            RatingOverride: entity.RatingOverride,
            Notes: entity.Notes);
    }
}
