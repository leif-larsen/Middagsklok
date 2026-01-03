You are working in a .NET 10 solution with a clean architecture.

Project structure:
- Middagsklok (main library)
  - Domain (pure domain models, no infrastructure)
  - Features (application use-cases)
- Middagsklok.App (console app, temporary entry point)

TASK:
Implement the feature "Generate Shopping List from Weekly Plan".

RULES:
- Follow strict separation of concerns.
- Domain contains only data models and invariants.
- Features contain orchestration logic.
- No database, no persistence, no external dependencies.
- No business logic in the console project.

DOMAIN MODELS (place in Middagsklok.Domain):
- Ingredient
  - Id
  - Name
  - Category
  - DefaultUnit
- Dish
  - Id
  - Name
  - Ingredients : List<DishIngredient>
- DishIngredient
  - Ingredient
  - Amount
  - Unit
  - Optional
- WeeklyPlan
  - WeekStartDate
  - Items : List<WeeklyPlanItem>
- WeeklyPlanItem
  - DayIndex (0–6)
  - Dish

FEATURE (place in Middagsklok.Features):
- GetShoppingList
  - Method:
    Generate(WeeklyPlan plan) -> ShoppingList

- ShoppingList
  - Items : List<ShoppingListItem>

- ShoppingListItem
  - IngredientName
  - Category
  - Amount
  - Unit

LOGIC RULES:
- Ignore DishIngredient where Optional = true
- Aggregate by (Ingredient.Id + Unit)
- Sum Amounts
- Do NOT convert units
- Sort output by Category, then IngredientName
- Deterministic output (stable ordering)

CONSOLE APP:
- Create a small hardcoded WeeklyPlan with 3–4 dishes
- Include duplicate ingredients across dishes
- Print the generated shopping list to console

TESTS:
- Add at least one unit test verifying:
  - Optional ingredients are ignored
  - Amounts are aggregated correctly

IMPORTANT:
- Keep the implementation simple and explicit.
- Do not introduce helpers, utils, or abstractions not required by the task.