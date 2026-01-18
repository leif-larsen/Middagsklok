# Middagsklok

Weekly meal planning application with a .NET backend and Next.js frontend.

## Project Structure

- `src/Middagsklok` - Core business logic library
- `src/Middagsklok.Api` - ASP.NET Core Web API
- `frontend/` - Next.js frontend application
- `tests/` - Unit and integration tests

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+ and npm
- SQLite (included with .NET)

### Running the API

```bash
# From the repository root
dotnet run --project src/Middagsklok.Api/Middagsklok.Api.csproj
```

The API will start on `http://localhost:5000` and automatically create a SQLite database with seed data.

### Running the Frontend

```bash
# From the repository root
cd frontend
npm install  # First time only
npm run dev
```

The frontend will start on `http://localhost:3000`.

## API Endpoints

- `GET /health` - Health check
- `GET /dishes` - Get all dishes
- `POST /dishes/import` - Import dishes from JSON
- `POST /weekly-plans/generate` - Generate a weekly plan
- `GET /weekly-plans?weekStart=YYYY-MM-DD` - Get existing plan

## Frontend Pages

- `/` - Landing page with API health check
- `/dishes` - View and import dishes
- `/weekly-plan` - Generate and view weekly plans

## Development Notes

- The API uses CORS to allow requests from `http://localhost:3000` and `http://localhost:3001`
- Database file (`middagsklok.db`) is created in the API project directory
- Frontend uses `.env.local` for configuration (not committed to Git)

## Sample Data

Use `sample-dishes.json` to import example dishes:

1. Go to http://localhost:3000/dishes
2. Click "Choose File" and select `sample-dishes.json` from the repository root
3. Dishes will be imported and displayed

To generate a weekly plan, you need at least 7 different dishes imported.