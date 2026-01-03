You are working in a .NET 10 solution with a clean architecture.

Authoritative schema doc:
- docs/data-model.md is the source of truth for entities, fields, and relationships.
Do not invent fields. If something is missing, implement only the minimal subset required for this feature.

Project structure:
- Middagsklok (main library)
  - Domain (pure domain models, no EF references)
  - Features (use-cases, no EF references)
  - Database (EF Core + SQLite, EF entities + mapping live here)
- Middagsklok.App (temporary entry point)
- Middagsklok.Tests.Unit
- Middagsklok.Tests.Integration

GOAL FEATURE:
"List dishes with ingredients from SQLite"

SCOPE:
Implement read-only persistence for:
- Dish
- Ingredient
- DishIngredient
Optionally DishTag if it is already in docs/data-model.md and easy to include.
Out of scope:
- DishHistory
- WeeklyPlan
- Offers/Stores

ARCHITECTURE RULES (non-negotiable):
- Domain models are pure and use Guid for all Id fields (as stated in docs/data-model.md).
- Database layer contains EF Core DbContext AND EF entities (separate from Domain models).
- Features layer exposes repository interfaces and query handlers.
- Only Database implements repositories and contains EF code.
- Console must not talk to DbContext directly; Console calls Feature.

EF CORE RULES:
- SQLite provider.
- No lazy loading.
- Read queries must use AsNoTracking().
- Avoid N+1: use Includes or a single query with projection.
- Deterministic ordering (ORDER BY / ThenBy) so console output and tests are stable.

IMPLEMENTATION TASKS:

1) DOMAIN (Middagsklok.Domain)
Create/ensure these domain models (pure POCOs, no EF attributes):
- Ingredient (Guid Id, string Name, string Category, string DefaultUnit)
- Dish (Guid Id, string Name, int ActiveMinutes, int TotalMinutes, int KidRating, int FamilyRating, bool IsPescetarian, bool HasOptionalMeatVariant, IReadOnlyList<DishIngredient> Ingredients)
- DishIngredient (Ingredient Ingredient, decimal Amount, string Unit, bool Optional)

Keep domain models simple and immutable where practical.

2) DATABASE (Middagsklok.Database)
Create EF entities mirroring the database tables described in docs/data-model.md.
- IngredientEntity
- DishEntity
- DishIngredientEntity (join table)
Add DbContext: MiddagsklokDbContext

Mapping constraints:
- EF entities may have navigation properties for EF only.
- Use Fluent API configurations (IEntityTypeConfiguration) to define schema.
- Store Guid values in SQLite as TEXT (or BLOB if you prefer), but keep Guid in Domain.
- Ensure uniqueness constraints for dish.name and ingredient.name (as in docs/data-model.md).
- DishIngredient primary key should be composite and stable (match docs/data-model.md or choose a sensible composite key).

Add DbBootstrapper:
- Applies migrations OR EnsureCreated (choose ONE; prefer migrations if fast to set up).
- Seed only when tables are empty.
- Seed at least:
  - 2 dishes
  - 6 ingredients
  - each dish has 3–5 dish_ingredient rows
  - at least one shared ingredient across dishes
  - at least one optional ingredient row

Seed data should use hardcoded deterministic Guids for stable tests.

3) FEATURES (Middagsklok.Features)
Define repository interfaces:
- IDishRepository
  - Task<IReadOnlyList<Dish>> GetAllWithIngredients(CancellationToken ct)
- IIngredientRepository (optional if needed later)

Implement a query/use-case:
- GetDishes (or similar)
  - Calls IDishRepository.GetAllWithIngredients
  - Returns IReadOnlyList<Dish>

4) DATABASE REPOSITORY IMPLEMENTATION
Implement IDishRepository in Database using EF Core:
- Query DishEntity + DishIngredientEntity + IngredientEntity
- Use AsNoTracking
- Return mapped Domain objects:
  - Dish with Ingredients populated
  - DishIngredient includes the mapped Ingredient domain object
Do NOT return EF entities outside Database.

Add mapping code:
- Either a dedicated mapper class (e.g., DomainMapper) or private mapping methods inside repository.
- Keep mapping explicit and readable (no magic reflection, no AutoMapper).

5) CONSOLE APP (Middagsklok.App)
- Configure DI:
  - DbContext (SQLite file "middagsklok.db")
  - DbBootstrapper
  - IDishRepository implementation
  - ListDishesQueryHandler
- On startup:
  - Run bootstrapper
  - Call handler
  - Print dishes sorted by name:
    - Dish name + "(active/total)"
    - Ingredients lines: "- name amount unit" and mark "(optional)" when Optional=true

6) TESTING
Integration tests (Middagsklok.Tests.Integration) using SQLite in-memory:
- Bootstrapping seeds when empty.
- Repository returns dishes with correct ingredient counts and optional flag.
- Deterministic ordering.

Unit tests (optional here):
- If you introduce non-trivial mapping helpers, unit test them.

IMPORTANT:
- Do not introduce AutoMapper.
- Do not leak EF Core types into Domain or Features.
- Keep this feature minimal: list dishes with ingredients only.