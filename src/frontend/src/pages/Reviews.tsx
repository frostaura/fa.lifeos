import { useState } from 'react';
import {
  useGetWeeklyReviewQuery,
  useGetMonthlyReviewQuery,
  useGetReviewHistoryQuery,
  useGenerateReviewMutation,
  useGetDimensionReviewQuery,
  useGetFinancialReviewQuery,
} from '../services/endpoints';
import { useGetDimensionsQuery } from '../services/endpoints/dimensions';
import { GlassCard } from '@components/atoms/GlassCard';
import { 
  ArrowUp, ArrowDown, Flame, Target, RefreshCw, 
  TrendingUp, Wallet, Heart, Users, Briefcase, 
  Gamepad2, Home, Palette, Brain, Globe, DollarSign,
  BarChart3, Activity
} from 'lucide-react';

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
  icon: Icon,
  gradient = 'from-accent-cyan to-accent-purple'
}: { 
  title: string; 
  currentValue: number | null | undefined;
  delta: number | null | undefined;
  suffix?: string;
  icon?: React.FC<{ className?: string }>;
  gradient?: string;
}) {
  return (
    <GlassCard className="p-4">
      <div className="flex items-center gap-2 mb-2">
        {Icon && (
          <div className={`w-8 h-8 rounded-lg bg-gradient-to-br ${gradient} flex items-center justify-center`}>
            <Icon className="w-4 h-4 text-white" />
          </div>
        )}
        <span className="text-sm text-text-secondary">{title}</span>
      </div>
      <div className="text-2xl font-bold text-text-primary mb-1">
        {currentValue !== null && currentValue !== undefined ? currentValue.toFixed(1) : '--'}{currentValue !== null && currentValue !== undefined && suffix}
      </div>
      <DeltaIndicator value={delta} suffix={suffix} />
    </GlassCard>
  );
}

function StreakCard({ streak }: { streak: { taskId?: string; taskTitle: string; streakDays?: number; currentStreak?: number } }) {
  const days = streak.streakDays ?? streak.currentStreak ?? 0;
  return (
    <div className="flex items-center justify-between bg-background-card rounded-lg px-4 py-3 border border-background-hover">
      <div className="flex items-center gap-3">
        <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-orange-500 to-red-500 flex items-center justify-center">
          <Flame className="w-4 h-4 text-white" />
        </div>
        <span className="text-text-primary">{streak.taskTitle}</span>
      </div>
      <span className="text-orange-400 font-semibold">{days} days</span>
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
    <div className="flex items-start gap-3 bg-background-card rounded-lg px-4 py-3 border border-background-hover">
      <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-accent-cyan to-accent-purple flex items-center justify-center shrink-0">
        <Icon className="w-4 h-4 text-white" />
      </div>
      <div className="flex-1">
        <div className="text-text-primary">{action.action}</div>
        <div className="flex items-center gap-2 mt-1">
          <span className={`text-xs px-2 py-0.5 rounded-full border ${style}`}>
            {action.priority.toUpperCase()}
          </span>
          <span className="text-xs text-text-tertiary">{action.dimension.replace('_', ' ')}</span>
        </div>
      </div>
    </div>
  );
}

function FinancialSummaryCard({ data }: { data: any }) {
  if (!data) return null;
  
  return (
    <GlassCard className="p-6">
      <div className="flex items-center gap-3 mb-4">
        <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-green-500 to-emerald-500 flex items-center justify-center">
          <Wallet className="w-5 h-5 text-white" />
        </div>
        <h3 className="text-lg font-semibold text-text-primary">Financial Summary</h3>
      </div>
      
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        <div>
          <div className="text-sm text-text-tertiary">Net Worth</div>
          <div className="text-xl font-bold text-text-primary">
            {data.netWorth != null ? `R ${data.netWorth.toLocaleString()}` : '--'}
          </div>
          <DeltaIndicator value={data.netWorthDelta} />
        </div>
        <div>
          <div className="text-sm text-text-tertiary">Income</div>
          <div className="text-lg font-semibold text-semantic-success">
            {data.totalIncome != null ? `R ${data.totalIncome.toLocaleString()}` : '--'}
          </div>
        </div>
        <div>
          <div className="text-sm text-text-tertiary">Expenses</div>
          <div className="text-lg font-semibold text-semantic-error">
            {data.totalExpenses != null ? `R ${data.totalExpenses.toLocaleString()}` : '--'}
          </div>
        </div>
        <div>
          <div className="text-sm text-text-tertiary">Cash Flow</div>
          <div className={`text-lg font-semibold ${(data.netCashFlow ?? 0) >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
            {data.netCashFlow != null ? `R ${data.netCashFlow.toLocaleString()}` : '--'}
          </div>
        </div>
        <div>
          <div className="text-sm text-text-tertiary">Savings Rate</div>
          <div className="text-lg font-semibold text-accent-cyan">
            {data.savingsRate != null ? `${data.savingsRate.toFixed(1)}%` : '--'}
          </div>
        </div>
      </div>
    </GlassCard>
  );
}

function DimensionScoresCard({ scores }: { scores: Record<string, number> | undefined }) {
  if (!scores || Object.keys(scores).length === 0) return null;

  return (
    <GlassCard className="p-6">
      <div className="flex items-center gap-3 mb-4">
        <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500 to-purple-500 flex items-center justify-center">
          <BarChart3 className="w-5 h-5 text-white" />
        </div>
        <h3 className="text-lg font-semibold text-text-primary">Dimension Scores</h3>
      </div>
      
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        {Object.entries(scores).map(([key, value]) => {
          const Icon = dimensionIcons[key] || Activity;
          const displayName = key.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
          
          return (
            <div key={key} className="bg-background rounded-lg p-3 border border-background-hover">
              <div className="flex items-center gap-2 mb-1">
                <Icon className="w-4 h-4 text-text-tertiary" />
                <span className="text-xs text-text-tertiary truncate">{displayName}</span>
              </div>
              <div className="text-lg font-bold text-text-primary">{value.toFixed(0)}</div>
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
          gradient="from-red-500 to-pink-500"
        />
        <ScoreCard 
          title="Adherence" 
          currentValue={data.adherenceIndex} 
          delta={data.adherenceIndexDelta} 
          suffix="%" 
          icon={Target}
          gradient="from-amber-500 to-orange-500"
        />
        <ScoreCard 
          title="Wealth Health" 
          currentValue={data.wealthHealth} 
          delta={data.wealthHealthDelta} 
          icon={DollarSign}
          gradient="from-green-500 to-emerald-500"
        />
        <ScoreCard 
          title="Longevity" 
          currentValue={data.longevity} 
          delta={data.longevityDelta} 
          suffix=" yrs" 
          icon={TrendingUp}
          gradient="from-blue-500 to-indigo-500"
        />
      </div>

      {/* Financial Summary */}
      {data.financialSummary && <FinancialSummaryCard data={data.financialSummary} />}

      {/* Dimension Scores */}
      {data.dimensionScores && <DimensionScoresCard scores={data.dimensionScores} />}

      {/* Top Streaks */}
      {data.topStreaks && data.topStreaks.length > 0 && (
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-3 flex items-center gap-2">
            <Flame className="w-4 h-4 text-orange-400" /> Top Streaks
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
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-3 flex items-center gap-2">
            <Target className="w-4 h-4 text-accent-cyan" /> Recommended Actions
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
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-3">Primary Stats Movement</h4>
          <div className="grid grid-cols-7 gap-2">
            {Object.entries(data.primaryStats).map(([stat, value]) => {
              const delta = data.primaryStatsDelta?.[stat] || 0;
              return (
                <div key={stat} className="bg-background rounded-lg p-2 text-center border border-background-hover">
                  <div className="text-xs text-text-tertiary capitalize">{stat}</div>
                  <div className="text-lg font-bold text-text-primary">{typeof value === 'number' ? value.toFixed(0) : value}</div>
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

function DimensionReviewContent({ dimensionCode }: { dimensionCode: string }) {
  const { data, isLoading, error } = useGetDimensionReviewQuery({ dimensionCode, period: 'weekly' });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-accent-cyan"></div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="text-center py-12">
        <p className="text-text-secondary">Unable to load dimension review</p>
      </div>
    );
  }

  const Icon = dimensionIcons[dimensionCode] || Activity;

  return (
    <div className="space-y-6">
      {/* Dimension Header */}
      <div className="flex items-center gap-4">
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-accent-purple to-violet-500 flex items-center justify-center">
          <Icon className="w-6 h-6 text-white" />
        </div>
        <div>
          <h3 className="text-xl font-semibold text-text-primary">{data.dimension.name}</h3>
          <p className="text-text-secondary text-sm">
            Score: <span className="text-accent-cyan font-semibold">{data.dimension.score.toFixed(1)}</span>
          </p>
        </div>
      </div>

      {/* Metrics */}
      {data.metrics && data.metrics.length > 0 && (
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-4">Metrics</h4>
          <div className="space-y-3">
            {data.metrics.map((metric, index) => (
              <div key={index} className="flex items-center justify-between bg-background rounded-lg p-3 border border-background-hover">
                <div>
                  <div className="text-text-primary font-medium">{metric.name}</div>
                  <div className="text-xs text-text-tertiary">{metric.recordCount} records</div>
                </div>
                <div className="text-right">
                  <div className="text-lg font-bold text-text-primary">
                    {metric.currentValue?.toFixed(1) ?? '--'} {metric.unit}
                  </div>
                  <DeltaIndicator value={metric.delta} suffix={` ${metric.unit}`} />
                </div>
              </div>
            ))}
          </div>
        </GlassCard>
      )}

      {/* Streaks */}
      {data.streaks && data.streaks.length > 0 && (
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-3 flex items-center gap-2">
            <Flame className="w-4 h-4 text-orange-400" /> Streaks
          </h4>
          <div className="space-y-2">
            {data.streaks.map((streak, index) => (
              <StreakCard key={index} streak={streak} />
            ))}
          </div>
        </GlassCard>
      )}

      {/* Milestones Summary */}
      <div className="grid grid-cols-2 gap-4">
        <GlassCard className="p-4 text-center">
          <div className="text-3xl font-bold text-accent-cyan">{data.activeMilestones}</div>
          <div className="text-sm text-text-tertiary">Active Milestones</div>
        </GlassCard>
        <GlassCard className="p-4 text-center">
          <div className="text-3xl font-bold text-semantic-success">{data.completedMilestones}</div>
          <div className="text-sm text-text-tertiary">Completed</div>
        </GlassCard>
      </div>

      {/* Recommended Actions */}
      {data.recommendedActions && data.recommendedActions.length > 0 && (
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-3 flex items-center gap-2">
            <Target className="w-4 h-4 text-accent-cyan" /> Recommended Actions
          </h4>
          <div className="space-y-2">
            {data.recommendedActions.map((action, index) => (
              <ActionCard key={index} action={action} />
            ))}
          </div>
        </GlassCard>
      )}
    </div>
  );
}

function FinancialReviewContent() {
  const { data, isLoading, error } = useGetFinancialReviewQuery({ period: 'monthly' });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-accent-cyan"></div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="text-center py-12">
        <p className="text-text-secondary">Unable to load financial review</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Financial Header */}
      <div className="flex items-center gap-4">
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-green-500 to-emerald-500 flex items-center justify-center">
          <Wallet className="w-6 h-6 text-white" />
        </div>
        <div>
          <h3 className="text-xl font-semibold text-text-primary">Financial Review</h3>
          <p className="text-text-secondary text-sm">{data.periodStart} - {data.periodEnd}</p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <GlassCard className="p-4">
          <div className="text-sm text-text-tertiary mb-1">Net Worth</div>
          <div className="text-xl font-bold text-text-primary">
            R {data.summary.netWorth.toLocaleString()}
          </div>
        </GlassCard>
        <GlassCard className="p-4">
          <div className="text-sm text-text-tertiary mb-1">Wealth Health</div>
          <div className="text-xl font-bold text-accent-cyan">
            {data.summary.wealthHealthScore.toFixed(1)}
          </div>
        </GlassCard>
        <GlassCard className="p-4">
          <div className="text-sm text-text-tertiary mb-1">Cash Flow</div>
          <div className={`text-xl font-bold ${data.summary.netCashFlow >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
            R {data.summary.netCashFlow.toLocaleString()}
          </div>
        </GlassCard>
        <GlassCard className="p-4">
          <div className="text-sm text-text-tertiary mb-1">Savings Rate</div>
          <div className="text-xl font-bold text-accent-purple">
            {data.summary.savingsRate.toFixed(1)}%
          </div>
        </GlassCard>
      </div>

      {/* Account Breakdown */}
      {data.accountBreakdown && data.accountBreakdown.length > 0 && (
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-4">Account Breakdown</h4>
          <div className="space-y-3">
            {data.accountBreakdown.map((account, index) => (
              <div key={index} className="flex items-center justify-between bg-background rounded-lg p-3 border border-background-hover">
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-accent-cyan to-accent-purple flex items-center justify-center">
                    <DollarSign className="w-4 h-4 text-white" />
                  </div>
                  <div>
                    <div className="text-text-primary font-medium">{account.type}</div>
                    <div className="text-xs text-text-tertiary">{account.count} account(s)</div>
                  </div>
                </div>
                <div className={`text-lg font-bold ${account.totalBalance >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
                  R {account.totalBalance.toLocaleString()}
                </div>
              </div>
            ))}
          </div>
        </GlassCard>
      )}

      {/* Projections */}
      {data.projections && (
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-4">12-Month Projection</h4>
          <div className="grid grid-cols-2 gap-4 mb-4">
            <div className="bg-background rounded-lg p-4 border border-background-hover">
              <div className="text-sm text-text-tertiary">Projected Net Worth</div>
              <div className="text-2xl font-bold text-text-primary">
                R {data.projections.projectedNetWorthIn12Months.toLocaleString()}
              </div>
            </div>
            <div className="bg-background rounded-lg p-4 border border-background-hover">
              <div className="text-sm text-text-tertiary">Projected Growth</div>
              <div className={`text-2xl font-bold ${data.projections.projectedGrowth >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
                R {data.projections.projectedGrowth.toLocaleString()}
              </div>
            </div>
          </div>
        </GlassCard>
      )}

      {/* Recommended Actions */}
      {data.recommendedActions && data.recommendedActions.length > 0 && (
        <GlassCard className="p-6">
          <h4 className="text-sm font-medium text-text-secondary mb-3 flex items-center gap-2">
            <Target className="w-4 h-4 text-accent-cyan" /> Recommended Actions
          </h4>
          <div className="space-y-2">
            {data.recommendedActions.map((action, index) => (
              <ActionCard key={index} action={action} />
            ))}
          </div>
        </GlassCard>
      )}
    </div>
  );
}

export function Reviews() {
  const [activeTab, setActiveTab] = useState<'weekly' | 'monthly' | 'financial' | string>('weekly');
  const { data: history } = useGetReviewHistoryQuery({ type: activeTab === 'financial' ? 'monthly' : activeTab, count: 5 });
  const { data: dimensions } = useGetDimensionsQuery();

  // Check if viewing a dimension
  const isDimensionView = activeTab.startsWith('dim_');
  const dimensionCode = isDimensionView ? activeTab.replace('dim_', '') : null;

  return (
    <>
      {/* Sticky Header */}
      <div className="sticky top-0 z-20 bg-background-primary/95 backdrop-blur-md border-b border-glass-border rounded-b-xl mb-6">
        <div className="p-6 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <h1 className="text-xl md:text-2xl lg:text-3xl font-bold text-text-primary">Reviews</h1>
        
          {/* Tab Switcher */}
          <div className="flex flex-wrap bg-background-card rounded-lg p-1 border border-background-hover">
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
          <button
            onClick={() => setActiveTab('financial')}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
              activeTab === 'financial'
                ? 'bg-gradient-to-r from-accent-cyan to-accent-purple text-white'
                : 'text-text-secondary hover:text-text-primary'
            }`}
          >
            💰 Financial
          </button>
        </div>
      </div>

      {/* Dimension Quick Select */}
      {dimensions?.data && dimensions.data.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {dimensions.data.map((dim) => {
            const Icon = dimensionIcons[dim.attributes.code] || Activity;
            const isSelected = activeTab === `dim_${dim.attributes.code}`;
            return (
              <button
                key={dim.id}
                onClick={() => setActiveTab(`dim_${dim.attributes.code}`)}
                className={`px-3 py-1.5 rounded-full text-sm font-medium transition-all flex items-center gap-2 ${
                  isSelected
                    ? 'bg-gradient-to-r from-accent-purple to-violet-600 text-white'
                    : 'bg-background-card text-text-secondary hover:bg-background-hover border border-background-hover'
                }`}
              >
                <Icon className="w-4 h-4" />
                {dim.attributes.name}
              </button>
            );
          })}
        </div>
      )}

      {/* Current Review */}
      <GlassCard variant="elevated" className="p-6">
        {activeTab === 'weekly' || activeTab === 'monthly' ? (
          <ReviewContent type={activeTab} />
        ) : activeTab === 'financial' ? (
          <FinancialReviewContent />
        ) : isDimensionView && dimensionCode ? (
          <DimensionReviewContent dimensionCode={dimensionCode} />
        ) : null}
      </GlassCard>

      {/* History */}
      {!isDimensionView && activeTab !== 'financial' && history && history.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold text-text-primary mb-4">Past Reviews</h3>
          <div className="space-y-2">
            {history.map((review, index) => (
              <GlassCard
                key={index}
                className="p-4 flex items-center justify-between"
              >
                <span className="text-text-primary">
                  {review.periodStart} - {review.periodEnd}
                </span>
                <div className="flex items-center gap-4 text-sm">
                  <span className="text-text-tertiary">
                    Health: <span className="text-text-primary font-semibold">{review.healthIndex?.toFixed(1) || '--'}</span>
                  </span>
                  <span className="text-text-tertiary">
                    Adherence: <span className="text-text-primary font-semibold">{review.adherenceIndex?.toFixed(1) || '--'}%</span>
                  </span>
                </div>
              </GlassCard>
            ))}
          </div>
        </div>
      )}
      </div>
    </>
  );
}

export default Reviews;
