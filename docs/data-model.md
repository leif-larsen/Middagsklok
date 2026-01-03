# Middagsklok – Data Model Documentation

## Overview
This document describes the full data model for Middagsklok.

The data model is designed to support:
- Weekly dinner planning (7 dinners per week)
- Pescetarian baseline with optional meat variants
- Time constraints (active vs total time)
- Ingredient-based shopping list generation
- Historical tracking of meals
- Optional integration with store discounts (Oda, Rema 1000, Kiwi)
- Future migration from SQLite to PostgreSQL

The database is the source of truth.  
Derived values must always be computed, never stored as mutable state.

---

## Core Principles
- History is append-only.
- "Last eaten" is derived from history, never stored directly.
- Active cooking time and total cooking time are separate concepts.
- Discounts influence planning through scoring, never hard constraints.
- Schema must remain portable to PostgreSQL.

---

## Entity Overview (High Level)

- Dish
- DishTag
- Ingredient
- DishIngredient
- DishHistory
- WeeklyPlan
- WeeklyPlanItem
- Store
- Offer
- OfferIngredientMap

---

## Dish

Represents a cookable dinner dish.

### Purpose
Used as the core planning unit for weekly dinner plans.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| id | UUID | Primary key |
| name | TEXT | Human-readable name of the dish |
| active_minutes | INT | Hands-on cooking time |
| total_minutes | INT | Total cooking time including waiting |
| kid_rating | INT (1–5) | How well the dish is received by the child |
| family_rating | INT (1–5) | Overall family preference |
| is_pescetarian | BOOLEAN | True if baseline dish contains no meat |
| has_optional_meat_variant | BOOLEAN | True if meat can be added optionally |
| notes | TEXT | Free-form notes |
| source_url | TEXT | Optional recipe reference |

### Notes
- `is_pescetarian = true` for all baseline dishes.
- Meat variants are modeled implicitly, not as separate dishes.
- Ratings are advisory, not constraints.

---

## DishTag

Tags provide flexible classification for dishes.

### Purpose
Used for:
- Variety control
- Planning constraints (e.g. fish)
- Scoring and explainability

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| dish_id | FK → Dish.id | Dish reference |
| tag | TEXT | Tag value (e.g. "fish", "taco", "pasta", "quick") |

### Examples
- fish
- vegetarian
- comfort
- kid_friendly
- quick
- weekend

---

## Ingredient

Represents a normalized raw ingredient.

### Purpose
Forms the basis for shopping lists and discount matching.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| id | UUID / INT | Primary key |
| name | TEXT | Normalized ingredient name |
| category | TEXT | produce, fish, dairy, dry, frozen, etc. |
| default_unit | TEXT | g, ml, stk, etc. |

### Notes
- Ingredient names must be singular and normalized.
- Categories are for shopping list grouping only.

---

## DishIngredient

Join table between Dish and Ingredient.

### Purpose
Defines required and optional ingredients per dish.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| dish_id | FK → Dish.id | Dish reference |
| ingredient_id | FK → Ingredient.id | Ingredient reference |
| amount | DECIMAL | Quantity required |
| unit | TEXT | Unit of measurement |
| optional | BOOLEAN | True if ingredient is optional |

### Notes
- Optional ingredients are excluded from mandatory shopping lists.
- Units must be compatible with ingredient default units.

---

## DishHistory

Records every time a dish is cooked.

### Purpose
Used to:
- Derive "last eaten"
- Penalize recent repeats
- Support explainable planning

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| id | UUID / INT | Primary key |
| dish_id | FK → Dish.id | Dish reference |
| date | DATE | Date the dish was eaten |
| rating_override | INT (1–5, nullable) | Optional rating for this instance |
| notes | TEXT | Observations or adjustments |

### Notes
- This table is append-only.
- No updates or deletes except for data correction.

---

## WeeklyPlan

Represents a generated weekly dinner plan.

### Purpose
Allows storing, reviewing, and comparing plans over time.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| id | UUID / INT | Primary key |
| week_start_date | DATE | Monday of the week |
| created_at | DATETIME | When the plan was generated |

---

## WeeklyPlanItem

Represents a single planned dinner in a week.

### Purpose
Maps dishes to specific days in a weekly plan.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| plan_id | FK → WeeklyPlan.id | Weekly plan reference |
| day_index | INT (0–6) | Day of week (0 = Monday) |
| dish_id | FK → Dish.id | Selected dish |

---

## Store

Represents a grocery store.

### Purpose
Used as a source for offers and pricing.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| id | UUID / INT | Primary key |
| name | TEXT | Store name (Oda, Rema1000, Kiwi) |

---

## Offer

Represents a raw discount or price offer.

### Purpose
Provides optional input to planning logic.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| id | UUID / INT | Primary key |
| store_id | FK → Store.id | Store reference |
| title | TEXT | Raw offer title |
| price | DECIMAL | Offer price |
| unit | TEXT | Unit (if available) |
| valid_from | DATE | Offer start date |
| valid_to | DATE | Offer end date |
| url | TEXT | Source URL |
| fetched_at | DATETIME | When the offer was imported |

### Notes
- Offer titles are not trusted or normalized.
- Offers are immutable snapshots.

---

## OfferIngredientMap

Maps offers to ingredients.

### Purpose
Bridges messy real-world offers with clean ingredient data.

### Fields
| Field | Type | Description |
|-----|-----|-------------|
| offer_id | FK → Offer.id | Offer reference |
| ingredient_id | FK → Ingredient.id | Ingredient reference |
| confidence | DECIMAL (0–1) | Mapping confidence |
| method | TEXT | manual, fuzzy, synonym |

### Notes
- Manual mappings override automated ones.
- Confidence is used for debugging and explainability, not logic.

---

## Derived Concepts (Not Stored)

These values are computed dynamically:
- Last eaten date per dish
- Weekly ingredient totals
- Shopping lists
- Discount influence on scoring
- Plan explanations and scoring breakdowns

---

## Future Extensions (Out of Scope for v1)
- Pantry / inventory tracking
- Nutritional data
- Cost optimization across stores
- Multiple household profiles
- Leftover tracking

---

## Summary
This data model prioritizes:
- Explainability
- Practical constraints
- Long-term maintainability
- Migration safety

It intentionally avoids premature optimization and over-modeling.