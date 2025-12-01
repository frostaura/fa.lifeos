import { GlassCard } from '@components/atoms/GlassCard';
import { StreakBadge } from '@components/molecules/StreakBadge';
import { Flame } from 'lucide-react';
import { cn } from '@utils/cn';
import type { Streak } from '@/types';

interface StreaksWidgetProps {
  streaks: Streak[];
  className?: string;
  maxItems?: number;
}

export function StreaksWidget({ streaks, className, maxItems = 5 }: StreaksWidgetProps) {
  const displayedStreaks = streaks
    .sort((a, b) => b.currentDays - a.currentDays)
    .slice(0, maxItems);

  return (
    <GlassCard variant="default" className={cn('p-6', className)}>
      <div className="flex items-center gap-2 mb-4">
        <Flame className="w-5 h-5 text-accent-orange" />
        <h2 className="text-lg font-semibold text-text-primary">Active Streaks</h2>
      </div>
      
      {displayedStreaks.length === 0 ? (
        <p className="text-text-tertiary text-sm">No active streaks yet. Start a habit!</p>
      ) : (
        <div className="space-y-3">
          {displayedStreaks.map((streak) => (
            <div
              key={streak.id}
              className="flex items-center justify-between py-2 border-b border-glass-border last:border-0"
            >
              <span className="text-text-primary font-medium">{streak.name}</span>
              <StreakBadge days={streak.currentDays} />
            </div>
          ))}
        </div>
      )}
      
      {streaks.length > maxItems && (
        <button className="mt-4 text-sm text-accent-purple hover:text-accent-purple/80 transition-colors">
          View all {streaks.length} streaks
        </button>
      )}
    </GlassCard>
  );
}
