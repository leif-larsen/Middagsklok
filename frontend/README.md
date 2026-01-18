# Middagsklok Frontend

Next.js frontend for the Middagsklok meal planning application.

## Getting Started

Make sure the API is running first (see main README).

Then start the development server:

```bash
npm install  # First time only
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser.

## Configuration

The frontend uses environment variables for configuration. Create a `.env.local` file:

```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

## Pages

- `/` - Landing page with API connection status
- `/dishes` - View and import dishes
- `/weekly-plan` - Generate and view weekly meal plans

## Tech Stack

- Next.js 16 with App Router
- TypeScript
- React 19
- Native fetch API (no additional data libraries)
