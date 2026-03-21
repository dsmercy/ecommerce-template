import { PackageSearch } from 'lucide-react';
import { Button } from '@components/ui/button';
import { cn } from '@lib/utils';

// ─── Props ────────────────────────────────────────────────────────────────────

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  className?: string;
}

// ─── EmptyState ───────────────────────────────────────────────────────────────

export function EmptyState({ icon, title, description, action, className }: EmptyStateProps) {
  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center gap-4 rounded-xl border border-dashed border-gray-200 bg-gray-50/50 px-6 py-16 text-center',
        className
      )}
    >
      <div className="flex h-14 w-14 items-center justify-center rounded-full bg-gray-100 text-gray-400">
        {icon ?? <PackageSearch className="h-7 w-7" aria-hidden="true" />}
      </div>

      <div className="space-y-1">
        <h3 className="text-base font-semibold text-gray-900">{title}</h3>
        {description && (
          <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
        )}
      </div>

      {action && (
        <Button onClick={action.onClick} variant="outline" size="sm" className="mt-2">
          {action.label}
        </Button>
      )}
    </div>
  );
}
