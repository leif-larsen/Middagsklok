# Middagsklok

A meal planning application with a .NET backend and React frontend.

## Projects

- **Middagsklok** - Core library with domain models, features, and database
- **Middagsklok.Api** - ASP.NET Core Minimal API for HTTP access
- **Middagsklok.Tests.Unit** - Unit tests
- **Middagsklok.Tests.Integration** - Integration tests

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQLite

### Running the API

```bash
cd src/Middagsklok.Api
dotnet run
```

The API will start on `http://localhost:5000` (or the port configured in `launchSettings.json`).

### API Endpoints

#### Health
- `GET /health` - Returns `{ "status": "ok" }`

#### Dishes
- `GET /dishes` - List all dishes with basic info (id, name, activeMinutes, totalMinutes)
- `POST /dishes/import` - Batch import dishes from JSON

Example import payload:
```json
{
  "dishes": [
    {
      "name": "Pasta Carbonara",
      "activeMinutes": 20,
      "totalMinutes": 30,
      "kidRating": 4,
      "familyRating": 5,
      "isPescetarian": false,
      "hasOptionalMeatVariant": false,
      "tags": ["pasta", "quick"],
      "ingredients": [
        {
          "name": "pasta",
          "category": "pantry",
          "amount": 400,
          "unit": "g",
          "optional": false
        }
      ]
    }
  ]
}
```

#### Weekly Planning
- `GET /weekly-plan/{weekStartDate}` - Get weekly plan for a specific week (format: YYYY-MM-DD)
- `POST /weekly-plan/generate` - Generate a new weekly plan

Example generate payload:
```json
{
  "weekStartDate": "2026-01-20"
}
```

### Swagger UI

When running in Development mode, Swagger UI is available at `/swagger`.

### Database

The API uses SQLite and stores the database in:
- **Windows**: `%LOCALAPPDATA%\Middagsklok\middagsklok.db`
- **Linux/Mac**: `~/.local/share/Middagsklok/middagsklok.db`

The database is automatically initialized on first run with sample data.

## Development

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```