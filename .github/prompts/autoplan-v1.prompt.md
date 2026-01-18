---
description: Ruleset for implementing the first version of the weekly planning validator in Middagsklok.
model: Claude Sonnet 4.5 (copilot)
agent: Plan
---

You are working in a .NET 10 solution with a clean architecture.

Project structure:
- Middagsklok (main library)
  - Domain
  - Features
  - Database
- Middagsklok.App
- Middagsklok.Tests.Unit
- Middagsklok.Tests.Integration

PREREQUISITES (already exist):
- WeeklyPlanRulesValidator (validates rules R1-R4)
- PlanningRules (45/60/2 default)
- WeeklyPlan + WeeklyPlanItem loaded with Dish + Ingredients
- IDishRepository (read dishes with ingredients)
- IDishHistoryRepository (GetLastEatenByDish)
- IWeeklyPlanRepository (CreateOrReplace, GetByWeekStartDate)

GOAL FEATURE:
Auto-generate a weekly plan (7 dinners) that passes the rule validator.

SCOPE (minimal but valuable):
- Generate a plan deterministically (given same input, same output).
- Use hard constraints from PlanningRules:
  - 7 unique dishes
  - time limits weekday/weekend
  - min fish dinners (by ingredient category == "fish")
- Use simple scoring:
  - prefer dishes not eaten recently
  - prefer higher kid/family ratings
  - prefer lower activeMinutes on weekdays
- Return explainability: per day, include 1–3 short reasons.
- Persist the generated plan (replace existing plan for the week).
- Add tests.

OUT OF SCOPE:
- Offers/discounts
- Ingredient reuse optimization
- Advanced variety (protein rotation, cuisine variety)
- User preferences beyond ratings
- Any randomness without explicit seed

DOMAIN (Middagsklok.Domain)
Add minimal support types:
1) PlannedDishExplanation
- Guid DishId
- IReadOnlyList<string> Reasons

2) GeneratedWeeklyPlanResult
- WeeklyPlan Plan
- IReadOnlyDictionary<int, PlannedDishExplanation> ExplanationsByDay

Keep these POCOs only.

FEATURES (Middagsklok.Features)
Add:
1) GenerateWeeklyPlanFeature
- DateOnly WeekStartDate
- PlanningRules? Rules (optional; if null use defaults)

2) GenerateWeeklyPlanFeature
- Dependencies:
  - IDishRepository
  - IDishHistoryRepository
  - IWeeklyPlanRepository
  - WeeklyPlanRulesValidator
  - IClock/TimeProvider (only if needed; avoid if not necessary)
- Method:
  Task<GeneratedWeeklyPlanResult> Execute(GenerateWeeklyPlanFeature cmd, CancellationToken ct)

ALGORITHM REQUIREMENTS (keep explicit, no complex frameworks):
Step A: Load candidates
- Load all dishes with ingredients from IDishRepository
- Filter candidates by time constraints per day:
  - For weekdays (0-4): Dish.TotalMinutes <= WeekdayMaxTotalMinutes
  - For weekend (5-6): Dish.TotalMinutes <= WeekendMaxTotalMinutes

Step B: Determine fish dishes
- A dish is “fish” if any ingredient category == "fish".

Step C: Score function (simple)
- Use last eaten from IDishHistoryRepository.GetLastEatenByDish()
- For each dish, compute a base score:
  - + familyRating * 2
  - + kidRating * 2
  - + (daysSinceLastEaten / 3)  (cap to avoid huge numbers)
  - - activeMinutes (weekday only weighting applied later)
If never eaten: treat daysSinceLastEaten as a high value (e.g., 999 capped).

Step D: Plan construction (deterministic greedy)
- Create empty selection for day 0..6.
- First, satisfy fish minimum:
  - Select the top-scoring fish dishes for two distinct days within 0..6,
    preferring weekdays first unless they violate time constraints.
- Then fill remaining days:
  - For each day (0..6 in order):
    - choose the highest scoring dish that:
      - is not already chosen
      - fits that day’s time constraints
    - For weekdays, slightly favor lower activeMinutes:
      - dayScore = baseScore - activeMinutes
      - weekend dayScore = baseScore

Step E: Validate and fallback
- Build WeeklyPlan object (7 items, dayIndex 0..6, Dish populated)
- Run WeeklyPlanRulesValidator.Validate(plan, rules)
- If violations exist:
  - Apply a simple fallback:
    - For each violation type, attempt to fix by swapping offending day with next-best candidate.
    - Keep it bounded: max 50 swap attempts.
  - If still failing after bounded attempts, throw a clear error with violations summary.

Step F: Persist
- Persist plan via IWeeklyPlanRepository.CreateOrReplace(plan)

Step G: Explainability
For each selected dish/day, produce 1–3 short reasons:
- If fish and needed for minimum: "Fish requirement"
- If never eaten or long time: "Not eaten recently (X days)"
- If high kid rating: "Good kid rating (N)"
- If fits time: "Fits time limit (TotalMinutes)"

Ensure reasons are deterministic and not too verbose.

CONSOLE (Middagsklok.App)
Add command:
- "plan generate <weekStartDate>"
Behavior:
- Calls GenerateWeeklyPlanFeature
- Prints plan day by day with dish name and reasons
- After printing, runs validate and prints OK/violations (should be OK)

TESTING
Unit tests (recommended):
- Given a small in-memory set of dishes + last-eaten map, generator produces:
  - 7 unique dishes
  - at least 2 fish dishes
  - respects time limits
  - deterministic output ordering

Integration tests (SQLite in-memory):
- With seeded dishes and some dish history:
  - "generate" persists a plan with 7 items
  - loading the plan returns dishes + ingredients populated
  - validator returns no violations

IMPORTANT:
- Do not introduce randomness. If you must break ties, do it by stable ordering (e.g., dish name then id).
- Keep implementation explicit (avoid building a generic rule engine).
- No EF types outside Database.