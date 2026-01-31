# Domain Model: `DishHistoryLog` (Dish Consumption)

## Purpose
`DishHistoryLog` records when a `Dish` was actually eaten, so the system can:
- prevent repeating the same dish within a time window (e.g., 7 days),
- show “last eaten” information,
- support smarter plan generation later.

This should be append-only: each record is one consumption event.

---

## Entity
**DishConsumptionEvent** (Entity)  
- Identity: `eventId` (stable, unique identifier)

---

## Attributes

| Field | Type | Required | Example | Notes |
|---|---|---:|---|---|
| `eventId` | UUID / string | ✅ | `"e91d2a..."` | Technical ID. |
| `dishId` | UUID / string | ✅ | `"c13a7f..."` | Reference to `Dish`. Live reference. |
| `eatenOn` | date (ISO-8601) | ✅ | `"2026-01-27"` | The date the dish was consumed. |
| `source` | `DishHistorySource` (enum) | ✅ | `WEEKLY_PLAN` | Where the event came from. |
| `weeklyPlanId` | UUID / string | ❌ | `"b2a91c..."` | Optional link if created from a plan. |
| `createdAt` | datetime | (recommended) | `"2026-01-27T20:10:00Z"` | When the log entry was recorded. |

---

## Enums / Value Sets

### `DishHistorySource`
| Enum name | Display name |
|---|---|
| `WEEKLY_PLAN` | Weekly plan |
| `MANUAL` | Manual entry |

> Keep this even if you “don’t care yet” — it costs almost nothing and prevents ambiguity later.

---

## Invariants (Domain Rules)

- `dishId` must reference an existing `Dish`.
- `eatenOn` must be a valid date.
- Append-only behavior: events should not be edited in normal operation (only corrected by adding a new event or an admin action).
- Recommended uniqueness constraint to prevent accidental duplicates:
  - `(dishId, eatenOn)` should be unique **unless** you intentionally allow the same dish to be eaten twice on the same day.

---

## Derived / Computed Values

- `lastEatenOn(dishId) = MAX(eatenOn) for that dishId`
- `eatenWithinWindow(dishId, startDate, endDate) = any event where eatenOn in range`

---

## Example (JSON)

```json
{
  "eventId": "e91d2a2c-4df0-4f2f-9aa8-01e4f8b2b8c0",
  "dishId": "c13a7f5b-9a2e-4f6a-9b21-1e73a8d4fabc",
  "eatenOn": "2026-01-27",
  "source": "WEEKLY_PLAN",
  "weeklyPlanId": "b2a91c36-0c3c-4b77-9e55-6f3a3e9f12aa",
  "createdAt": "2026-01-27T20:10:00Z"
}
```

## Practical Notes (what you should NOT overbuild)
- Don’t store lastEaten as a column in the log — it becomes inconsistent.
- If you need fast lookup for “last eaten” across many dishes, add a separate read model:
    - DishLastEaten { dishId, lastEatenOn, updatedAt }
- maintained by updating on insert (or by a scheduled job).

This is an optimization, not the source of truth.