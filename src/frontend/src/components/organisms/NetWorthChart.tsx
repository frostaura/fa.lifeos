import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';
import { GlassCard } from '@components/atoms/GlassCard';
import { cn } from '@utils/cn';
import type { NetWorthDataPoint } from '@/types';
import { formatCurrency } from '@components/molecules/CurrencySelector';

interface NetWorthChartProps {
  data: NetWorthDataPoint[];
  currency?: string;
  showCard?: boolean;
  height?: number;
  className?: string;
}

interface TooltipPayloadItem {
  value: number;
  dataKey: string;
}

interface CustomTooltipProps {
  active?: boolean;
  payload?: TooltipPayloadItem[];
  label?: string;
  currency: string;
}

function CustomTooltip({ active, payload, label, currency }: CustomTooltipProps) {
  if (!active || !payload?.length) return null;

  return (
    <div className="bg-background-tertiary border border-glass-border rounded-lg p-3 shadow-lg">
      <p className="text-text-tertiary text-xs mb-1">{label}</p>
      <p className="text-text-primary font-semibold">
        {formatCurrency(payload[0].value, currency)}
      </p>
    </div>
  );
}

export function NetWorthChart({
  data,
  currency = 'ZAR',
  showCard = false,
  height = 300,
  className,
}: NetWorthChartProps) {
  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-ZA', { month: 'short', year: '2-digit' });
  };

  const chartContent = (
    <ResponsiveContainer width="100%" height={height}>
      <AreaChart data={data} margin={{ top: 10, right: 10, left: 10, bottom: 0 }}>
        <defs>
          <linearGradient id="netWorthGradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.4} />
            <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
        <XAxis
          dataKey="date"
          tickFormatter={formatDate}
          stroke="#6b6b7a"
          tick={{ fill: '#6b6b7a', fontSize: 12 }}
          tickLine={{ stroke: '#6b6b7a' }}
        />
        <YAxis
          stroke="#6b6b7a"
          tick={{ fill: '#6b6b7a', fontSize: 12 }}
          tickFormatter={(value) => {
            if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
            if (value >= 1000) return `${(value / 1000).toFixed(0)}K`;
            return value.toString();
          }}
          tickLine={{ stroke: '#6b6b7a' }}
        />
        <Tooltip content={<CustomTooltip currency={currency} />} />
        <Area
          type="monotone"
          dataKey="value"
          stroke="#8b5cf6"
          strokeWidth={2}
          fill="url(#netWorthGradient)"
        />
      </AreaChart>
    </ResponsiveContainer>
  );

  if (!showCard) {
    return <div className={className}>{chartContent}</div>;
  }

  return (
    <GlassCard variant="default" className={cn('p-6', className)}>
      <h3 className="text-lg font-semibold text-text-primary mb-4">Net Worth History</h3>
      {chartContent}
    </GlassCard>
  );
}
