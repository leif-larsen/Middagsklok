You are working in a .NET 10 solution with a clean architecture.

Authoritative schema doc:
- docs/data-model.md is the source of truth for entities, fields, and relationships.

Project structure:
- Middagsklok (main library)
  - Domain (pure domain models)
  - Features (use-cases)
  - Database (EF Core + SQLite, EF entities + mapping)
- Middagsklok.App (temporary entry point)
- Middagsklok.Tests.Integration

GOAL FEATURE:
"DishHistory: record when dishes were eaten and expose last-eaten information."

SCOPE:
Implement persistence + feature logic for:
- DishHistory (append-only)
And expose:
- LogDishEaten (command)
- GetDishHistory (query)
- GetLastEatenPerDish (query)

Out of scope:
- Auto weekly planning
- Offers/discounts
- Pantry/inventory
- Editing/deleting history (except data correction not implemented now)

ARCHITECTURE RULES (non-negotiable):
- Domain models are pure, use Guid for Ids, no EF references.
- Database has EF entities + DbContext + repository implementations + mapping.
- Features define repository interfaces + handlers, no EF types.
- Console calls Features, not DbContext.

DOMAIN MODELS (Middagsklok.Domain):
Add:
- DishHistoryEntry
  - Guid Id
  - Guid DishId
  - DateOnly Date
  - int? RatingOverride (nullable)
  - string? Notes

Notes:
- DishHistoryEntry references Dish by DishId to keep the model small.
- DishHistory is append-only (no update/delete methods in v1).

FEATURES (Middagsklok.Features):
1) Repository interface (added to the corresponding file in database/repositories):
- IDishHistoryRepository
  - Task Add(DishHistoryEntry entry, CancellationToken ct)
  - Task<IReadOnlyList<DishHistoryEntry>> GetForDish(Guid dishId, CancellationToken ct)
  - Task<IReadOnlyList<DishHistoryEntry>> GetBetween(DateOnly from, DateOnly to, CancellationToken ct)  (optional)
  - Task<Dictionary<Guid, DateOnly>> GetLastEatenByDish(CancellationToken ct)

2) Use-cases/features:
A) LogDishEatenFeature
- Input: dishId, date (DateOnly), ratingOverride (optional), notes (optional)
- Validation:
  - dishId must exist (use IDishRepository.GetByIdWithIngredients(dishId))
  - date must not be in the future (based on IClock/TimeProvider abstraction)
  - ratingOverride, if present, must be 1..5
- Behavior:
  - Create DishHistoryEntry with new Guid
  - Persist via repository
  - Return the created entry

B) GetDishHistoryFeature
- Input: dishId
- Behavior:
  - Validate dish exists
  - Return entries sorted by Date descending (deterministic)

C) GetLastEatenByDishFeature
- Behavior:
  - Return dictionary dishId -> last eaten date
  - Deterministic and testable

Time handling:
- Introduce an IClock (or TimeProvider wrapper) in Features to validate "not future".
- Do NOT use DateTime.Now directly in handlers.

DATABASE (Middagsklok.Database):
1) EF schema/entities:
- DishHistoryEntity
  - Id (Guid)
  - DishId (Guid) FK to dish
  - Date (store DateOnly as TEXT "YYYY-MM-DD" or INTEGER days; use the same approach as WeeklyPlan WeekStartDate)
  - RatingOverride (nullable int)
  - Notes (nullable text)

Constraints:
- FK to dish table.
- Index on DishId + Date for query performance.
- DishHistory is append-only: no repo methods for update/delete.

2) Repository implementation:
- Implement IDishHistoryRepository using EF Core with AsNoTracking for reads.
- Queries must be deterministic:
  - GetForDish: order by Date descending, then Id descending.
  - GetLastEatenByDish: group by DishId and take MAX(Date) in SQL (not in-memory).
- Map EF entities -> Domain models explicitly. No AutoMapper.

3) Migrations/bootstrap:
- Add migration for dish_history table and apply via existing DbBootstrapper.
- Do not seed dish history by default.

CONSOLE APP (Middagsklok.App):
Add minimal commands (do NOT build a complex CLI):
- "history log" (args: dishId, date optional default=today)
- "history show" (args: dishId)
- "history last" (prints last eaten date for all dishes ordered by dish name)

Console must call handlers only.

INTEGRATION TESTS (required):
Using SQLite in-memory:
1) Logging:
- LogDishEaten inserts a record.
- Future date is rejected (using fake clock/time provider).
2) Query:
- GetForDish returns entries ordered by date desc.
3) Aggregation:
- GetLastEatenByDish returns correct max date per dish and is done via SQL (verify by inspecting generated query if feasible, otherwise accept correctness + performance intent).

IMPORTANT:
- No EF types in Features/Domain.
- No direct DateTime.Now usage in handlers.
- Keep feature minimal and deterministic.