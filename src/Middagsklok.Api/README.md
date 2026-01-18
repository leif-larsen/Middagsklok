# Middagsklok API

A minimal .NET 10 API that provides HTTP endpoints for the Middagsklok meal planning application.

## Overview

This API serves as a thin adapter layer over the existing Middagsklok features, exposing them via HTTP endpoints for use by frontend applications.

## Features

- **Local-only operation**: Designed for local development with SQLite database
- **No authentication**: Simple API without auth complexity
- **Minimal API style**: Uses .NET 10 Minimal API (no MVC/controllers)
- **CORS enabled**: Allows requests from http://localhost:3000

## Running the API

```bash
cd src/Middagsklok.Api
dotnet run
```

The API will start on http://localhost:5000 by default.

## Endpoints

### Health Check
- **GET** `/health`
- Returns: `{ "status": "ok" }`

### Dishes

#### Get All Dishes
- **GET** `/dishes`
- Returns: Array of dishes with ingredients
- Response format (camelCase):
```json
[
  {
    "id": "guid",
    "name": "string",
    "activeMinutes": 20,
    "totalMinutes": 40,
    "kidRating": 4,
    "familyRating": 5,
    "isPescetarian": true,
    "hasOptionalMeatVariant": false,
    "ingredients": [...]
  }
]
```

#### Import Dishes
- **POST** `/dishes/import`
- Accepts: Batch import command with array of dishes
- Request format:
```json
{
  "dishes": [
    {
      "name": "Dish Name",
      "activeMinutes": 20,
      "totalMinutes": 40,
      "kidRating": 4,
      "familyRating": 5,
      "isPescetarian": false,
      "hasOptionalMeatVariant": false,
      "tags": ["tag1"],
      "ingredients": [
        {
          "name": "Ingredient",
          "category": "Category",
          "amount": 100,
          "unit": "g",
          "optional": false
        }
      ]
    }
  ]
}
```
- Returns: Import result with created/skipped/failed counts

### Weekly Plans

#### Get Weekly Plan
- **GET** `/weekly-plan/{weekStartDate}`
- Parameters: `weekStartDate` in YYYY-MM-DD format (e.g., 2026-01-19)
- Returns: Weekly plan with 7 days of dishes, or 404 if not found

#### Generate Weekly Plan
- **POST** `/weekly-plan/generate`
- Accepts: Generation request
```json
{
  "weekStartDate": "2026-01-19",
  "rules": {  // optional
    "minFishDinnersPerWeek": 2,
    "weekdayMaxTotalMinutes": 45,
    "weekendMaxTotalMinutes": 90
  }
}
```
- Returns: Generated weekly plan with explanations for each day's selection

## Database

The API uses SQLite with automatic database initialization. The database file is created at:
- `%LOCALAPPDATA%/middagsklok.db` (Windows)
- `~/.local/share/middagsklok.db` (Linux/macOS)

On first run, the database is seeded with sample dishes.

## Architecture

- **No business logic**: All logic stays in the Middagsklok.Features namespace
- **Thin adapter**: API only handles HTTP concerns (routing, serialization, status codes)
- **DI configured**: All features and repositories are dependency-injected
- **Explicit JSON mapping**: Responses are explicitly mapped to ensure consistent camelCase format

## Testing

Use the included `Middagsklok.Api.http` file with Visual Studio or HTTP clients to test endpoints.
