# Middagsklok

Weekly meal planning application.

## Project Structure

- `src/Middagsklok` - Core business logic library
- `src/frontend/` - Next.js frontend application (developer UI)
- `tests/` - Unit and integration tests

## Frontend Setup

The frontend is a barebone Next.js application for local development and testing.

### Prerequisites

- Node.js 20+ and npm
- A running Middagsklok API server

### Configuration

Create `src/frontend/.env.local`:

```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

Update the URL to match your API server location.

### Running the Frontend

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