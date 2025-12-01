import { cn } from '@utils/cn';

interface BadgeProps {
  children: React.ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info';
  size?: 'sm' | 'md';
  className?: string;
}

const variants = {
  default: 'bg-glass-medium text-text-secondary',
  success: 'bg-semantic-success/20 text-semantic-success',
  warning: 'bg-semantic-warning/20 text-semantic-warning',
  error: 'bg-semantic-error/20 text-semantic-error',
  info: 'bg-semantic-info/20 text-semantic-info',
};

const sizes = {
  sm: 'px-2 py-0.5 text-xs',
  md: 'px-2.5 py-1 text-sm',
};

export function Badge({ children, variant = 'default', size = 'sm', className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center font-medium rounded-full',
        variants[variant],
        sizes[size],
        className
      )}
    >
      {children}
    </span>
  );
}
