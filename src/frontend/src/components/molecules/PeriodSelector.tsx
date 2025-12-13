import { cn } from '@utils/cn';
import type { ChartPeriod } from '@/hooks/useChartPeriod';

interface PeriodSelectorProps {
  value: ChartPeriod;
  onChange: (period: ChartPeriod) => void;
  className?: string;
}

const PERIODS: ChartPeriod[] = ['1M', '3M', '6M', '1Y', '5Y', '10Y', 'ALL'];

export function PeriodSelector({ value, onChange, className }: PeriodSelectorProps) {
  return (
    <div className={cn('flex gap-1 flex-wrap', className)}>
      {PERIODS.map((period) => (
        <button
          key={period}
          onClick={() => onChange(period)}
          className={cn(
            'px-3 py-1.5 rounded-lg text-sm font-medium transition-all',
            value === period
              ? 'bg-accent-purple text-white shadow-glow-sm'
              : 'bg-background-secondary text-text-secondary hover:bg-background-tertiary hover:text-text-primary'
          )}
        >
          {period}
        </button>
      ))}
    </div>
  );
}
