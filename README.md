# Middagsklok

Middagsklok is a personal meal-planning app for organizing dishes, generating weekly plans, and creating shopping lists.

## Tech stack

- Backend: ASP.NET Core Minimal APIs (.NET 10), EF Core, PostgreSQL
- Frontend: Next.js (App Router, TypeScript)
- Local orchestration: .NET Aspire (`Middagsklok.AppHost`)

## What it does

- Manage dishes and ingredients
- Generate and update weekly meal plans
- Mark planned meals as eaten
- Build shopping lists from a selected week
- Configure planning settings

## Run locally

From the repository root:

```bash
dotnet run --project src/Middagsklok.AppHost
```

This starts the full local environment (API, frontend, and PostgreSQL).

## Project structure

- `src/Middagsklok.Api` - Backend API and domain logic
- `src/Frontend/middagsklok` - Next.js frontend
- `src/Middagsklok.AppHost` - Aspire app host for local development
- `tests/Middagsklok.Tests` - Unit tests
