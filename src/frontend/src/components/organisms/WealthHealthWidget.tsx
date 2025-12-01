import { useState, useEffect } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { 
  PiggyBank, 
  Shield, 
  Layers, 
  ArrowUpRight,
  Info
} from 'lucide-react';
import { cn } from '@utils/cn';

interface WealthHealthData {
  overallScore: number;
  components: {
    savingsRate: number;
    debtToIncome: number;
    emergencyFund: number;
    diversification: number;
    netWorthGrowth: number;
  };
  details: {
    savingsRate?: number;
    debtToIncome?: number;
    emergencyFundMonths?: number;
    accountTypeCount?: number;
    netWorthGrowthPercent?: number;
  };
}

function getScoreColor(score: number): string {
  if (score >= 80) return 'text-semantic-success';
  if (score >= 60) return 'text-accent-purple';
  if (score >= 40) return 'text-yellow-500';
  return 'text-semantic-error';
}

function getScoreBgColor(score: number): string {
  if (score >= 80) return 'bg-semantic-success/20';
  if (score >= 60) return 'bg-accent-purple/20';
  if (score >= 40) return 'bg-yellow-500/20';
  return 'bg-semantic-error/20';
}

interface ComponentRowProps {
  icon: React.ReactNode;
  label: string;
  score: number;
  detail: string;
}

function ComponentRow({ icon, label, score, detail }: ComponentRowProps) {
  return (
    <div className="flex items-center justify-between py-2">
      <div className="flex items-center gap-3">
        <div className={cn('p-2 rounded-lg', getScoreBgColor(score))}>
          {icon}
        </div>
        <div>
          <span className="text-text-primary text-sm font-medium">{label}</span>
          <p className="text-text-tertiary text-xs">{detail}</p>
        </div>
      </div>
      <span className={cn('text-lg font-bold', getScoreColor(score))}>
        {Math.round(score)}
      </span>
    </div>
  );
}

export function WealthHealthWidget() {
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<WealthHealthData | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchWealthHealth = async () => {
      try {
        const token = localStorage.getItem('accessToken');
        const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};

        const response = await fetch('/api/dashboard/wealth-health', { headers });
        if (response.ok) {
          const result = await response.json();
          setData(result.data);
        } else {
          setError('Failed to load wealth health data');
        }
      } catch (err) {
        setError('Failed to load wealth health data');
        console.error('Failed to fetch wealth health:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchWealthHealth();
  }, []);

  if (loading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-center h-48">
          <Spinner size="md" />
        </div>
      </GlassCard>
    );
  }

  if (error || !data) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <Shield className="w-5 h-5 text-accent-purple" />
          <h2 className="text-lg font-semibold text-text-primary">Wealth Health</h2>
        </div>
        <p className="text-text-tertiary text-sm text-center py-8">
          {error || 'No data available'}
        </p>
      </GlassCard>
    );
  }

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Shield className="w-5 h-5 text-accent-purple" />
          <h2 className="text-lg font-semibold text-text-primary">Wealth Health</h2>
        </div>
        <div className="group relative">
          <Info className="w-4 h-4 text-text-tertiary cursor-help" />
          <div className="absolute right-0 top-6 hidden group-hover:block bg-background-card p-3 rounded-lg shadow-lg z-10 w-64">
            <p className="text-xs text-text-secondary">
              A composite score based on savings rate, debt management, emergency fund, 
              portfolio diversification, and net worth growth.
            </p>
          </div>
        </div>
      </div>

      {/* Overall Score */}
      <div className="text-center mb-6">
        <div className={cn('text-5xl font-bold', getScoreColor(data.overallScore))}>
          {Math.round(data.overallScore)}
        </div>
        <p className="text-text-tertiary text-sm mt-1">Overall Score</p>
      </div>

      {/* Component Breakdown */}
      <div className="space-y-1 divide-y divide-border-subtle">
        <ComponentRow 
          icon={<PiggyBank className={cn('w-4 h-4', getScoreColor(data.components.savingsRate))} />}
          label="Savings Rate"
          score={data.components.savingsRate}
          detail={`${data.details.savingsRate ?? 0}% of income`}
        />
        <ComponentRow 
          icon={<Shield className={cn('w-4 h-4', getScoreColor(data.components.debtToIncome))} />}
          label="Debt-to-Income"
          score={data.components.debtToIncome}
          detail={`${data.details.debtToIncome ?? 0}% ratio`}
        />
        <ComponentRow 
          icon={<Shield className={cn('w-4 h-4', getScoreColor(data.components.emergencyFund))} />}
          label="Emergency Fund"
          score={data.components.emergencyFund}
          detail={`${data.details.emergencyFundMonths ?? 0} months covered`}
        />
        <ComponentRow 
          icon={<Layers className={cn('w-4 h-4', getScoreColor(data.components.diversification))} />}
          label="Diversification"
          score={data.components.diversification}
          detail={`${data.details.accountTypeCount ?? 0} account types`}
        />
        <ComponentRow 
          icon={<ArrowUpRight className={cn('w-4 h-4', getScoreColor(data.components.netWorthGrowth))} />}
          label="Net Worth Growth"
          score={data.components.netWorthGrowth}
          detail={`${data.details.netWorthGrowthPercent ?? 0}% YoY`}
        />
      </div>
    </GlassCard>
  );
}
