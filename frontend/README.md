# Middagsklok Frontend

Next.js frontend for the Middagsklok meal planning application.

## Prerequisites

You need a running Middagsklok API server. The API is developed separately and should expose the following endpoints:

- `GET /health` - Health check
- `GET /dishes` - List all dishes
- `POST /dishes/import` - Import dishes from JSON
- `POST /weekly-plans/generate` - Generate weekly plan
- `GET /weekly-plans?weekStart=YYYY-MM-DD` - Get existing plan

## Configuration

Create a `.env.local` file in this directory:

```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

Update the URL to match your API server.

## Getting Started

```bash
npm install  # First time only
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser.

## Pages

- `/` - Landing page with API connection status
- `/dishes` - View and import dishes
- `/weekly-plan` - Generate and view weekly meal plans

## Tech Stack

- Next.js 16 with App Router
- TypeScript
- React 19
- Native fetch API (no additional data libraries)
- Inline styles (no styling framework)
