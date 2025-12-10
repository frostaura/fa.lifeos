import { Link } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { BarChart2, ArrowRight, Loader2, TrendingUp, TrendingDown } from 'lucide-react';
import { useGetMetricDefinitionsQuery } from '@/services';
import type { MetricDefinition } from '@/services';

interface LinkedMetricsSectionProps {
  dimensionId: string;
  dimensionColor: string;
}

export function LinkedMetricsSection({ dimensionId, dimensionColor }: LinkedMetricsSectionProps) {
  const { data: metrics, isLoading, error } = useGetMetricDefinitionsQuery();

  // Filter metrics for this dimension
  const dimensionMetrics = metrics?.filter(m => m.dimensionId === dimensionId) || [];

  const calculateProgress = (metric: MetricDefinition): number => {
    if (!metric.targetValue || metric.latestValue === undefined || metric.latestValue === null) {
      return 0;
    }
    
    if (metric.targetDirection === 'AtOrBelow') {
      // For "at or below" targets (e.g., weight loss)
      // Progress is how close we are to the target from above
      if (metric.latestValue <= metric.targetValue) return 100;
      // Assume a reasonable max range above target
      const startValue = metric.targetValue * 1.5;
      const progress = ((startValue - metric.latestValue) / (startValue - metric.targetValue)) * 100;
      return Math.max(0, Math.min(100, progress));
    } else {
      // For "at or above" targets (e.g., steps)
      return Math.min(100, (metric.latestValue / metric.targetValue) * 100);
    }
  };

  const getProgressColor = (progress: number): string => {
    if (progress >= 100) return 'bg-green-500';
    if (progress >= 75) return 'bg-green-500';
    if (progress >= 50) return 'bg-blue-500';
    if (progress >= 25) return 'bg-yellow-500';
    return 'bg-gray-500';
  };

  if (isLoading) {
    return (
      <GlassCard variant="elevated" className="p-6">
        <div className="flex items-center justify-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-accent-primary" />
        </div>
      </GlassCard>
    );
  }

  if (error) {
    return (
      <GlassCard variant="elevated" className="p-6">
        <p className="text-red-500 text-center">Failed to load metrics</p>
      </GlassCard>
    );
  }

  if (dimensionMetrics.length === 0) {
    return null; // Don't show section if no metrics linked
  }

  return (
    <GlassCard variant="elevated" className="p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <BarChart2 className="w-5 h-5" style={{ color: dimensionColor }} />
          <h2 className="text-xl font-semibold text-text-primary">Metrics</h2>
        </div>
        <Link
          to="/metrics"
          className="flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary transition-colors"
        >
          View All
          <ArrowRight className="w-4 h-4" />
        </Link>
      </div>

      <div className="space-y-3">
        {dimensionMetrics.map((metric) => {
          const progress = calculateProgress(metric);
          const hasTarget = metric.targetValue !== undefined && metric.targetValue !== null;
          const hasValue = metric.latestValue !== undefined && metric.latestValue !== null;

          return (
            <div
              key={metric.id}
              className="p-3 bg-glass-light rounded-lg"
            >
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <span className="text-text-primary font-medium">{metric.name}</span>
                  {metric.targetDirection === 'AtOrBelow' ? (
                    <TrendingDown className="w-3 h-3 text-blue-400" />
                  ) : (
                    <TrendingUp className="w-3 h-3 text-green-400" />
                  )}
                </div>
                {hasValue && (
                  <span className="text-text-secondary text-sm">
                    {typeof metric.latestValue === 'number' 
                      ? metric.latestValue.toLocaleString() 
                      : metric.latestValue}
                    {metric.unit && ` ${metric.unit}`}
                  </span>
                )}
              </div>

              {hasTarget && (
                <>
                  <div className="flex items-center justify-between text-xs text-text-tertiary mb-1">
                    <span>
                      Target: {metric.targetDirection === 'AtOrBelow' ? '≤' : '≥'} {metric.targetValue?.toLocaleString()} {metric.unit}
                    </span>
                    <span>{Math.round(progress)}%</span>
                  </div>
                  <div className="h-2 bg-glass-medium rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all duration-500 ${getProgressColor(progress)}`}
                      style={{ width: `${progress}%` }}
                    />
                  </div>
                </>
              )}

              {!hasTarget && !hasValue && (
                <p className="text-xs text-text-tertiary">No data recorded yet</p>
              )}
            </div>
          );
        })}
      </div>
    </GlassCard>
  );
}
