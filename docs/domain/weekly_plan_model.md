# Domain Model: `WeeklyPlan`

## Purpose
`WeeklyPlan` represents a 7-day meal plan anchored to a configurable `startDate`.
Each day in the plan has an explicit selection state: either a chosen `Dish` or an explicit "empty" selection.

The plan references `Dish` by ID and always reflects the latest version of the dish (live reference).

---

## Entity
**WeeklyPlan** (Entity)  
- Identity: `weeklyPlanId` (stable, unique identifier)

---

## Attributes

| Field | Type | Required | Example | Notes |
|---|---|---:|---|---|
| `weeklyPlanId` | UUID / string | ✅ | `"b2a91c..."` | Technical ID. |
| `startDate` | date (ISO-8601) | ✅ | `"2026-01-26"` | Anchors the 7-day range. Not tied to Monday. |
| `days` | `PlannedDay[]` | ✅ | see below | Must contain exactly 7 entries. |
| `createdAt` | datetime | (recommended) | `"2026-01-25T18:00:00Z"` | Audit field. |
| `updatedAt` | datetime | (recommended) | `"2026-01-26T09:15:00Z"` | Audit field. |

---

## Value Objects

### `PlannedDay`
Represents one calendar date inside the plan and its selection state.

| Field | Type | Required | Example | Notes |
|---|---|---:|---|---|
| `date` | date (ISO-8601) | ✅ | `"2026-01-26"` | Must be within `[startDate, startDate + 6]`. |
| `selection` | `DishSelection` | ✅ | see below | Explicitly tracks “empty” vs selected. |

### `DishSelection`
A discriminated union describing either a selected dish or an explicit empty selection.

**Option A (object union)**
- `{"type":"EMPTY"}`
- `{"type":"DISH","dishId":"..."}`

| Field | Type | Required | Example |
|---|---|---:|---|
| `type` | enum `DishSelectionType` | ✅ | `EMPTY` / `DISH` |
| `dishId` | UUID / string | conditional | `"c13a7f..."` |

### `DishSelectionType`
| Enum name | Display name |
|---|---|
| `EMPTY` | No dish |
| `DISH` | Dish selected |

---

## Invariants (Domain Rules)

- `startDate` must be a valid date.
- `days.length` must be exactly `7`.
- `days[*].date` must be **unique** within the plan.
- `days[*].date` must cover the full contiguous range:
  - `days` contains every date from `startDate` through `startDate + 6` (no gaps).
- For each day:
  - if `selection.type == DISH` then `selection.dishId` must be present and reference an existing `Dish`.
  - if `selection.type == EMPTY` then `dishId` must be absent/null.

---

## Uniqueness Constraint

- Only one `WeeklyPlan` may exist for the same `startDate` (single-user assumption).
  - If/when you add users/tenants later, scope this to `(ownerId, startDate)`.

---

## Derived / Computed Values

- `endDate = startDate + 6 days`
- `plannedDishes = days where selection.type == DISH`

---

## Supporting Data (Dish Lookup)
The weekly planner UI needs a lightweight list of dishes for selection.
Expose a lookup endpoint that returns only the data needed for the picker.

**Endpoint**
- `GET /dishes/lookup`

**Response (JSON)**
```json
{
  "dishes": [
    { "id": "c13a7f...", "name": "Spaghetti Carbonara", "dishType": "Pasta" }
  ]
}
```


---

## Example (JSON)

```json
{
  "weeklyPlanId": "b2a91c36-0c3c-4b77-9e55-6f3a3e9f12aa",
  "startDate": "2026-01-26",
  "days": [
    { "date": "2026-01-26", "selection": { "type": "DISH", "dishId": "dish-carbonara" } },
    { "date": "2026-01-27", "selection": { "type": "DISH", "dishId": "dish-thai-green-curry" } },
    { "date": "2026-01-28", "selection": { "type": "DISH", "dishId": "dish-greek-salad" } },
    { "date": "2026-01-29", "selection": { "type": "EMPTY" } },
    { "date": "2026-01-30", "selection": { "type": "DISH", "dishId": "dish-chicken-tikka-masala" } },
    { "date": "2026-01-31", "selection": { "type": "EMPTY" } },
    { "date": "2026-02-01", "selection": { "type": "EMPTY" } }
  ],
  "createdAt": "2026-01-25T18:00:00Z",
  "updatedAt": "2026-01-26T09:15:00Z"
}
```
