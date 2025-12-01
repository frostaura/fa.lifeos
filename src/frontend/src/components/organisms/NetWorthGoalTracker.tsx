import { GlassCard } from '@components/atoms/GlassCard';
import { ProgressBar } from '@components/atoms/ProgressBar';
import { Spinner } from '@components/atoms/Spinner';
import { Trophy, CheckCircle2, Circle } from 'lucide-react';
import { cn } from '@utils/cn';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import { useGetAccountsQuery } from '@/services';

interface Milestone {
  label: string;
  value: number;
  color: string;
}

const MILESTONES: Milestone[] = [
  { label: 'R100K', value: 100000, color: '#8b5cf6' },
  { label: 'R500K', value: 500000, color: '#06b6d4' },
  { label: 'R1M', value: 1000000, color: '#22c55e' },
  { label: 'R5M', value: 5000000, color: '#f59e0b' },
  { label: 'R10M', value: 10000000, color: '#ec4899' },
];

export function NetWorthGoalTracker() {
  const { data: accountsData, isLoading, error } = useGetAccountsQuery();

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-center h-40">
          <Spinner size="lg" />
        </div>
      </GlassCard>
    );
  }

  if (error) {
    return (
      <GlassCard variant="default" className="p-6">
        <p className="text-semantic-error">Failed to load net worth data</p>
      </GlassCard>
    );
  }

  const netWorth = accountsData?.meta?.netWorth || 0;
  
  // Find current milestone and next milestone
  const completedMilestones = MILESTONES.filter(m => netWorth >= m.value);
  const nextMilestone = MILESTONES.find(m => netWorth < m.value);
  
  // Calculate progress to next milestone
  let progressToNext = 100;
  let previousValue = 0;
  
  if (nextMilestone) {
    const prevMilestone = [...MILESTONES].reverse().find(m => m.value < nextMilestone.value && netWorth >= m.value);
    previousValue = prevMilestone?.value || 0;
    const range = nextMilestone.value - previousValue;
    const currentProgress = netWorth - previousValue;
    progressToNext = Math.min(100, Math.max(0, (currentProgress / range) * 100));
  }

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center gap-2 mb-4">
        <Trophy className="w-5 h-5 text-accent-yellow" />
        <h2 className="text-lg font-semibold text-text-primary">Net Worth Milestones</h2>
      </div>

      {/* Current Net Worth Display */}
      <div className="text-center mb-6 p-4 rounded-lg bg-background-hover/50">
        <p className="text-text-secondary text-sm mb-1">Current Net Worth</p>
        <p className="text-3xl font-bold text-text-primary">
          {formatCurrency(netWorth)}
        </p>
        {completedMilestones.length > 0 && (
          <p className="text-sm text-semantic-success mt-1">
            {completedMilestones.length} milestone{completedMilestones.length > 1 ? 's' : ''} achieved!
          </p>
        )}
      </div>

      {/* Progress to Next Milestone */}
      {nextMilestone && (
        <div className="mb-6">
          <div className="flex items-center justify-between text-sm mb-2">
            <span className="text-text-secondary">Progress to {nextMilestone.label}</span>
            <span className="text-text-primary font-medium">
              {formatCurrency(netWorth - previousValue)} / {formatCurrency(nextMilestone.value - previousValue)}
            </span>
          </div>
          <ProgressBar 
            value={progressToNext} 
            color={nextMilestone.color} 
            height="md"
          />
          <p className="text-xs text-text-tertiary mt-1">
            {formatCurrency(nextMilestone.value - netWorth)} remaining
          </p>
        </div>
      )}

      {/* Milestones Checklist */}
      <div className="space-y-3">
        <p className="text-sm text-text-secondary font-medium">Milestones</p>
        {MILESTONES.map((milestone) => {
          const isCompleted = netWorth >= milestone.value;
          return (
            <div 
              key={milestone.label}
              className={cn(
                'flex items-center justify-between p-3 rounded-lg transition-colors',
                isCompleted 
                  ? 'bg-semantic-success/10 border border-semantic-success/30' 
                  : 'bg-background-hover/30'
              )}
            >
              <div className="flex items-center gap-3">
                {isCompleted ? (
                  <CheckCircle2 className="w-5 h-5 text-semantic-success" />
                ) : (
                  <Circle className="w-5 h-5 text-text-tertiary" />
                )}
                <span className={cn(
                  'font-medium',
                  isCompleted ? 'text-semantic-success' : 'text-text-primary'
                )}>
                  {milestone.label}
                </span>
              </div>
              <span className={cn(
                'text-sm',
                isCompleted ? 'text-semantic-success' : 'text-text-tertiary'
              )}>
                {isCompleted ? 'Achieved' : formatCurrency(milestone.value)}
              </span>
            </div>
          );
        })}
      </div>
    </GlassCard>
  );
}
