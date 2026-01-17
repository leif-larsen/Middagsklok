---
description: Ruleset for implementing the first version of the weekly planning validator in Middagsklok.
model: Claude Sonnet 4.5 (copilot)
agent: Plan
---

You are working in a .NET 10 solution with a clean architecture.

Project structure:
- Middagsklok (main library)
  - Domain (pure domain models)
  - Features (use-cases)
  - Database (EF Core + SQLite)
- Middagsklok.App (temporary entry point)
- Middagsklok.Tests.Unit
- Middagsklok.Tests.Integration

GOAL (minimal but valuable):
Implement the first version of the weekly planning rule set as a validator.
It validates an existing WeeklyPlan and returns a list of rule violations with clear messages.

SCOPE (keep it small):
- Add rule models
- Add a validator that checks 4 rules
- Add a console command that validates a stored weekly plan
- Add unit tests for the rules

OUT OF SCOPE:
- Auto-generating a plan
- Scoring / optimization
- Offers / discounts
- DishHistory usage (for now)

DOMAIN (Middagsklok.Domain)
Add:
1) PlanningRules (configuration object)
- int WeekdayMaxTotalMinutes (default 45)
- int WeekendMaxTotalMinutes (default 60)
- int MinFishDinnersPerWeek (default 2)

2) RuleViolation
- string RuleCode
- string Message
- IReadOnlyList<int> DayIndices (optional, can be empty)
- IReadOnlyList<Guid> DishIds (optional, can be empty)

Keep them as pure POCOs.

FEATURES (Middagsklok.Features)
Add:
1) WeeklyPlanRulesValidator
- Method:
  Validate(WeeklyPlan plan, PlanningRules rules) -> IReadOnlyList<RuleViolation>

2) Implement exactly these rules (minimal set that still has real value):
R1) Exactly 7 items in plan and day_index is a complete unique set 0..6.
- If missing/duplicate day_index, return violation with the problematic day indices.

R2) No duplicate dishes within the same week (Dish.Id must be unique across items).
- If duplicates exist, return violation listing the duplicate dishIds.

R3) Time limits:
- For day 0..4 (weekdays): Dish.TotalMinutes <= WeekdayMaxTotalMinutes
- For day 5..6 (weekend): Dish.TotalMinutes <= WeekendMaxTotalMinutes
- Return violation listing offending days and dishIds.

R4) Fish minimum:
- At least MinFishDinnersPerWeek “fish dishes” in the plan.
- Define “fish dish” as: a dish that has at least one Ingredient with Category == "fish"
  (Dish.Ingredients -> DishIngredient.Ingredient.Category)
- Return violation if count < min.

IMPORTANT:
- The validator must not query the database. It works only with the WeeklyPlan object passed in (already loaded with dishes + ingredients).
- Output must be deterministic and testable.

CONSOLE (Middagsklok.App)
Add a command:
- "plan validate <weekStartDate>"
Behavior:
- Load plan via existing GetWeeklyPlanFeature (from your previous feature).
- Create default PlanningRules (45/60/2).
- Run validator.
- If no violations, print "OK".
- Else print each violation: RuleCode + Message + days.

TESTS (Middagsklok.Tests.Unit)
Add unit tests for each rule:
- R1: missing day or duplicate day index -> violation
- R2: same dish twice -> violation
- R3: weekday dish too long -> violation
- R4: fish count less than 2 -> violation

KEEP IT SMALL:
- Do not introduce new frameworks (no MediatR required).
- Do not create a complex rule engine. Just implement these 4 checks explicitly.
- Keep messages short and clear.

Start with a plan, and once I say "continue", implement the next step.