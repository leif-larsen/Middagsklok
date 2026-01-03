```instructions
---
applyTo: "src/client/**/components/**/*.tsx"
---

# UI & Styling Guidelines

## Component Library

- **shadcn/ui** (new-york style) — copy components, don't install as dependency
- **Radix UI** primitives for accessibility
- **Lucide React** for icons
- **Sonner** for toast notifications

## Adding shadcn/ui Components

```bash
npx shadcn@latest add button
npx shadcn@latest add dialog
```

Components are copied to `src/components/ui/` — customize freely.

## Tailwind CSS

- **Tailwind v4** with CSS variables for theming
- Use utility classes, avoid custom CSS
- Responsive: mobile-first (`sm:`, `md:`, `lg:`)

```tsx
// Use cn() for conditional classes
import { cn } from '@/lib/utils'

<div className={cn(
    "flex items-center gap-2",
    isActive && "bg-primary text-primary-foreground"
)} />
```

## Color Tokens

Use semantic color tokens, not raw colors:

| Token | Usage |
|-------|-------|
| `bg-background` | Page background |
| `bg-card` | Card surfaces |
| `text-foreground` | Primary text |
| `text-muted-foreground` | Secondary text |
| `bg-primary` | Primary actions |
| `text-destructive` | Error states |
| `border` | Default borders |

## Spacing & Layout

```tsx
// Consistent spacing scale
<div className="p-4 sm:p-6 lg:p-8">        // Responsive padding
<div className="space-y-4">                 // Vertical stack
<div className="flex items-center gap-2">   // Horizontal with gap
<div className="grid grid-cols-2 gap-4">    // Grid layout
```

## Icons

```tsx
import { Calendar, User, X } from 'lucide-react'

// Standard sizes
<Calendar className="h-4 w-4" />      // Small (inline)
<Calendar className="h-5 w-5" />      // Medium (buttons)
<Calendar className="h-6 w-6" />      // Large (headers)
```

## Buttons

```tsx
import { Button } from '@/components/ui/button'

<Button>Primary Action</Button>
<Button variant="outline">Secondary</Button>
<Button variant="ghost">Tertiary</Button>
<Button variant="destructive">Danger</Button>
<Button size="sm">Small</Button>
<Button disabled={isLoading}>
    {isLoading ? 'Laster...' : 'Send'}
</Button>
```

## Dialogs & Modals

```tsx
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog'

<Dialog open={open} onOpenChange={setOpen}>
    <DialogContent>
        <DialogHeader>
            <DialogTitle>Tittel</DialogTitle>
            <DialogDescription>Beskrivelse</DialogDescription>
        </DialogHeader>
        {/* Content */}
        <DialogFooter>
            <Button variant="outline" onClick={() => setOpen(false)}>
                Avbryt
            </Button>
            <Button onClick={handleConfirm}>Bekreft</Button>
        </DialogFooter>
    </DialogContent>
</Dialog>
```

## Toast Notifications

```tsx
import { toast } from 'sonner'

toast.success('Reservasjon opprettet!')
toast.error('Kunne ikke lagre endringer')
toast.info('Oppdaterer...')
```

## Loading States

```tsx
// Use skeleton components for content loading
import { Skeleton } from '@/components/ui/skeleton'

{isLoading ? (
    <Skeleton className="h-12 w-full" />
) : (
    <ActualContent />
)}

// Use disabled + text change for button loading
<Button disabled={isLoading}>
    {isLoading ? 'Reserverer...' : 'Reserver'}
</Button>
```

## Accessibility

- All interactive elements must be keyboard accessible
- Use semantic HTML (`button`, `nav`, `main`, `section`)
- Include `aria-label` for icon-only buttons
- Radix primitives handle most a11y automatically

```tsx
<Button aria-label="Lukk dialog">
    <X className="h-4 w-4" />
</Button>
```

```