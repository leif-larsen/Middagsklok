# API Contracts

This directory contains the shared API contract between the Middagsklok backend and frontend.

## Files

- `openapi.json` - OpenAPI 3.0 specification for the Middagsklok API (auto-generated, committed to git)

## Regenerating the OpenAPI Specification

To regenerate the `openapi.json` file after making API changes:

### Prerequisites

- .NET 10.0 SDK installed
- Solution built successfully

### Steps

1. Build the solution:
   ```bash
   dotnet build
   ```

2. Start the API server:
   ```bash
   cd src/Middagsklok.Api
   dotnet run --no-build
   ```

3. In a separate terminal, fetch the OpenAPI spec:
   ```bash
   curl http://localhost:5000/swagger/v1/swagger.json > contracts/openapi.json
   ```

4. Stop the API server (Ctrl+C)

5. Commit the updated `openapi.json`:
   ```bash
   git add contracts/openapi.json
   git commit -m "chore: update OpenAPI spec"
   ```

### Alternative: Using the provided script

A convenience script is provided:

```bash
./contracts/regenerate-openapi.sh
```

This script will:
1. Build the solution
2. Start the API
3. Download the OpenAPI spec
4. Stop the API

## Using the Contract

### Backend

The backend uses DTOs from the `Middagsklok.Contracts` project for all API endpoints.

### Frontend

The frontend should use the `openapi.json` as the source of truth for:
- Request/response payload shapes
- Available endpoints
- Data types and validation rules

You can use code generation tools like:
- `openapi-typescript` for TypeScript type generation
- `openapi-generator` for full client generation

## Contract Principles

1. **DTOs only** - No domain entities or EF types exposed
2. **String IDs** - GUIDs are serialized as strings
3. **ISO dates** - Dates use `YYYY-MM-DD` format
4. **No leakage** - Domain logic stays in the backend
