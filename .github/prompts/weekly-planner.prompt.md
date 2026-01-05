You are working in a .NET 10 solution with a clean architecture.

Authoritative schema doc:
- docs/data-model.md is the source of truth for entities, fields, and relationships.
Do not invent fields outside the minimal needs of this feature.

Project structure:
- Middagsklok (main library)
  - Domain (pure domain models)
  - Features (use-cases)
  - Database (EF Core + SQLite, EF entities + mapping)
- Middagsklok.App (temporary entry point)
- Middagsklok.Tests.Integration

GOAL FEATURE:
"Persist a weekly plan (7 dinners) and generate a consolidated shopping list from that plan."

SCOPE:
Implement persistence + read for:
- WeeklyPlan
- WeeklyPlanItem
And a feature that:
- loads a WeeklyPlan including Dish + Ingredients
- generates an aggregated shopping list (same rules as previous shopping list feature)

Out of scope:
- Auto-planning algorithm
- DishHistory / last eaten logic
- Offers/discount logic
- Pantry/inventory

ARCHITECTURE RULES (non-negotiable):
- Domain models are pure, use Guid for Ids, no EF references.
- Database has EF entities + DbContext + repository implementations + mapping.
- Features define repository interfaces + handlers, no EF types.
- Console calls Feature, not DbContext.

DOMAIN MODELS (Middagsklok.Domain):
Add/ensure:
- WeeklyPlan
  - Guid Id
  - DateOnly WeekStartDate
  - DateTimeOffset CreatedAt
  - IReadOnlyList<WeeklyPlanItem> Items
- WeeklyPlanItem
  - int DayIndex (0–6, where 0 = Monday)
  - Dish Dish

Shopping list models:
- ShoppingList
- ShoppingListItem (IngredientName, Category, Amount, Unit)

(Ingredient, Dish, DishIngredient already exist from previous features.)

FEATURES (Middagsklok.Features):
1) Repository interfaces (should be injected into handlers, but created with the corresponding Database implementations):
- IWeeklyPlanRepository
  - Task<WeeklyPlan?> GetByWeekStartDate(DateOnly weekStart, CancellationToken ct)
  - Task<WeeklyPlan> CreateOrReplace(WeeklyPlan plan, CancellationToken ct)
- IDishRepository
  - Task<IReadOnlyList<Dish>> GetAllWithIngredients(CancellationToken ct)
  - Task<Dish?> GetByIdWithIngredients(Guid dishId, CancellationToken ct)

2) Use-cases / handlers:
A) CreateWeeklyPlanFeature
- Input: weekStartDate + 7 dishIds (for day 0..6)
- Behavior:
  - Validate exactly 7 items and day_index covers 0..6 uniquely
  - Load each Dish with ingredients
  - Persist WeeklyPlan + Items (replace existing plan for that week if exists)
  - Return the created plan

B) GetWeeklyPlanFeature
- Input: weekStartDate
- Output: WeeklyPlan with Items including Dish + Ingredients

C) CreateShoppingListForWeekFeature
- Input: weekStartDate
- Behavior:
  - Load plan with dishes and ingredients
  - Aggregate shopping list:
    - ignore optional ingredients
    - aggregate by (Ingredient.Id + Unit)
    - sum amounts
    - no unit conversion
    - order by Category then IngredientName
  - Return ShoppingList

DATABASE (Middagsklok.Database):
1) EF entities & schema:
- WeeklyPlanEntity
  - Id (Guid)
  - WeekStartDate (store as TEXT "YYYY-MM-DD" or INTEGER days; pick a consistent approach)
  - CreatedAt (DateTimeOffset)
- WeeklyPlanItemEntity
  - PlanId (Guid)
  - DayIndex (int)
  - DishId (Guid)

Constraints:
- Unique weekly plan per WeekStartDate (unique index).
- Items: unique (PlanId, DayIndex).
- FK to Dish table for DishId.

2) Repositories:
- Implement IWeeklyPlanRepository with EF Core:
  - CreateOrReplace:
    - If plan exists for weekStartDate: delete existing items + plan, then insert new
      OR update plan row and replace items (choose the simplest correct method).
  - GetByWeekStartDate:
    - Load plan + items + dishes + dish_ingredients + ingredients
    - AsNoTracking
    - Deterministic ordering (DayIndex)
  - Map EF entities → Domain models explicitly.

3) Ensure seeding:
- Keep existing dish/ingredient seed data from prior feature.
- Do NOT seed weekly plans by default unless needed for demo.

CONSOLE APP (Middagsklok.Console):
Add commands (simple menu or args):
- "plan create" → creates plan for next Monday (or current week's Monday) using 7 dish IDs from seeded dishes
- "plan show" → prints the weekly plan
- "plan shopping" → prints the shopping list for the week

Keep console thin: it just calls handlers and prints.

INTEGRATION TESTS (required):
Using SQLite in-memory:
1) CreateWeeklyPlanCommand persists 7 items and replaces existing plan for the same week.
2) GetWeeklyPlan returns items ordered by DayIndex with Dish populated.
3) GenerateShoppingList aggregates shared ingredients and ignores optional ingredients.

IMPORTANT:
- No EF types leak into Features/Domain.
- Use AsNoTracking for reads.
- Avoid N+1 queries.
- Keep it minimal: manual plan creation only (no auto planner).