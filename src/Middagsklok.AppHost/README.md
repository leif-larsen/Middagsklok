# Middagsklok.AppHost

This project uses .NET Aspire to orchestrate the Middagsklok API and Next.js frontend for local development.

## Prerequisites

- .NET 10.0 SDK or later
- Node.js 20+ and npm
- .NET Aspire workload (optional, now uses NuGet packages)

## What Does This Do?

The AppHost orchestrates two services:

1. **API** (`Middagsklok.Api`) - The .NET backend API on port 5000
2. **Frontend** (`frontend`) - The Next.js application on port 3000

Both services are automatically configured with:
- Service discovery (frontend knows API URL)
- Dynamic CORS (API allows requests from frontend)
- SQLite database (file-based, local to API)

## Running Locally

From the AppHost directory:

```bash
cd src/Middagsklok.AppHost
dotnet run
```

Or from Visual Studio / VS Code, set `Middagsklok.AppHost` as the startup project.

This will:
1. Start the Aspire Dashboard (usually at http://localhost:15070)
2. Start the Middagsklok.Api project
3. Start the Next.js frontend via `npm run dev`
4. Configure environment variables for service discovery

## Aspire Dashboard

Once running, open the Aspire Dashboard URL shown in the console (typically http://localhost:15070).

The dashboard shows:
- Service status (running/stopped)
- Logs from both services
- Metrics and traces
- Environment variables
- Endpoints

## Troubleshooting

### DCP Not Available

If you see errors about DCP (Distributed Container Platform) not being available:

- Ensure you're running on Windows, macOS, or Linux with proper Aspire support
- The AppHost requires local orchestration capabilities
- This is a local development tool only, not for CI/CD environments

### Frontend Not Starting

Ensure npm dependencies are installed:

```bash
cd src/frontend
npm install
```

### API Database Issues

The API uses SQLite with a file-based database (`middagsklok.db`). This file is created automatically in the API's working directory.

## Architecture

```
AppHost
  ├── API (Project Reference)
  │   ├── Port: 5000
  │   ├── CORS: Configured for frontend
  │   └── Database: SQLite (local file)
  └── Frontend (npm app)
      ├── Port: 3000
      ├── Script: npm run dev
      └── Environment: NEXT_PUBLIC_API_BASE_URL from API
```

## Environment Variables

The AppHost automatically configures:

**For the API:**
- `AllowedOrigins__0` - Frontend URL for CORS

**For the Frontend:**
- `NEXT_PUBLIC_API_BASE_URL` - API base URL
- `PORT` - Port to run on (3000)

## Out of Scope

This AppHost is for **local development only**:
- ❌ Not for production deployment
- ❌ No container orchestration
- ❌ No cloud resources
- ❌ No secrets management

For production, deploy API and frontend separately according to your deployment strategy.
