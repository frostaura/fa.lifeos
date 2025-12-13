import { useState } from 'react';
import {
  useGetWeeklyReviewQuery,
  useGetMonthlyReviewQuery,
  useGenerateReviewMutation,
  useGetFinancialReviewQuery,
} from '../services/endpoints';
import { GlassCard } from '@components/atoms/GlassCard';
import { 
  ArrowUp, ArrowDown, Target, RefreshCw, 
  Wallet, DollarSign
} from 'lucide-react';

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

function ActionCard({ action }: { action: { action: string; priority: string; dimension: string } }) {
  const priorityStyles = {
    high: 'text-semantic-error bg-semantic-error/10 border-semantic-error/30',
    medium: 'text-semantic-warning bg-semantic-warning/10 border-semantic-warning/30',
    low: 'text-semantic-success bg-semantic-success/10 border-semantic-success/30',
  };

  const style = priorityStyles[action.priority as keyof typeof priorityStyles] || priorityStyles.low;

  return (
    <div className="flex items-start gap-3 bg-background-card rounded-lg px-4 py-3 border border-background-hover">
      <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-accent-cyan to-accent-purple flex items-center justify-center shrink-0">
        <Target className="w-4 h-4 text-white" />
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

function WeeklyReviewContent() {
  const { data, isLoading } = useGetWeeklyReviewQuery();
  const [generateReview, { isLoading: generating }] = useGenerateReviewMutation();

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
        <p className="text-text-secondary mb-4">No weekly review data available</p>
        <button
          onClick={() => generateReview({ type: 'weekly' })}
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
          Week of {data.periodStart} - {data.periodEnd}
        </h3>
        <button
          onClick={() => generateReview({ type: 'weekly' })}
          disabled={generating}
          className="text-text-secondary hover:text-text-primary p-2 rounded-lg hover:bg-background-hover transition-colors"
        >
          <RefreshCw className={`w-4 h-4 ${generating ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Financial Summary */}
      {data.financialSummary && (
        <GlassCard className="p-6">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-green-500 to-emerald-500 flex items-center justify-center">
              <Wallet className="w-5 h-5 text-white" />
            </div>
            <h3 className="text-lg font-semibold text-text-primary">Weekly Financial Summary</h3>
          </div>
          
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div>
              <div className="text-sm text-text-tertiary">Net Worth</div>
              <div className="text-xl font-bold text-text-primary">
                {data.financialSummary.netWorth != null ? `R ${data.financialSummary.netWorth.toLocaleString()}` : '--'}
              </div>
              <DeltaIndicator value={data.financialSummary.netWorthDelta} />
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Income</div>
              <div className="text-lg font-semibold text-semantic-success">
                {data.financialSummary.totalIncome != null ? `R ${data.financialSummary.totalIncome.toLocaleString()}` : '--'}
              </div>
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Expenses</div>
              <div className="text-lg font-semibold text-semantic-error">
                {data.financialSummary.totalExpenses != null ? `R ${data.financialSummary.totalExpenses.toLocaleString()}` : '--'}
              </div>
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Cash Flow</div>
              <div className={`text-lg font-semibold ${(data.financialSummary.netCashFlow ?? 0) >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
                {data.financialSummary.netCashFlow != null ? `R ${data.financialSummary.netCashFlow.toLocaleString()}` : '--'}
              </div>
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Savings Rate</div>
              <div className="text-lg font-semibold text-accent-cyan">
                {data.financialSummary.savingsRate != null ? `${data.financialSummary.savingsRate.toFixed(1)}%` : '--'}
              </div>
            </div>
          </div>
        </GlassCard>
      )}
    </div>
  );
}

function MonthlyReviewContent() {
  const { data, isLoading } = useGetMonthlyReviewQuery();
  const [generateReview, { isLoading: generating }] = useGenerateReviewMutation();

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
        <p className="text-text-secondary mb-4">No monthly review data available</p>
        <button
          onClick={() => generateReview({ type: 'monthly' })}
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
          Month of {data.periodStart} - {data.periodEnd}
        </h3>
        <button
          onClick={() => generateReview({ type: 'monthly' })}
          disabled={generating}
          className="text-text-secondary hover:text-text-primary p-2 rounded-lg hover:bg-background-hover transition-colors"
        >
          <RefreshCw className={`w-4 h-4 ${generating ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Financial Summary */}
      {data.financialSummary && (
        <GlassCard className="p-6">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-green-500 to-emerald-500 flex items-center justify-center">
              <Wallet className="w-5 h-5 text-white" />
            </div>
            <h3 className="text-lg font-semibold text-text-primary">Monthly Financial Summary</h3>
          </div>
          
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div>
              <div className="text-sm text-text-tertiary">Net Worth</div>
              <div className="text-xl font-bold text-text-primary">
                {data.financialSummary.netWorth != null ? `R ${data.financialSummary.netWorth.toLocaleString()}` : '--'}
              </div>
              <DeltaIndicator value={data.financialSummary.netWorthDelta} />
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Income</div>
              <div className="text-lg font-semibold text-semantic-success">
                {data.financialSummary.totalIncome != null ? `R ${data.financialSummary.totalIncome.toLocaleString()}` : '--'}
              </div>
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Expenses</div>
              <div className="text-lg font-semibold text-semantic-error">
                {data.financialSummary.totalExpenses != null ? `R ${data.financialSummary.totalExpenses.toLocaleString()}` : '--'}
              </div>
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Cash Flow</div>
              <div className={`text-lg font-semibold ${(data.financialSummary.netCashFlow ?? 0) >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
                {data.financialSummary.netCashFlow != null ? `R ${data.financialSummary.netCashFlow.toLocaleString()}` : '--'}
              </div>
            </div>
            <div>
              <div className="text-sm text-text-tertiary">Savings Rate</div>
              <div className="text-lg font-semibold text-accent-cyan">
                {data.financialSummary.savingsRate != null ? `${data.financialSummary.savingsRate.toFixed(1)}%` : '--'}
              </div>
            </div>
          </div>
        </GlassCard>
      )}
    </div>
  );
}

export function FinancialReviews() {
  const [activeTab, setActiveTab] = useState<'weekly' | 'monthly' | 'financial'>('financial');

  return (
    <div className="space-y-4">
      {/* Header */}
      <div>
        <h1 className="text-base md:text-lg font-bold text-text-primary">Financial Reviews</h1>
        <p className="text-text-secondary mt-0.5 text-xs">Track your financial performance over time</p>
      </div>

      {/* Tab Switcher */}
      <GlassCard variant="default" className="p-2">
        <div className="flex flex-wrap gap-1">
          <button
            onClick={() => setActiveTab('financial')}
            className={`px-3 py-2 rounded-lg text-xs md:text-sm font-medium transition-all ${
              activeTab === 'financial'
                ? 'bg-accent-purple/20 text-accent-purple'
                : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
            }`}
          >
            💰 Full Review
          </button>
          <button
            onClick={() => setActiveTab('weekly')}
            className={`px-3 py-2 rounded-lg text-xs md:text-sm font-medium transition-all ${
              activeTab === 'weekly'
                ? 'bg-accent-purple/20 text-accent-purple'
                : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
            }`}
          >
            Weekly
          </button>
          <button
            onClick={() => setActiveTab('monthly')}
            className={`px-3 py-2 rounded-lg text-xs md:text-sm font-medium transition-all ${
              activeTab === 'monthly'
                ? 'bg-accent-purple/20 text-accent-purple'
                : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
            }`}
          >
            Monthly
          </button>
        </div>
      </GlassCard>

      {/* Current Review Content */}
      <GlassCard variant="elevated" className="p-4 md:p-6">
        {activeTab === 'financial' && <FinancialReviewContent />}
        {activeTab === 'weekly' && <WeeklyReviewContent />}
        {activeTab === 'monthly' && <MonthlyReviewContent />}
      </GlassCard>
    </div>
  );
}

export default FinancialReviews;
