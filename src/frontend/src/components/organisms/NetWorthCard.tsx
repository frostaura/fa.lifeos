import { useSelector } from 'react-redux';
import { GlassCard } from '@components/atoms/GlassCard';
import { CurrencySelector, formatCurrency } from '@components/molecules/CurrencySelector';
import { ArrowUpRight, ArrowDownRight } from 'lucide-react';
import { cn } from '@utils/cn';
import type { RootState } from '@store/index';

interface NetWorthCardProps {
  value: number;
  change?: number;
  changePercent?: number;
  className?: string;
}

export function NetWorthCard({ value, change = 0, changePercent = 0, className }: NetWorthCardProps) {
  const currency = useSelector((state: RootState) => state.ui.currency);
  const isPositive = changePercent >= 0;
  const TrendIcon = isPositive ? ArrowUpRight : ArrowDownRight;
  const trendColor = isPositive ? 'text-semantic-success' : 'text-semantic-error';

  return (
    <GlassCard variant="elevated" className={cn('p-6', className)}>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-text-secondary">Net Worth</h2>
        <CurrencySelector size="sm" />
      </div>
      <div className="flex flex-col items-center justify-center min-h-[8rem]">
        <span className="text-4xl lg:text-5xl font-bold text-text-primary">
          {formatCurrency(value, currency)}
        </span>
        <div className={cn('flex items-center gap-1 mt-2', trendColor)}>
          <TrendIcon className="w-4 h-4" />
          <span className="font-medium">
            {isPositive ? '+' : ''}{changePercent.toFixed(1)}% YTD
          </span>
        </div>
        {change !== 0 && (
          <span className="text-text-tertiary text-sm mt-1">
            {isPositive ? '+' : ''}{formatCurrency(change, currency)}
          </span>
        )}
      </div>
    </GlassCard>
  );
}
