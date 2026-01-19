using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Entities;
using Middagsklok.Database.Repositories;
using Middagsklok.Domain;
using Middagsklok.Features.Shared;
using Middagsklok.Features.DishHistory.Log;
using Middagsklok.Features.DishHistory.Get;
using Middagsklok.Features.DishHistory.GetLastEaten;

namespace Middagsklok.Tests.Integration;

public class DishHistoryTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly DishHistoryRepository _historyRepository;
    private readonly DishRepository _dishRepository;
    private readonly Guid _testDishId;

    public DishHistoryTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _historyRepository = new DishHistoryRepository(_context);
        _dishRepository = new DishRepository(_context);

        // Seed test data
        _testDishId = Guid.NewGuid();
        SeedTestDish(_testDishId, "Test Dish");
    }

    private void SeedTestDish(Guid dishId, string name)
    {
        var dish = new DishEntity
        {
            Id = dishId,
            Name = name,
            ActiveMinutes = 30,
            TotalMinutes = 45,
            KidRating = 4,
            FamilyRating = 5,
            IsPescetarian = true,
            HasOptionalMeatVariant = false
        };

        _context.Dishes.Add(dish);
        _context.SaveChanges();
    }

    [Fact]
    public async Task LogDishEaten_InsertsRecord()
    {
        // Arrange
        var clock = new FakeClock(new DateOnly(2026, 1, 10));
        var feature = new LogDishEatenFeature(_dishRepository, _historyRepository, clock);
        var request = new LogDishEatenRequest(_testDishId, new DateOnly(2026, 1, 10));

        // Act
        var result = await feature.Execute(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testDishId, result.DishId);
        Assert.Equal(new DateOnly(2026, 1, 10), result.Date);
    }

    [Fact]
    public async Task LogDishEaten_FutureDate_ThrowsArgumentException()
    {
        // Arrange
        var clock = new FakeClock(new DateOnly(2026, 1, 10));
        var feature = new LogDishEatenFeature(_dishRepository, _historyRepository, clock);
        var request = new LogDishEatenRequest(_testDishId, new DateOnly(2026, 1, 15));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => feature.Execute(request));
    }

    [Fact]
    public async Task LogDishEaten_InvalidRating_ThrowsArgumentException()
    {
        // Arrange
        var clock = new FakeClock(new DateOnly(2026, 1, 10));
        var feature = new LogDishEatenFeature(_dishRepository, _historyRepository, clock);
        var request = new LogDishEatenRequest(_testDishId, new DateOnly(2026, 1, 10), RatingOverride: 6);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => feature.Execute(request));
    }

    [Fact]
    public async Task LogDishEaten_NonExistentDish_ThrowsArgumentException()
    {
        // Arrange
        var clock = new FakeClock(new DateOnly(2026, 1, 10));
        var feature = new LogDishEatenFeature(_dishRepository, _historyRepository, clock);
        var request = new LogDishEatenRequest(Guid.NewGuid(), new DateOnly(2026, 1, 10));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => feature.Execute(request));
    }

    [Fact]
    public async Task GetForDish_ReturnsEntriesOrderedByDateDescending()
    {
        // Arrange
        var entry1 = new DishHistoryEntry(Guid.NewGuid(), _testDishId, new DateOnly(2026, 1, 5), null, null);
        var entry2 = new DishHistoryEntry(Guid.NewGuid(), _testDishId, new DateOnly(2026, 1, 10), null, null);
        var entry3 = new DishHistoryEntry(Guid.NewGuid(), _testDishId, new DateOnly(2026, 1, 8), null, null);

        await _historyRepository.Add(entry1);
        await _historyRepository.Add(entry2);
        await _historyRepository.Add(entry3);

        var feature = new GetDishHistoryFeature(_dishRepository, _historyRepository);

        // Act
        var result = await feature.Execute(_testDishId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 10), result[0].Date);
        Assert.Equal(new DateOnly(2026, 1, 8), result[1].Date);
        Assert.Equal(new DateOnly(2026, 1, 5), result[2].Date);
    }

    [Fact]
    public async Task GetLastEatenByDish_ReturnsMaxDatePerDish()
    {
        // Arrange
        var dish2Id = Guid.NewGuid();
        SeedTestDish(dish2Id, "Test Dish 2");

        await _historyRepository.Add(new DishHistoryEntry(Guid.NewGuid(), _testDishId, new DateOnly(2026, 1, 5), null, null));
        await _historyRepository.Add(new DishHistoryEntry(Guid.NewGuid(), _testDishId, new DateOnly(2026, 1, 10), null, null));
        await _historyRepository.Add(new DishHistoryEntry(Guid.NewGuid(), dish2Id, new DateOnly(2026, 1, 12), null, null));

        var feature = new GetLastEatenByDishFeature(_historyRepository);

        // Act
        var result = await feature.Execute();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 10), result[_testDishId]);
        Assert.Equal(new DateOnly(2026, 1, 12), result[dish2Id]);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private class FakeClock : IClock
    {
        public FakeClock(DateOnly today)
        {
            Today = today;
        }

        public DateOnly Today { get; }
    }
}
