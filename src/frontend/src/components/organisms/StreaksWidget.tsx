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
    <GlassCard variant="default" className={cn('p-4 md:p-6', className)}>
      <div className="flex items-center gap-2 mb-3 md:mb-4">
        <Flame className="w-4 h-4 md:w-5 md:h-5 text-accent-orange" />
        <h2 className="text-base md:text-lg font-semibold text-text-primary whitespace-nowrap">Active Streaks</h2>
      </div>
      
      {displayedStreaks.length === 0 ? (
        <p className="text-text-tertiary text-xs md:text-sm">No active streaks yet. Start a habit!</p>
      ) : (
        <div className="space-y-2 md:space-y-3">
          {displayedStreaks.map((streak) => (
            <div
              key={streak.id}
              className="flex items-center justify-between py-1.5 md:py-2 border-b border-glass-border last:border-0"
            >
              <span className="text-text-primary font-medium text-xs md:text-sm whitespace-nowrap overflow-hidden text-ellipsis">{streak.name}</span>
              <StreakBadge days={streak.currentDays} />
            </div>
          ))}
        </div>
      )}
      
      {streaks.length > maxItems && (
        <button className="mt-3 md:mt-4 text-xs md:text-sm text-accent-purple hover:text-accent-purple/80 transition-colors whitespace-nowrap">
          View all {streaks.length} streaks
        </button>
      )}
    </GlassCard>
  );
}
