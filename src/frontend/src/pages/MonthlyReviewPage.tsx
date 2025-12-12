import { useState } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { Button } from '@components/atoms/Button';
import { Badge } from '@components/atoms/Badge';
import { useGetMonthlyReviewQuery, useRecordMetricsMutation } from '@/store/api/mcpApi';
import { TrendingUp, TrendingDown, Award, Target, DollarSign } from 'lucide-react';
import { cn } from '@utils/cn';

export function MonthlyReviewPage() {
  // Get current month start
  const getMonthStart = (date: Date = new Date()) => {
    const d = new Date(date);
    d.setDate(1);
    d.setHours(0, 0, 0, 0);
    return d.toISOString().split('T')[0];
  };

  const [monthStartDate, setMonthStartDate] = useState(getMonthStart());
  const { data: review, isLoading, error, refetch } = useGetMonthlyReviewQuery({ monthStartDate });
  const [recordMetrics] = useRecordMetricsMutation();
  const [markingComplete, setMarkingComplete] = useState(false);

  const handleMarkComplete = async () => {
    setMarkingComplete(true);
    try {
      await recordMetrics({
        timestamp: new Date().toISOString(),
        source: 'ui',
        metrics: {
          growth_mind: {
            monthly_review_done: true,
          },
        },
      }).unwrap();
      await refetch();
    } catch (err) {
      console.error('Failed to mark review complete:', err);
    } finally {
      setMarkingComplete(false);
    }
  };

  const navigateMonth = (direction: 'prev' | 'next') => {
    const current = new Date(monthStartDate);
    current.setMonth(current.getMonth() + (direction === 'next' ? 1 : -1));
    setMonthStartDate(getMonthStart(current));
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !review) {
    return (
      <div className="text-center text-red-400 p-8">
        <p>Error loading monthly review</p>
        <Button onClick={() => refetch()} className="mt-4">
          Retry
        </Button>
      </div>
    );
  }

  const formatMonth = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-US', { 
      month: 'long',
      year: 'numeric',
    });
  };

  return (
    <div className="space-y-6 max-w-6xl mx-auto px-4 md:px-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl md:text-3xl font-bold text-white">
            Monthly Review
          </h1>
          <p className="text-gray-400 mt-1">
            {formatMonth(review.period.start)}
          </p>
        </div>
        
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" onClick={() => navigateMonth('prev')}>
            ← Previous
          </Button>
          <Button variant="secondary" size="sm" onClick={() => navigateMonth('next')}>
            Next →
          </Button>
        </div>
      </div>

      {/* Net Worth Change - Hero Card */}
      <GlassCard variant="elevated" className="p-8">
        <div className="flex items-center gap-2 mb-6">
          <DollarSign className="w-6 h-6 text-amber-400" />
          <h2 className="text-2xl font-bold text-white">Net Worth Trajectory</h2>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div>
            <p className="text-sm text-gray-400 mb-2">Start of Month</p>
            <p className="text-3xl font-bold text-white">
              {new Intl.NumberFormat('en-ZA', {
                style: 'currency',
                currency: 'ZAR',
                minimumFractionDigits: 0,
                maximumFractionDigits: 0,
              }).format(review.netWorthChange.from)}
            </p>
          </div>
          
          <div>
            <p className="text-sm text-gray-400 mb-2">End of Month</p>
            <p className="text-3xl font-bold text-white">
              {new Intl.NumberFormat('en-ZA', {
                style: 'currency',
                currency: 'ZAR',
                minimumFractionDigits: 0,
                maximumFractionDigits: 0,
              }).format(review.netWorthChange.to)}
            </p>
          </div>
          
          <div>
            <p className="text-sm text-gray-400 mb-2">Change</p>
            <div className="flex items-baseline gap-2">
              <p className={cn(
                'text-3xl font-bold',
                review.netWorthChange.percentChange >= 0 ? 'text-green-400' : 'text-red-400'
              )}>
                {review.netWorthChange.percentChange >= 0 ? '+' : ''}
                {review.netWorthChange.percentChange.toFixed(1)}%
              </p>
              {review.netWorthChange.percentChange >= 0 ? (
                <TrendingUp className="w-6 h-6 text-green-400" />
              ) : (
                <TrendingDown className="w-6 h-6 text-red-400" />
              )}
            </div>
            <p className={cn(
              'text-sm mt-1',
              review.netWorthChange.percentChange >= 0 ? 'text-green-400' : 'text-red-400'
            )}>
              {new Intl.NumberFormat('en-ZA', {
                style: 'currency',
                currency: 'ZAR',
                minimumFractionDigits: 0,
                maximumFractionDigits: 0,
                signDisplay: 'always',
              }).format(review.netWorthChange.to - review.netWorthChange.from)}
            </p>
          </div>
        </div>
      </GlassCard>

      {/* Primary Stats Evolution */}
      <GlassCard variant="default" className="p-6">
        <h3 className="text-lg font-semibold text-white mb-6">Identity Stats Evolution</h3>
        
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* Radar comparison would go here if we had before/after data */}
          <div className="space-y-4">
            <h4 className="text-sm font-medium text-gray-400">Current Stats</h4>
            {Object.entries(review.primaryStatsChange).map(([stat, change]) => (
              <div key={stat} className="space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-white capitalize">{stat}</span>
                  <div className="flex items-center gap-2">
                    <span className="text-white font-bold">
                      {Math.round(change.to)}
                    </span>
                    <span className={cn(
                      'text-sm font-medium',
                      change.to >= change.from ? 'text-green-400' : 'text-red-400'
                    )}>
                      {change.to >= change.from ? '+' : ''}
                      {(change.to - change.from).toFixed(1)}
                    </span>
                  </div>
                </div>
                <div className="h-2 bg-gray-800 rounded-full overflow-hidden">
                  <div 
                    className="h-full bg-gradient-to-r from-purple-400 to-pink-600 transition-all duration-500"
                    style={{ width: `${change.to}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
          
          {/* Placeholder for radar - would need actual implementation */}
          <div className="flex items-center justify-center bg-gray-800/30 rounded-lg p-8">
            <p className="text-gray-400 text-center">
              Identity Radar visualization<br />
              <span className="text-sm">(Full implementation pending)</span>
            </p>
          </div>
        </div>
      </GlassCard>

      {/* Milestones & Top Metrics Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Milestones Completed */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center gap-2 mb-4">
            <Award className="w-5 h-5 text-amber-400" />
            <h3 className="text-lg font-semibold text-white">Milestones Completed</h3>
          </div>
          
          <div className="space-y-3">
            {review.milestonesCompleted.length === 0 ? (
              <p className="text-gray-400 text-sm text-center py-8">
                No milestones completed this month
              </p>
            ) : (
              review.milestonesCompleted.map((milestone, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between p-3 rounded-lg bg-amber-500/10 border border-amber-500/20"
                >
                  <span className="text-white font-medium">{milestone.title}</span>
                  <span className="text-xs text-gray-400">
                    {new Date(milestone.completedAt).toLocaleDateString()}
                  </span>
                </div>
              ))
            )}
          </div>
        </GlassCard>

        {/* Top Metric Improvements */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center gap-2 mb-4">
            <Target className="w-5 h-5 text-green-400" />
            <h3 className="text-lg font-semibold text-white">Top Improvements</h3>
          </div>
          
          <div className="space-y-3">
            {review.topMetricImprovements.length === 0 ? (
              <p className="text-gray-400 text-sm text-center py-8">
                No significant improvements tracked
              </p>
            ) : (
              review.topMetricImprovements.map((improvement, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between p-3 rounded-lg bg-green-500/10 border border-green-500/20"
                >
                  <span className="text-white font-medium">{improvement.metricName}</span>
                  <Badge variant="success">{improvement.improvement}</Badge>
                </div>
              ))
            )}
          </div>
        </GlassCard>
      </div>

      {/* Focus Areas for Next Month */}
      <GlassCard variant="elevated" className="p-6">
        <h3 className="text-lg font-semibold text-white mb-4">Focus Areas for Next Month</h3>
        
        <ul className="space-y-3">
          {review.focusAreas.map((area, idx) => (
            <li
              key={idx}
              className="flex items-start gap-3 p-4 rounded-lg bg-gray-800/50 border border-gray-700/50"
            >
              <span className="flex-shrink-0 w-6 h-6 rounded-full bg-purple-500/20 text-purple-400 flex items-center justify-center text-sm font-bold">
                {idx + 1}
              </span>
              <span className="text-gray-200">{area}</span>
            </li>
          ))}
        </ul>
        
        <div className="mt-6 flex justify-center">
          <Button
            onClick={handleMarkComplete}
            disabled={markingComplete}
            size="lg"
            className="w-full md:w-auto"
          >
            {markingComplete ? 'Marking Complete...' : 'Mark Review as Complete'}
          </Button>
        </div>
      </GlassCard>
    </div>
  );
}
