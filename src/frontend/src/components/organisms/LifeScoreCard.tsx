import { GlassCard } from '@components/atoms/GlassCard';
import { ProgressRing } from '@components/atoms/ProgressRing';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { cn } from '@utils/cn';

interface LifeScoreCardProps {
  score: number;
  trend?: number;
  className?: string;
}

function getScoreColor(score: number): string {
  if (score >= 80) return '#22c55e'; // Green
  if (score >= 60) return '#8b5cf6'; // Purple
  if (score >= 40) return '#eab308'; // Yellow
  return '#ef4444'; // Red
}

export function LifeScoreCard({ score, trend = 0, className }: LifeScoreCardProps) {
  const TrendIcon = trend > 0 ? TrendingUp : trend < 0 ? TrendingDown : Minus;
  const trendColor = trend > 0 ? 'text-semantic-success' : trend < 0 ? 'text-semantic-error' : 'text-text-tertiary';

  return (
    <GlassCard variant="elevated" glow="accent" className={cn('p-6', className)}>
      <h2 className="text-lg font-semibold text-text-secondary mb-4">Life Score</h2>
      <div className="flex items-center justify-center">
        <ProgressRing
          progress={score}
          size={160}
          strokeWidth={10}
          gradientId="lifeScoreGradient"
        >
          <svg width="0" height="0" className="absolute">
            <defs>
              <linearGradient id="lifeScoreGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#8b5cf6" />
                <stop offset="50%" stopColor="#22d3ee" />
                <stop offset="100%" stopColor="#ec4899" />
              </linearGradient>
            </defs>
          </svg>
          <div className="flex flex-col items-center">
            <span 
              className="text-5xl font-bold transition-colors"
              style={{ color: getScoreColor(score) }}
            >
              {score}
            </span>
            <span className="text-text-tertiary text-sm">/ 100</span>
            {trend !== 0 && (
              <div className={cn('flex items-center gap-1 mt-1', trendColor)}>
                <TrendIcon className="w-4 h-4" />
                <span className="text-sm font-medium">{Math.abs(trend)}%</span>
              </div>
            )}
          </div>
        </ProgressRing>
      </div>
      <div 
        className="mt-4 text-center text-sm text-text-secondary"
        role="meter"
        aria-valuenow={score}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label="Life Score"
      >
        <span className="sr-only">Life Score: {score} out of 100</span>
      </div>
    </GlassCard>
  );
}
