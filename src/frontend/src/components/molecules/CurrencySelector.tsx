import { useDispatch, useSelector } from 'react-redux';
import { ChevronDown } from 'lucide-react';
import { cn } from '@utils/cn';
import type { RootState } from '@store/index';
import { setCurrency } from '@store/slices/uiSlice';
import { CURRENCIES } from '@/types';

interface CurrencySelectorProps {
  className?: string;
  size?: 'sm' | 'md';
}

export function CurrencySelector({ className, size = 'md' }: CurrencySelectorProps) {
  const dispatch = useDispatch();
  const currency = useSelector((state: RootState) => state.ui.currency);

  const sizeStyles = {
    sm: 'px-2 py-1 text-sm',
    md: 'px-3 py-2',
  };

  return (
    <div className={cn('relative', className)}>
      <select
        value={currency}
        onChange={(e) => dispatch(setCurrency(e.target.value))}
        className={cn(
          'appearance-none bg-glass-medium border border-glass-border rounded-lg',
          'text-text-primary cursor-pointer pr-8',
          'focus:outline-none focus:ring-2 focus:ring-accent-purple/50',
          'transition-colors',
          sizeStyles[size]
        )}
      >
        {CURRENCIES.map((c) => (
          <option key={c.value} value={c.value}>
            {c.symbol} {c.value}
          </option>
        ))}
      </select>
      <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 w-4 h-4 text-text-tertiary pointer-events-none" />
    </div>
  );
}

export function formatCurrency(value: number, currency: string = 'ZAR'): string {
  const curr = CURRENCIES.find((c) => c.value === currency);
  const symbol = curr?.symbol || currency;

  if (currency === 'BTC') {
    return `${symbol}${value.toFixed(8)}`;
  }

  return `${symbol} ${value.toLocaleString(undefined, {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  })}`;
}
