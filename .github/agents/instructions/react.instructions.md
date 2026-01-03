```instructions
---
applyTo: "src/client/**/*.tsx,src/client/**/*.ts"
---

# React & TypeScript Standards

## Project Setup

- **React 19** with functional components only
- **TypeScript** with strict mode
- **Vite** for dev server and bundling
- Path aliases: `@/components`, `@/lib`, `@/services`, `@/types`, `@/pages`

## Component Patterns

```tsx
// Prefer function declarations for components
export function MyComponent({ prop }: Props) {
    const [state, setState] = useState<Type>(initialValue)
    
    // Effects after state
    useEffect(() => { ... }, [deps])
    
    // Handlers before return
    const handleClick = () => { ... }
    
    return <div>...</div>
}
```

## Hooks

- Use `useState` for local component state
- Use `useEffect` for side effects (API calls, subscriptions)
- Use `useCallback` for stable function references in deps
- Use `useMemo` for expensive computations

## State Management

- Keep state close to where it's used
- Lift state up when needed for sibling communication
- No global state library — use props and callbacks

## API Calls

```tsx
// Use the api service, handle errors with ApiError
import { api, ApiError } from '@/services/api'

const [data, setData] = useState<Type[]>([])
const [isLoading, setIsLoading] = useState(true)
const [error, setError] = useState<string | null>(null)

useEffect(() => {
    api.getData()
        .then(setData)
        .catch((err) => {
            const message = err instanceof ApiError ? err.message : 'Feil ved lasting'
            setError(message)
        })
        .finally(() => setIsLoading(false))
}, [])
```

## Error Handling

- Use `ApiError` class for API errors (check `isConflict`, `isNotFound`, `isUnauthorized`)
- Show user-friendly Norwegian error messages
- Use `toast.error()` from Sonner for transient errors
- Display inline errors for form validation

## Types

- Define types in `@/types/index.ts`
- Use `interface` for object shapes
- Export all types from the index file
- Match API response shapes from backend DTOs

```typescript
export interface Booking {
    id: number
    deskId: number
    date: string      // ISO date: YYYY-MM-DD
    startTime: string // HH:mm:ss
    endTime: string   // HH:mm:ss
}
```

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Components | PascalCase | `BookingModal`, `WeekView` |
| Hooks | camelCase with `use` prefix | `useState`, `useCallback` |
| Handlers | `handle` + action | `handleClick`, `handleSubmit` |
| Boolean state | `is`/`has` prefix | `isLoading`, `hasError` |
| API functions | verb + noun | `getDesks`, `createBooking` |

## File Organization

```
src/
├── components/     # Reusable components
│   └── ui/         # shadcn/ui primitives
├── pages/          # Page-level components
├── services/       # API and external services
├── types/          # TypeScript interfaces
└── lib/            # Utilities (cn, helpers)
```

## Localization

- All user-facing text in **Norwegian (Bokmål)**
- Use `nb-NO` locale for date formatting
- Error messages should be helpful and friendly

```tsx
date.toLocaleDateString('nb-NO', { weekday: 'long', month: 'long', day: 'numeric' })
```

```