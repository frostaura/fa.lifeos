import { useState } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { Button } from '@components/atoms/Button';
import { Badge } from '@components/atoms/Badge';
import { ProgressBar } from '@components/atoms/ProgressBar';
import { useGetWeeklyReviewQuery, useRecordMetricsMutation } from '@/store/api/mcpApi';
import { TrendingUp, TrendingDown, Target, AlertTriangle, Calendar } from 'lucide-react';
import { cn } from '@utils/cn';

export function WeeklyReviewPage() {
  // Get current week start (Monday)
  const getWeekStart = (date: Date = new Date()) => {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d.toISOString().split('T')[0];
  };

  const [weekStartDate, setWeekStartDate] = useState(getWeekStart());
  const { data: review, isLoading, error, refetch } = useGetWeeklyReviewQuery({ weekStartDate });
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
            weekly_review_done: true,
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

  const navigateWeek = (direction: 'prev' | 'next') => {
    const current = new Date(weekStartDate);
    current.setDate(current.getDate() + (direction === 'next' ? 7 : -7));
    setWeekStartDate(getWeekStart(current));
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
        <p>Error loading weekly review</p>
        <Button onClick={() => refetch()} className="mt-4">
          Retry
        </Button>
      </div>
    );
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric',
      year: 'numeric',
    });
  };

  return (
    <div className="space-y-6 max-w-6xl mx-auto px-4 md:px-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl md:text-3xl font-bold text-white">
            Weekly Review
          </h1>
          <p className="text-gray-400 mt-1">
            {formatDate(review.period.start)} - {formatDate(review.period.end)}
          </p>
        </div>
        
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" onClick={() => navigateWeek('prev')}>
            ← Previous
          </Button>
          <Button variant="secondary" size="sm" onClick={() => navigateWeek('next')}>
            Next →
          </Button>
        </div>
      </div>

      {/* Score Changes Row */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {/* Health Index Change */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-medium text-gray-400">Health Index</h3>
            {review.healthIndexChange.to >= review.healthIndexChange.from ? (
              <TrendingUp className="w-5 h-5 text-green-400" />
            ) : (
              <TrendingDown className="w-5 h-5 text-red-400" />
            )}
          </div>
          
          <div className="space-y-2">
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-bold text-white">
                {Math.round(review.healthIndexChange.to)}
              </span>
              <span className={cn(
                'text-sm font-medium',
                review.healthIndexChange.to >= review.healthIndexChange.from
                  ? 'text-green-400'
                  : 'text-red-400'
              )}>
                {review.healthIndexChange.to >= review.healthIndexChange.from ? '+' : ''}
                {(review.healthIndexChange.to - review.healthIndexChange.from).toFixed(1)}
              </span>
            </div>
            
            <ProgressBar 
              value={review.healthIndexChange.to} 
              color="from-green-400 to-emerald-600"
              height="sm"
            />
          </div>
        </GlassCard>

        {/* Adherence Index Change */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-medium text-gray-400">Adherence</h3>
            {review.adherenceIndexChange.to >= review.adherenceIndexChange.from ? (
              <TrendingUp className="w-5 h-5 text-green-400" />
            ) : (
              <TrendingDown className="w-5 h-5 text-red-400" />
            )}
          </div>
          
          <div className="space-y-2">
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-bold text-white">
                {Math.round(review.adherenceIndexChange.to)}
              </span>
              <span className={cn(
                'text-sm font-medium',
                review.adherenceIndexChange.to >= review.adherenceIndexChange.from
                  ? 'text-green-400'
                  : 'text-red-400'
              )}>
                {review.adherenceIndexChange.to >= review.adherenceIndexChange.from ? '+' : ''}
                {(review.adherenceIndexChange.to - review.adherenceIndexChange.from).toFixed(1)}
              </span>
            </div>
            
            <ProgressBar 
              value={review.adherenceIndexChange.to} 
              color="from-blue-400 to-cyan-600"
              height="sm"
            />
          </div>
        </GlassCard>

        {/* Wealth Health Change */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-medium text-gray-400">Wealth Health</h3>
            {review.wealthHealthChange.to >= review.wealthHealthChange.from ? (
              <TrendingUp className="w-5 h-5 text-green-400" />
            ) : (
              <TrendingDown className="w-5 h-5 text-red-400" />
            )}
          </div>
          
          <div className="space-y-2">
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-bold text-white">
                {Math.round(review.wealthHealthChange.to)}
              </span>
              <span className={cn(
                'text-sm font-medium',
                review.wealthHealthChange.to >= review.wealthHealthChange.from
                  ? 'text-green-400'
                  : 'text-red-400'
              )}>
                {review.wealthHealthChange.to >= review.wealthHealthChange.from ? '+' : ''}
                {(review.wealthHealthChange.to - review.wealthHealthChange.from).toFixed(1)}
              </span>
            </div>
            
            <ProgressBar 
              value={review.wealthHealthChange.to} 
              color="from-amber-400 to-orange-600"
              height="sm"
            />
          </div>
        </GlassCard>

        {/* Longevity Change */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-medium text-gray-400">Years Added</h3>
            {review.longevityChange.to >= review.longevityChange.from ? (
              <TrendingUp className="w-5 h-5 text-green-400" />
            ) : (
              <TrendingDown className="w-5 h-5 text-red-400" />
            )}
          </div>
          
          <div className="space-y-2">
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-bold text-white">
                +{review.longevityChange.to.toFixed(1)}
              </span>
              <span className="text-sm text-gray-400">years</span>
            </div>
            
            <p className={cn(
              'text-sm font-medium',
              review.longevityChange.to >= review.longevityChange.from
                ? 'text-green-400'
                : 'text-red-400'
            )}>
              {review.longevityChange.to >= review.longevityChange.from ? '+' : ''}
              {(review.longevityChange.to - review.longevityChange.from).toFixed(1)} vs last week
            </p>
          </div>
        </GlassCard>
      </div>

      {/* Streaks Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Top Streaks */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center gap-2 mb-4">
            <Target className="w-5 h-5 text-green-400" />
            <h3 className="text-lg font-semibold text-white">Top Streaks</h3>
          </div>
          
          <div className="space-y-3">
            {review.topStreaks.length === 0 ? (
              <p className="text-gray-400 text-sm text-center py-8">
                No active streaks yet. Start building consistency!
              </p>
            ) : (
              review.topStreaks.map((streak, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between p-3 rounded-lg bg-green-500/10 border border-green-500/20"
                >
                  <span className="text-white font-medium">{streak.taskTitle}</span>
                  <Badge variant="success">
                    🔥 {streak.length} days
                  </Badge>
                </div>
              ))
            )}
          </div>
        </GlassCard>

        {/* At-Risk Streaks */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center gap-2 mb-4">
            <AlertTriangle className="w-5 h-5 text-amber-400" />
            <h3 className="text-lg font-semibold text-white">At-Risk Streaks</h3>
          </div>
          
          <div className="space-y-3">
            {review.atRiskStreaks.length === 0 ? (
              <p className="text-gray-400 text-sm text-center py-8">
                All streaks are on track! Keep it up!
              </p>
            ) : (
              review.atRiskStreaks.map((streak, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between p-3 rounded-lg bg-amber-500/10 border border-amber-500/20"
                >
                  <span className="text-white font-medium">{streak.taskTitle}</span>
                  <Badge variant="warning">
                    ⚠️ {streak.consecutiveMisses} misses
                  </Badge>
                </div>
              ))
            )}
          </div>
        </GlassCard>
      </div>

      {/* Focus Actions */}
      <GlassCard variant="elevated" className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <Calendar className="w-5 h-5 text-purple-400" />
          <h3 className="text-lg font-semibold text-white">Focus Actions for Next Week</h3>
        </div>
        
        <ul className="space-y-3">
          {review.focusActions.map((action, idx) => (
            <li
              key={idx}
              className="flex items-start gap-3 p-4 rounded-lg bg-gray-800/50 border border-gray-700/50"
            >
              <span className="flex-shrink-0 w-6 h-6 rounded-full bg-purple-500/20 text-purple-400 flex items-center justify-center text-sm font-bold">
                {idx + 1}
              </span>
              <span className="text-gray-200">{action}</span>
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
