import { cn } from '@utils/cn';

interface ProgressBarProps {
  value: number; // 0-100
  color?: string;
  className?: string;
  height?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
}

const heights = {
  sm: 'h-1',
  md: 'h-2',
  lg: 'h-3',
};

export function ProgressBar({
  value,
  color = '#8b5cf6',
  className,
  height = 'md',
  showLabel = false,
}: ProgressBarProps) {
  const clampedValue = Math.min(100, Math.max(0, value));

  return (
    <div className={cn('w-full', className)}>
      {showLabel && (
        <div className="flex justify-between text-xs text-text-tertiary mb-1">
          <span>Progress</span>
          <span>{Math.round(clampedValue)}%</span>
        </div>
      )}
      <div className={cn('w-full bg-glass-light rounded-full overflow-hidden', heights[height])}>
        <div
          className="h-full rounded-full transition-all duration-500 ease-out"
          style={{
            width: `${clampedValue}%`,
            backgroundColor: color,
          }}
        />
      </div>
    </div>
  );
}
