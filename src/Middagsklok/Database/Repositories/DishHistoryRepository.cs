using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Entities;
using Middagsklok.Domain;
using Middagsklok.Features.DishHistory.Log;
using Middagsklok.Features.DishHistory.Get;
using Middagsklok.Features.DishHistory.GetLastEaten;

namespace Middagsklok.Database.Repositories;

public class DishHistoryRepository :
    Features.DishHistory.Log.IDishHistoryRepository,
    Features.DishHistory.Get.IDishHistoryRepository,
    Features.DishHistory.GetLastEaten.IDishHistoryRepository,
    Features.WeeklyPlans.Generate.IDishHistoryRepository,
    Features.WeeklyPlans.Save.IDishHistoryRepository
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

    public async Task AddBatch(IEnumerable<DishHistoryEntry> entries, CancellationToken ct = default)
    {
        var entities = entries.Select(entry => new DishHistoryEntity
        {
            Id = entry.Id,
            DishId = entry.DishId,
            Date = entry.Date,
            RatingOverride = entry.RatingOverride,
            Notes = entry.Notes
        }).ToList();

        // Use ExecuteUpdate to handle conflicts gracefully
        // If a duplicate (dish_id, date) exists due to unique index, it will be ignored
        foreach (var entity in entities)
        {
            _context.Set<DishHistoryEntity>().Add(entity);
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
        {
            // Ignore duplicate entries - this is expected when re-saving a plan
            // Clear the tracked entities to avoid issues
            foreach (var entity in entities)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }
        }
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
