import { cn } from '@utils/cn';

interface StreakBadgeProps {
  days: number;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

export function StreakBadge({ days, size = 'md', className }: StreakBadgeProps) {
  const sizeStyles = {
    sm: 'text-sm',
    md: 'text-base',
    lg: 'text-lg',
  };

  const isHot = days >= 3;
  const isOnFire = days >= 7;
  const isLegendary = days >= 30;

  const getColor = () => {
    if (isLegendary) return 'text-accent-purple';
    if (isOnFire) return 'text-accent-orange';
    if (isHot) return 'text-semantic-warning';
    return 'text-text-tertiary';
  };

  return (
    <span className={cn('font-bold', sizeStyles[size], getColor(), className)}>
      🔥 {days} {days === 1 ? 'day' : 'days'}
    </span>
  );
}
