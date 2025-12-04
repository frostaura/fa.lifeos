import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  ReferenceDot,
} from 'recharts';
import { GlassCard } from '@components/atoms/GlassCard';
import { cn } from '@utils/cn';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import type { ProjectionDataPoint, MilestoneResult, ProjectionAccountBalance } from '@/types';

interface ProjectionChartProps {
  data: ProjectionDataPoint[];
  milestones?: MilestoneResult[];
  currency?: string;
  showCard?: boolean;
  height?: number;
  className?: string;
}

interface TooltipPayloadItem {
  value: number;
  dataKey: string;
  name: string;
  color: string;
  payload?: ProjectionDataPoint;
}

interface CustomTooltipProps {
  active?: boolean;
  payload?: TooltipPayloadItem[];
  label?: string;
  currency: string;
}

function CustomTooltip({ active, payload, label, currency }: CustomTooltipProps) {
  if (!active || !payload?.length) return null;

  // Get accounts from the first payload item (all items share the same data point)
  const accounts = payload[0]?.payload?.accounts;

  return (
    <div className="bg-background-tertiary border border-glass-border rounded-lg p-3 shadow-lg max-w-sm">
      <p className="text-text-tertiary text-xs mb-2">{label}</p>
      {payload.map((p, i) => (
        <div key={i} className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full" style={{ backgroundColor: p.color }} />
          <span className="text-text-secondary text-sm">{p.name}:</span>
          <span className="text-text-primary font-medium text-sm">
            {formatCurrency(p.value, currency)}
          </span>
        </div>
      ))}
      
      {/* Account Balances Table */}
      {accounts && accounts.length > 0 && (
        <div className="mt-3 pt-3 border-t border-glass-border">
          <p className="text-text-tertiary text-xs mb-2 font-medium">Account Balances</p>
          <div className="space-y-1 max-h-48 overflow-y-auto">
            {accounts
              .filter((a: ProjectionAccountBalance) => a.balance !== 0)
              .sort((a: ProjectionAccountBalance, b: ProjectionAccountBalance) => Math.abs(b.balance) - Math.abs(a.balance))
              .map((account: ProjectionAccountBalance) => (
                <div key={account.accountId} className="flex items-center justify-between gap-4 text-xs">
                  <span className="text-text-secondary truncate max-w-[140px]" title={account.accountName}>
                    {account.accountName}
                  </span>
                  <span className={cn(
                    'font-medium whitespace-nowrap',
                    account.balance >= 0 ? 'text-semantic-success' : 'text-semantic-error'
                  )}>
                    {formatCurrency(account.balance, currency)}
                  </span>
                </div>
              ))}
          </div>
        </div>
      )}
    </div>
  );
}

export function ProjectionChart({
  data,
  milestones = [],
  currency = 'ZAR',
  showCard = false,
  height = 350,
  className,
}: ProjectionChartProps) {
  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-ZA', { month: 'short', year: '2-digit' });
  };

  // Find milestone data points
  const milestonePoints = milestones
    .filter((m) => m.achievedDate)
    .map((m) => {
      const dataPoint = data.find((d) => d.date === m.achievedDate);
      return dataPoint ? { ...m, ...dataPoint } : null;
    })
    .filter(Boolean);

  const chartContent = (
    <ResponsiveContainer width="100%" height={height}>
      <LineChart data={data} margin={{ top: 20, right: 30, left: 10, bottom: 0 }}>
        <defs>
          <linearGradient id="projectionGradient" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor="#8b5cf6" />
            <stop offset="100%" stopColor="#22d3ee" />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
        <XAxis
          dataKey="date"
          tickFormatter={formatDate}
          stroke="#6b6b7a"
          tick={{ fill: '#6b6b7a', fontSize: 12 }}
        />
        <YAxis
          stroke="#6b6b7a"
          tick={{ fill: '#6b6b7a', fontSize: 12 }}
          tickFormatter={(value) => {
            if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
            if (value >= 1000) return `${(value / 1000).toFixed(0)}K`;
            return value.toString();
          }}
        />
        <Tooltip content={<CustomTooltip currency={currency} />} />
        <Line
          type="monotone"
          dataKey="netWorth"
          name="Net Worth"
          stroke="url(#projectionGradient)"
          strokeWidth={3}
          dot={false}
          activeDot={{ r: 6, fill: '#8b5cf6' }}
        />
        <Line
          type="monotone"
          dataKey="income"
          name="Income"
          stroke="#22c55e"
          strokeWidth={2}
          strokeDasharray="5 5"
          dot={false}
        />
        <Line
          type="monotone"
          dataKey="expenses"
          name="Expenses"
          stroke="#ef4444"
          strokeWidth={2}
          strokeDasharray="5 5"
          dot={false}
        />
        {/* Milestone markers */}
        {milestonePoints.map((milestone, i) => (
          <ReferenceDot
            key={i}
            x={milestone!.date}
            y={milestone!.netWorth}
            r={8}
            fill="#eab308"
            stroke="#eab308"
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  );

  if (!showCard) {
    return <div className={className}>{chartContent}</div>;
  }

  return (
    <GlassCard variant="default" className={cn('p-6', className)}>
      <h3 className="text-lg font-semibold text-text-primary mb-4">Financial Projection</h3>
      {chartContent}
      {milestones.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-4">
          {milestones.map((m, i) => (
            <div key={i} className="flex items-center gap-2">
              <div className="w-2 h-2 rounded-full bg-semantic-warning" />
              <span className="text-sm text-text-secondary">
                {m.label}: {m.achievedDate ? formatDate(m.achievedDate) : 'Pending'}
              </span>
            </div>
          ))}
        </div>
      )}
    </GlassCard>
  );
}
