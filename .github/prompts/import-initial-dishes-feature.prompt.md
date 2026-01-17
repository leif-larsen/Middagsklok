You are working in a .NET 10 solution with a clean architecture.

Authoritative schema doc:
- docs/data-model.md is the source of truth for tables/fields/relationships.

Project structure:
- Middagsklok (main library)
  - Domain (pure domain models)
  - Features (use-cases)
  - Database (EF Core + SQLite, EF entities + mapping)
- Middagsklok.App (temporary entry point)
- Middagsklok.Tests.Integration

GOAL FEATURE:
"Batch import dishes from a JSON file (multiple dishes at once)."

SCOPE:
Implement:
- Batch import command that accepts a JSON file containing a list of dishes
- Upsert ingredients (by normalized name)
- Enforce dish name uniqueness (case-insensitive)
- Insert dish_ingredient rows and dish_tag rows (if DishTag exists)
- Return a per-dish result (created / skipped / failed with error)
- Minimal list command to verify (dish list)

Out of scope:
- Update/delete dishes
- UI
- Offers/discounts
- DishHistory changes

ARCHITECTURE RULES:
- Domain models are pure; no EF references.
- Database layer contains DbContext + EF entities + configurations + repository implementations.
- Features contain interfaces + handlers only (no EF).
- Console calls handlers only.

FEATURES (Middagsklok.Features)

1) Import models (DTOs):
- BatchImportDishesFeature
  - IReadOnlyList<AddDishFeature> Dishes

Reuse AddDishFeature and AddDishIngredientItem from previous feature.

- BatchImportResult
  - int Total
  - int Created
  - int Skipped
  - int Failed
  - IReadOnlyList<BatchImportDishResult> Results

- BatchImportDishResult
  - string Name
  - string Status  ("created" | "skipped" | "failed")
  - Guid? DishId
  - string? Error

2) Handler:
- BatchImportDishesFeature
  - Task<BatchImportResult> Execute(BatchImportDishesFeature cmd, CancellationToken ct)

Rules:
- Validate the command has at least 1 dish.
- For each dish:
  - Run the same validations as AddDish.
  - If dish name already exists (case-insensitive):
    - Status = "skipped"
    - DishId = existing id
    - continue
  - Otherwise create dish + ingredients + joins.
  - If one dish fails, the import continues for other dishes.
- Use transaction PER DISH to avoid partial inserts for that dish.
- Do not use one big transaction for the entire file.

Repository:
- Create IDishImportRepository (or extend IDishWriteRepository) with:
  - Task<Guid?> FindDishIdByName(string nameNormalized, CancellationToken ct)
  - Task<Guid> InsertDish(AddDishCommand cmd, CancellationToken ct)
Keep names minimal; no frameworks.

DATABASE (Middagsklok.Database)
- Implement the repository using EF Core.
- Enforce uniqueness:
  - Prefer a normalized name column + unique index if schema allows.
  - If not, enforce via query check (acceptable for local app).
- Ingredient upsert by normalized name.
- Aggregate duplicate ingredient lines within each dish input (same ingredient + unit + optional) before saving.

CONSOLE (Middagsklok.App)
Update/replace command:
- "dish import <path-to-json>"
Behavior:
- Read JSON file into BatchImportDishesFeature using System.Text.Json.
- Call BatchImportDishesFeature.
- Print summary:
  - Total/Created/Skipped/Failed
- Print per-dish results (Name + Status + optional error)

JSON FORMAT:
The file contains either:
Option A (preferred): wrapper object:
{
  "dishes": [
    { ...AddDishCommand... },
    { ...AddDishCommand... }
  ]
}

Use this exact schema.

Example:
{
  "dishes": [
    {
      "name": "Laks med ovnsgrønnsaker",
      "activeMinutes": 15,
      "totalMinutes": 35,
      "kidRating": 4,
      "familyRating": 5,
      "isPescetarian": true,
      "hasOptionalMeatVariant": false,
      "tags": ["fish", "quick"],
      "ingredients": [
        {"name": "laks", "category": "fish", "amount": 600, "unit": "g", "optional": false},
        {"name": "potet", "category": "produce", "amount": 800, "unit": "g", "optional": false}
      ]
    },
    {
      "name": "Veggie taco",
      "activeMinutes": 20,
      "totalMinutes": 35,
      "kidRating": 4,
      "familyRating": 4,
      "isPescetarian": true,
      "hasOptionalMeatVariant": true,
      "tags": ["taco", "quick"],
      "ingredients": [
        {"name": "tortilla", "category": "dry", "amount": 8, "unit": "stk", "optional": false},
        {"name": "bønner", "category": "dry", "amount": 2, "unit": "boks", "optional": false},
        {"name": "ost", "category": "dairy", "amount": 200, "unit": "g", "optional": true}
      ]
    }
  ]
}

INTEGRATION TESTS (SQLite in-memory):
1) Imports multiple dishes:
- 2 dishes in one JSON -> 2 created.
2) Skips existing dish by name (case-insensitive) and returns existing id.
3) Continues on error:
- One dish invalid (e.g., rating=99) -> Failed for that dish, others still created.
4) Ingredient upsert:
- Shared ingredient across imported dishes is not duplicated in ingredient table.
5) Duplicate ingredient lines within a dish are aggregated before save.

IMPORTANT:
- No EF types in Features/Domain.
- Keep it explicit and readable.
- No AutoMapper, no MediatR.