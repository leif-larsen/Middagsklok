# Middagsklok

Weekly meal planning application.

## Project Structure

- `src/Middagsklok` - Core business logic library
- `src/Middagsklok.Api` - ASP.NET Core Web API
- `src/Middagsklok.AppHost` - .NET Aspire orchestration for local development
- `src/frontend/` - Next.js frontend application (developer UI)
- `tests/` - Unit and integration tests

## Getting Started

### Option 1: Using .NET Aspire (Recommended for Local Development)

The easiest way to run both the API and frontend together:

```bash
cd src/Middagsklok.AppHost
dotnet run
```

This starts:
- The Middagsklok API on http://localhost:5000
- The Next.js frontend on http://localhost:3000
- The Aspire Dashboard (typically at http://localhost:15070)

All services are automatically configured with service discovery and proper environment variables.

See [src/Middagsklok.AppHost/README.md](src/Middagsklok.AppHost/README.md) for more details.

### Option 2: Manual Setup

If you prefer to run services separately:

#### Running the API

```bash
cd src/Middagsklok.Api
dotnet run
```

The API will start on http://localhost:5000.

#### Running the Frontend

The frontend is a barebone Next.js application for local development and testing.

##### Prerequisites

- Node.js 20+ and npm
- A running Middagsklok API server

##### Configuration

Create `src/frontend/.env.local`:

```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

Update the URL to match your API server location.

##### Starting the Frontend

```bash
cd src/frontend
npm install  # First time only
npm run dev
```

The frontend will start on `http://localhost:3000`.

## Frontend Features

- **Landing page (`/`)** - API health check and connection status
- **Dishes page (`/dishes`)** - View all dishes, import dishes from JSON file
- **Weekly Plan page (`/weekly-plan`)** - Generate and view weekly meal plans

## Sample Data

Use `sample-dishes.json` to import example dishes through the frontend.