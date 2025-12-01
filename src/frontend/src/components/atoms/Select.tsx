import { forwardRef } from 'react';
import { ChevronDown } from 'lucide-react';
import { cn } from '@utils/cn';

interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'children'> {
  label?: string;
  error?: string;
  options: SelectOption[];
  placeholder?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, options, placeholder, className, ...props }, ref) => {
    return (
      <div className="w-full">
        {label && (
          <label className="block text-sm font-medium text-text-secondary mb-1.5">
            {label}
          </label>
        )}
        <div className="relative">
          <select
            ref={ref}
            className={cn(
              'w-full px-3 py-2 pr-10 rounded-lg appearance-none',
              'bg-glass-medium border border-glass-border',
              'text-text-primary',
              'focus:outline-none focus:ring-2 focus:ring-accent-purple/50 focus:border-accent-purple',
              'transition-colors cursor-pointer',
              error && 'border-semantic-error',
              className
            )}
            {...props}
          >
            {placeholder && (
              <option value="" disabled>
                {placeholder}
              </option>
            )}
            {options.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-tertiary pointer-events-none" />
        </div>
        {error && <p className="mt-1 text-sm text-semantic-error">{error}</p>}
      </div>
    );
  }
);

Select.displayName = 'Select';
