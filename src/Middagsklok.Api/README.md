# Middagsklok API

Barebone .NET 10 Minimal API for Middagsklok.

## Getting Started

### Prerequisites
- .NET 10 SDK

### Running the API

```bash
cd src/Middagsklok.Api
dotnet run
```

The API will start on `http://localhost:5000`.

## Endpoints

### Health
- `GET /health` - Returns `{"status": "ok"}`

### Dishes
- `GET /dishes` - Returns list of dishes
  - Response: `[{id, name, activeMinutes, totalMinutes}, ...]`
- `POST /dishes/import` - Import dishes from JSON
  - Request body: `BatchImportDishesCommand`
  - Response: `BatchImportResult` with summary and details

### Weekly Plans
- `GET /weekly-plan/{weekStartDate}` - Get weekly plan by start date
  - Date format: `yyyy-MM-dd` (e.g., `2026-01-20`)
  - Response: `WeeklyPlan` or 404 if not found
- `POST /weekly-plan/generate` - Generate a new weekly plan
  - Request body: `GenerateWeeklyPlanRequest` with `weekStartDate`
  - Response: `GeneratedWeeklyPlanResult` with plan and explanations

## Configuration

Configuration is in `appsettings.json`:
- Connection string: `"DefaultConnection": "Data Source=middagsklok.db"`
- CORS: Enabled for `http://localhost:3000`

## Database

The API uses SQLite. The database is automatically created on startup if it doesn't exist. 
**No seed data is added** - use the import endpoint to add dishes manually.

## Example Import

```bash
curl -X POST http://localhost:5000/dishes/import \
  -H "Content-Type: application/json" \
  -d @sample-dishes.json
```

Sample JSON format:
```json
{
  "dishes": [
    {
      "name": "Pasta with Tomato Sauce",
      "activeMinutes": 20,
      "totalMinutes": 30,
      "kidRating": 4,
      "familyRating": 5,
      "isPescetarian": true,
      "hasOptionalMeatVariant": false,
      "tags": ["pasta", "quick"],
      "ingredients": [
        {
          "name": "Pasta",
          "category": "Grains",
          "amount": 400,
          "unit": "g",
          "optional": false
        }
      ]
    }
  ]
}
```
