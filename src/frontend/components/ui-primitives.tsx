import { cn } from '@/lib/utils';

interface PageTitleProps {
  children: React.ReactNode;
  className?: string;
}

export function PageTitle({ children, className }: PageTitleProps) {
  return (
    <h1 className={cn("text-3xl font-bold tracking-tight", className)}>
      {children}
    </h1>
  );
}

interface SectionCardProps {
  children: React.ReactNode;
  title?: string;
  description?: string;
  className?: string;
}

export function SectionCard({ children, title, description, className }: SectionCardProps) {
  return (
    <div className={cn("rounded-lg border bg-card text-card-foreground shadow-sm", className)}>
      {(title || description) && (
        <div className="flex flex-col space-y-1.5 p-6 pb-4">
          {title && <h3 className="text-lg font-semibold leading-none tracking-tight">{title}</h3>}
          {description && <p className="text-sm text-muted-foreground">{description}</p>}
        </div>
      )}
      <div className={cn("p-6", title || description ? "pt-0" : "")}>
        {children}
      </div>
    </div>
  );
}

interface FormRowProps {
  label: string;
  children: React.ReactNode;
  required?: boolean;
  className?: string;
}

export function FormRow({ label, children, required, className }: FormRowProps) {
  return (
    <div className={cn("space-y-2", className)}>
      <label className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
        {label}
        {required && <span className="text-destructive ml-1">*</span>}
      </label>
      {children}
    </div>
  );
}

interface EmptyStateProps {
  title: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ReactNode;
}

export function EmptyState({ title, description, action, icon }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      {icon && <div className="mb-4 text-muted-foreground">{icon}</div>}
      <h3 className="text-lg font-semibold">{title}</h3>
      {description && (
        <p className="mt-2 text-sm text-muted-foreground max-w-md">
          {description}
        </p>
      )}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
