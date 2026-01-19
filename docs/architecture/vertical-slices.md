# Vertical Slices Architecture

## Overview

Middagsklok follows a **Vertical Slice Architecture** pattern where each feature is self-contained and encapsulates all the code needed for a specific use case. This approach makes features easier to understand, modify, and test in isolation.

## Core Principles

1. **Feature-first organization**: Code is organized by use case, not by technical layer
2. **Self-contained slices**: Each feature contains everything it needs (request/response models, handlers, validation, interfaces)
3. **Minimal coupling**: Features don't reference each other directly
4. **Single Responsibility**: Each feature slice handles exactly one use case
5. **Interface proximity**: Interfaces are defined close to where they're used (inside the feature)

## Folder Structure

```
src/Middagsklok/
├── Features/
│   ├── Dishes/
│   │   ├── Import/                    # Batch import dishes use case
│   │   │   ├── BatchImportDishesFeature.cs
│   │   │   ├── BatchImportDishesModels.cs
│   │   │   └── IDishImportRepository.cs
│   │   └── List/                      # List all dishes use case
│   │       ├── GetDishesFeature.cs
│   │       └── IDishRepository.cs
│   ├── WeeklyPlans/
│   │   ├── Get/                       # Get existing weekly plan
│   │   │   ├── GetWeeklyPlanFeature.cs
│   │   │   └── IWeeklyPlanRepository.cs
│   │   ├── Create/                    # Create weekly plan manually
│   │   │   ├── CreateWeeklyPlanFeature.cs
│   │   │   ├── IDishRepository.cs
│   │   │   └── IWeeklyPlanRepository.cs
│   │   └── Generate/                  # Generate weekly plan with AI
│   │       ├── GenerateWeeklyPlanFeature.cs
│   │       ├── GenerateWeeklyPlanRequest.cs
│   │       ├── GeneratedWeeklyPlanResult.cs
│   │       ├── WeeklyPlanRulesValidator.cs
│   │       ├── IDishRepository.cs
│   │       ├── IDishHistoryRepository.cs
│   │       └── IWeeklyPlanRepository.cs
│   ├── DishHistory/
│   │   ├── Log/                       # Log when a dish was eaten
│   │   │   ├── LogDishEatenFeature.cs
│   │   │   └── IRepositories.cs
│   │   ├── Get/                       # Get history for a dish
│   │   │   ├── GetDishHistoryFeature.cs
│   │   │   └── IRepositories.cs
│   │   └── GetLastEaten/              # Get last eaten dates
│   │       ├── GetLastEatenByDishFeature.cs
│   │       └── IDishHistoryRepository.cs
│   ├── ShoppingList/
│   │   └── GenerateForWeek/           # Generate shopping list from plan
│   │       ├── CreateShoppingListForWeekFeature.cs
│   │       ├── GetShoppingList.cs
│   │       ├── ShoppingList.cs
│   │       ├── ShoppingListItem.cs
│   │       └── IWeeklyPlanRepository.cs
│   └── Shared/
│       └── IClock.cs                  # Truly cross-cutting concerns only
├── Domain/                            # Core domain models (value objects, entities)
│   ├── Dish.cs
│   ├── WeeklyPlan.cs
│   ├── Ingredient.cs
│   └── ...
└── Database/                          # EF Core infrastructure
    ├── MiddagsklokDbContext.cs
    ├── Entities/
    │   ├── DishEntity.cs
    │   └── ...
    └── Repositories/                  # Concrete implementations
        ├── DishRepository.cs
        ├── WeeklyPlanRepository.cs
        └── ...
```

## Anatomy of a Feature Slice

Each feature slice contains:

### 1. Feature Handler
The main class that orchestrates the use case logic.

```csharp
public class GetDishesFeature
{
    private readonly IDishRepository _dishRepository;

    public GetDishesFeature(IDishRepository dishRepository)
    {
        _dishRepository = dishRepository;
    }

    public Task<IReadOnlyList<Dish>> Execute(CancellationToken ct = default)
    {
        return _dishRepository.GetAllWithIngredients(ct);
    }
}
```

### 2. Request/Response Models
Feature-specific DTOs and models.

```csharp
public record BatchImportDishesCommand(List<AddDishCommand> Dishes);

public record BatchImportResult(
    int Total,
    int Created,
    int Skipped,
    int Failed,
    IReadOnlyList<BatchImportDishResult> Results);
```

### 3. Repository Interfaces
Feature-specific interfaces defining only the data access methods needed for this feature.

```csharp
namespace Middagsklok.Features.Dishes.List;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllWithIngredients(CancellationToken ct = default);
    Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct = default);
}
```

### 4. Validation Logic (if needed)
Feature-specific validation or business rules.

```csharp
public class WeeklyPlanRulesValidator
{
    public IReadOnlyList<RuleViolation> Validate(WeeklyPlan plan, PlanningRules rules)
    {
        // Feature-specific validation logic
    }
}
```

## Repository Implementation Pattern

Repository implementations live in `Database/Repositories/` and implement multiple feature-specific interfaces:

```csharp
using Middagsklok.Features.Dishes.List;
using Middagsklok.Features.WeeklyPlans.Create;
using Middagsklok.Features.WeeklyPlans.Generate;

public class DishRepository :
    Features.Dishes.List.IDishRepository,
    Features.WeeklyPlans.Create.IDishRepository,
    Features.WeeklyPlans.Generate.IDishRepository
{
    // Implementation serves all feature interfaces
}
```

This approach allows:
- Each feature to define only the methods it needs
- Database layer to implement all interfaces with a single class
- Features to remain decoupled from each other

## Dependency Flow

```
API/App Layer
     ↓
Feature Slices (no cross-references)
     ↓
Domain Models (shared)
     ↓
Database (via interfaces defined in features)
```

**Rules:**
- API/App references Features
- Features reference Domain
- Features define repository interfaces
- Database implements feature interfaces
- Features NEVER reference other features
- Features NEVER reference Database directly

## Benefits

1. **Easy to locate code**: Everything for a use case is in one folder
2. **Reduced cognitive load**: Only need to understand one slice at a time
3. **Safe modifications**: Changes to one feature don't affect others
4. **Clear testing boundaries**: Each slice can be tested in isolation
5. **Scalable**: New features don't affect existing ones
6. **Onboarding friendly**: New developers can understand features independently

## Adding a New Feature

1. Create a new folder under the appropriate category (e.g., `Features/Dishes/Update/`)
2. Add the feature handler class (e.g., `UpdateDishFeature.cs`)
3. Define request/response models in the same folder
4. Define repository interfaces needed (only the methods you need)
5. Implement the interfaces in `Database/Repositories/`
6. Register the feature in `Program.cs` dependency injection
7. Wire up API endpoints or console commands

## Example: Adding a "Delete Dish" Feature

```
Features/Dishes/Delete/
├── DeleteDishFeature.cs
├── DeleteDishRequest.cs
└── IDishRepository.cs
```

```csharp
namespace Middagsklok.Features.Dishes.Delete;

public record DeleteDishRequest(Guid DishId);

public interface IDishRepository
{
    Task<bool> DeleteDish(Guid dishId, CancellationToken ct = default);
}

public class DeleteDishFeature
{
    private readonly IDishRepository _dishRepository;

    public DeleteDishFeature(IDishRepository dishRepository)
    {
        _dishRepository = dishRepository;
    }

    public async Task<bool> Execute(DeleteDishRequest request, CancellationToken ct = default)
    {
        return await _dishRepository.DeleteDish(request.DishId, ct);
    }
}
```

Then update `Database/Repositories/DishRepository.cs` to implement the new interface:

```csharp
public class DishRepository :
    Features.Dishes.List.IDishRepository,
    Features.Dishes.Delete.IDishRepository  // Add this
{
    // Implement DeleteDish method
}
```

## Anti-Patterns to Avoid

❌ **Don't create "service" classes that handle multiple use cases**
```csharp
// BAD: God class with multiple responsibilities
public class DishService
{
    public Task<List<Dish>> GetAll() { }
    public Task<Dish> GetById(Guid id) { }
    public Task Create(Dish dish) { }
    public Task Update(Dish dish) { }
    public Task Delete(Guid id) { }
}
```

✅ **Do create focused feature handlers for each use case**
```csharp
// GOOD: One feature, one responsibility
public class GetDishesFeature { }
public class GetDishByIdFeature { }
public class CreateDishFeature { }
```

❌ **Don't reference other feature folders**
```csharp
// BAD: Feature depending on another feature
using Middagsklok.Features.Dishes.Import;

public class WeeklyPlanFeature
{
    // Don't do this!
}
```

✅ **Do use Domain models or Database repositories**
```csharp
// GOOD: Depend on Domain or Database through interfaces
using Middagsklok.Domain;

public class WeeklyPlanFeature
{
    private readonly IDishRepository _dishRepository;
}
```

❌ **Don't put global interfaces in a central location**
```csharp
// BAD: Global Interfaces/ folder
Interfaces/
├── IDishRepository.cs
└── IWeeklyPlanRepository.cs
```

✅ **Do define interfaces in the feature that needs them**
```csharp
// GOOD: Interface lives with the feature
Features/Dishes/List/
├── GetDishesFeature.cs
└── IDishRepository.cs
```

## Migration Notes

This architecture was applied to the existing Middagsklok codebase as a refactoring. All functionality remains the same; only the organization changed.

**Previous structure:** Features were grouped under a single `Features/` folder with some feature-specific subfolders but repository interfaces were centralized in `Database/Repositories/`.

**New structure:** Each feature is now fully self-contained with its own interfaces, maintaining the same behavior but with better organization and maintainability.

---

_Last updated: 2026-01-19_
