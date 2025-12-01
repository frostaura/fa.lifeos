import { forwardRef } from 'react';
import { cn } from '@utils/cn';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: React.ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, icon, className, ...props }, ref) => {
    return (
      <div className="w-full">
        {label && (
          <label className="block text-sm font-medium text-text-secondary mb-1.5">
            {label}
          </label>
        )}
        <div className="relative">
          {icon && (
            <div className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary">
              {icon}
            </div>
          )}
          <input
            ref={ref}
            className={cn(
              'w-full px-3 py-2 rounded-lg',
              'bg-glass-medium border border-glass-border',
              'text-text-primary placeholder-text-tertiary',
              'focus:outline-none focus:ring-2 focus:ring-accent-purple/50 focus:border-accent-purple',
              'transition-colors',
              icon ? 'pl-10' : '',
              error ? 'border-semantic-error focus:ring-semantic-error/50 focus:border-semantic-error' : '',
              className
            )}
            {...props}
          />
        </div>
        {error && <p className="mt-1 text-sm text-semantic-error">{error}</p>}
      </div>
    );
  }
);

Input.displayName = 'Input';
