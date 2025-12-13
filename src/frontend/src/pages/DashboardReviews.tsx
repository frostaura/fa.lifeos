import { useState } from 'react';
import {
  useGetWeeklyReviewQuery,
  useGetMonthlyReviewQuery,
  useGenerateReviewMutation,
} from '../services/endpoints';
import { GlassCard } from '@components/atoms/GlassCard';
import { 
  ArrowUp, ArrowDown, Flame, Target, RefreshCw, 
  TrendingUp, Wallet, Heart, Users, Briefcase, 
  Gamepad2, Home, Palette, Brain, Globe, DollarSign,
  BarChart3
} from 'lucide-react';
import { formatCurrency, formatPercentage } from '@utils/numberFormatter';

// Icon mapping for dimensions
const dimensionIcons: Record<string, React.FC<{ className?: string }>> = {
  health_recovery: Heart,
  relationships: Users,
  work_contribution: Briefcase,
  play_adventure: Gamepad2,
  asset_care: Home,
  create_craft: Palette,
  growth_mind: Brain,
  community_meaning: Globe,
};

function DeltaIndicator({ value, suffix = '' }: { value: number | null | undefined; suffix?: string }) {
  if (value === null || value === undefined || value === 0) {
    return <span className="text-text-tertiary text-xs">--</span>;
  }

  const isPositive = value > 0;
  return (
    <span className={`flex items-center gap-1 text-xs ${isPositive ? 'text-semantic-success' : 'text-semantic-error'}`}>
      {isPositive ? <ArrowUp className="w-3 h-3" /> : <ArrowDown className="w-3 h-3" />}
      {isPositive ? '+' : ''}{value.toFixed(1)}{suffix}
    </span>
  );
}

function ScoreCard({ 
  title, 
  currentValue,
  delta, 
  suffix = '',
  icon: Icon
}: { 
  title: string; 
  currentValue: number | null | undefined;
  delta: number | null | undefined;
  suffix?: string;
  icon?: React.FC<{ className?: string }>;
}) {
  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center gap-2 mb-3">
        {Icon && (
          <Icon className="w-5 h-5 text-purple-400" />
        )}
        <span className="text-text-secondary text-sm">{title}</span>
      </div>
      <div className="flex items-center justify-between mb-2">
        <span className="text-3xl font-bold text-text-primary">
          {currentValue !== null && currentValue !== undefined ? currentValue.toFixed(1) : '--'}{currentValue !== null && currentValue !== undefined && suffix}
        </span>
        <DeltaIndicator value={delta} suffix={suffix} />
      </div>
    </GlassCard>
  );
}

function StreakCard({ streak }: { streak: { taskId?: string; taskTitle: string; streakDays?: number; currentStreak?: number } }) {
  const days = streak.streakDays ?? streak.currentStreak ?? 0;
  return (
    <div className="group flex items-center justify-between bg-black/90 rounded-lg px-4 py-3 border border-white/10 hover:border-accent-purple/50 transition-all cursor-pointer hover:shadow-glow-sm">
      <div className="flex items-center gap-3">
        <Flame className="w-5 h-5 text-orange-400 group-hover:text-orange-300 transition-colors" />
        <span className="text-text-primary font-medium group-hover:text-text-primary transition-colors">{streak.taskTitle}</span>
      </div>
      <span className="text-orange-400 font-bold text-lg">{days} days</span>
    </div>
  );
}

function ActionCard({ action }: { action: { action: string; priority: string; dimension: string } }) {
  const priorityStyles = {
    high: 'text-semantic-error bg-semantic-error/10 border-semantic-error/30',
    medium: 'text-semantic-warning bg-semantic-warning/10 border-semantic-warning/30',
    low: 'text-semantic-success bg-semantic-success/10 border-semantic-success/30',
  };

  const style = priorityStyles[action.priority as keyof typeof priorityStyles] || priorityStyles.low;
  const Icon = dimensionIcons[action.dimension] || Target;

  return (
    <div className="group flex items-start gap-3 bg-black/90 rounded-lg px-4 py-3 border border-white/10 hover:border-accent-cyan/50 transition-all cursor-pointer hover:shadow-glow-sm">
      <Icon className="w-5 h-5 text-accent-cyan mt-0.5 flex-shrink-0" />
      <div className="flex-1 min-w-0">
        <p className="text-text-primary font-medium mb-1 group-hover:text-text-primary transition-colors">{action.action}</p>
        <div className="flex items-center gap-2 flex-wrap">
          <span className={`text-xs font-semibold px-2 py-0.5 rounded border ${style}`}>
            {action.priority.toUpperCase()}
          </span>
          <span className="text-xs text-text-secondary capitalize">{action.dimension.replace('_', ' ')}</span>
        </div>
      </div>
    </div>
  );
}

function FinancialSummaryCard({ data }: { data: any }) {
  if (!data) return null;
  
  return (
    <GlassCard variant="solid" className="p-6">
      <div className="flex items-center gap-3 mb-4">
        <Wallet className="w-5 h-5 text-amber-400" />
        <h3 className="text-lg font-semibold text-text-primary">Financial Summary</h3>
      </div>
      
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <div>
          <div className="text-sm text-text-secondary mb-1">Net Worth</div>
          <div className="text-lg font-bold text-text-primary break-words">
            {formatCurrency(data.netWorth)}
          </div>
          <DeltaIndicator value={data.netWorthDelta} />
        </div>
        <div>
          <div className="text-sm text-text-secondary mb-1">Income</div>
          <div className="text-lg font-semibold text-semantic-success">
            {formatCurrency(data.totalIncome)}
          </div>
        </div>
        <div>
          <div className="text-sm text-text-secondary mb-1">Expenses</div>
          <div className="text-lg font-semibold text-semantic-error">
            {formatCurrency(data.totalExpenses)}
          </div>
        </div>
        <div>
          <div className="text-sm text-text-secondary mb-1">Cash Flow</div>
          <div className={`text-lg font-semibold ${(data.netCashFlow ?? 0) >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
            {formatCurrency(data.netCashFlow)}
          </div>
        </div>
        <div>
          <div className="text-sm text-text-secondary mb-1">Savings Rate</div>
          <div className="text-lg font-semibold text-accent-cyan">
            {formatPercentage(data.savingsRate)}
          </div>
        </div>
      </div>
    </GlassCard>
  );
}

function DimensionScoresCard({ scores }: { scores: Record<string, number> | undefined }) {
  if (!scores || Object.keys(scores).length === 0) return null;

  return (
    <GlassCard variant="solid" className="p-6">
      <div className="flex items-center gap-3 mb-4">
        <BarChart3 className="w-5 h-5 text-purple-400" />
        <h3 className="text-lg font-semibold text-text-primary">Dimension Scores</h3>
      </div>
      
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {Object.entries(scores).map(([key, value]) => {
          const displayName = key.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
          
          return (
            <div key={key} className="group p-4 rounded-lg bg-black/90 border border-white/10 hover:border-accent-purple/50 transition-all cursor-pointer hover:shadow-glow-sm">
              <p className="text-sm text-text-secondary capitalize mb-2 group-hover:text-text-primary transition-colors">
                {displayName}
              </p>
              <p className="text-2xl font-bold text-text-primary">{value.toFixed(0)}</p>
            </div>
          );
        })}
      </div>
    </GlassCard>
  );
}

function ReviewContent({ type }: { type: 'weekly' | 'monthly' }) {
  const { data: weeklyData, isLoading: weeklyLoading } = useGetWeeklyReviewQuery(undefined, { skip: type !== 'weekly' });
  const { data: monthlyData, isLoading: monthlyLoading } = useGetMonthlyReviewQuery(undefined, { skip: type !== 'monthly' });
  const [generateReview, { isLoading: generating }] = useGenerateReviewMutation();

  const data = type === 'weekly' ? weeklyData : monthlyData;
  const isLoading = type === 'weekly' ? weeklyLoading : monthlyLoading;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-accent-cyan"></div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="text-center py-12">
        <p className="text-text-secondary mb-4">No review data available</p>
        <button
          onClick={() => generateReview({ type })}
          disabled={generating}
          className="bg-gradient-to-r from-accent-cyan to-accent-purple hover:opacity-90 text-white px-4 py-2 rounded-lg inline-flex items-center gap-2 transition-all"
        >
          <RefreshCw className={`w-4 h-4 ${generating ? 'animate-spin' : ''}`} />
          Generate Review
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Period Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-text-primary">
          {type === 'weekly' ? 'Week of ' : 'Month of '}
          {data.periodStart} - {data.periodEnd}
        </h3>
        <button
          onClick={() => generateReview({ type })}
          disabled={generating}
          className="text-text-secondary hover:text-text-primary p-2 rounded-lg hover:bg-background-hover transition-colors"
        >
          <RefreshCw className={`w-4 h-4 ${generating ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Score Cards with Current Values + Deltas */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <ScoreCard 
          title="Health Index" 
          currentValue={data.healthIndex} 
          delta={data.healthIndexDelta} 
          icon={Heart}
        />
        <ScoreCard 
          title="Adherence" 
          currentValue={data.adherenceIndex} 
          delta={data.adherenceIndexDelta} 
          suffix="%" 
          icon={Target}
        />
        <ScoreCard 
          title="Wealth Health" 
          currentValue={data.wealthHealth} 
          delta={data.wealthHealthDelta} 
          icon={DollarSign}
        />
        <ScoreCard 
          title="Longevity" 
          currentValue={data.longevity} 
          delta={data.longevityDelta} 
          suffix=" yrs" 
          icon={TrendingUp}
        />
      </div>

      {/* Financial Summary */}
      {data.financialSummary && <FinancialSummaryCard data={data.financialSummary} />}

      {/* Dimension Scores */}
      {data.dimensionScores && <DimensionScoresCard scores={data.dimensionScores} />}

      {/* Top Streaks */}
      {data.topStreaks && data.topStreaks.length > 0 && (
        <GlassCard variant="solid" className="p-6">
          <h4 className="text-base font-semibold text-text-primary mb-4 flex items-center gap-2">
            <Flame className="w-5 h-5 text-orange-400" /> Top Streaks
          </h4>
          <div className="space-y-2">
            {data.topStreaks.map((streak, index) => (
              <StreakCard key={index} streak={streak} />
            ))}
          </div>
        </GlassCard>
      )}

      {/* Recommended Actions */}
      {data.recommendedActions && data.recommendedActions.length > 0 && (
        <GlassCard variant="solid" className="p-6">
          <h4 className="text-base font-semibold text-text-primary mb-4 flex items-center gap-2">
            <Target className="w-5 h-5 text-blue-400" /> Recommended Actions
          </h4>
          <div className="space-y-2">
            {data.recommendedActions.map((action, index) => (
              <ActionCard key={index} action={action} />
            ))}
          </div>
        </GlassCard>
      )}

      {/* Primary Stats - Current Values */}
      {data.primaryStats && Object.keys(data.primaryStats).length > 0 && (
        <GlassCard variant="solid" className="p-6">
          <h4 className="text-base font-semibold text-text-primary mb-4">Primary Stats Movement</h4>
          <div className="grid grid-cols-7 gap-4">
            {Object.entries(data.primaryStats).map(([stat, value]) => {
              const delta = data.primaryStatsDelta?.[stat] || 0;
              return (
                <div key={stat} className="group p-4 rounded-lg bg-black/90 border border-white/10 hover:border-accent-purple/50 transition-all text-center hover:shadow-glow-sm">
                  <div className="text-xs text-text-secondary capitalize mb-2 group-hover:text-text-primary transition-colors">{stat}</div>
                  <div className="text-2xl font-bold text-text-primary mb-1">{typeof value === 'number' ? value.toFixed(0) : value}</div>
                  <DeltaIndicator value={delta} />
                </div>
              );
            })}
          </div>
        </GlassCard>
      )}
    </div>
  );
}

export function DashboardReviews() {
  const [activeTab, setActiveTab] = useState<'weekly' | 'monthly'>('weekly');

  return (
    <div className="space-y-6">
      {/* Tab Switcher */}
      <div className="flex flex-wrap bg-background-card rounded-lg p-1 border border-background-hover w-fit">
        <button
          onClick={() => setActiveTab('weekly')}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
            activeTab === 'weekly'
              ? 'bg-gradient-to-r from-accent-cyan to-accent-purple text-white'
              : 'text-text-secondary hover:text-text-primary'
          }`}
        >
          Weekly
        </button>
        <button
          onClick={() => setActiveTab('monthly')}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
            activeTab === 'monthly'
              ? 'bg-gradient-to-r from-accent-cyan to-accent-purple text-white'
              : 'text-text-secondary hover:text-text-primary'
          }`}
        >
          Monthly
        </button>
      </div>

      {/* Current Review */}
      <GlassCard variant="elevated" className="p-6">
        <ReviewContent type={activeTab} />
      </GlassCard>
    </div>
  );
}

export default DashboardReviews;
