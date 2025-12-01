import { useNavigate } from 'react-router-dom';
import {
  Heart,
  Brain,
  Briefcase,
  DollarSign,
  Users,
  Gamepad2,
  TrendingUp,
  Globe,
  Palette,
  Home,
  type LucideIcon,
} from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { ProgressBar } from '@components/atoms/ProgressBar';
import { cn } from '@utils/cn';

interface DimensionCardProps {
  dimension: {
    id: string;
    name: string;
  };
  score: number;
  trend?: number;
  activeMilestones?: number;
  className?: string;
}

const dimensionIcons: Record<string, LucideIcon> = {
  health: Heart,
  mind: Brain,
  work: Briefcase,
  money: DollarSign,
  relationships: Users,
  play: Gamepad2,
  growth: TrendingUp,
  community: Globe,
  create: Palette,
  assets: Home,
};

const dimensionColors: Record<string, string> = {
  health: '#22c55e',
  mind: '#8b5cf6',
  work: '#f97316',
  money: '#eab308',
  relationships: '#ec4899',
  play: '#22d3ee',
  growth: '#6366f1',
  community: '#14b8a6',
  create: '#a855f7',
  assets: '#64748b',
};

export function DimensionCard({
  dimension,
  score,
  trend = 0,
  activeMilestones = 0,
  className,
}: DimensionCardProps) {
  const navigate = useNavigate();
  const Icon = dimensionIcons[dimension.id] || Globe;
  const color = dimensionColors[dimension.id] || '#6b7280';

  const handleClick = () => {
    navigate(`/dimensions/${dimension.id}`);
  };

  return (
    <GlassCard
      variant="default"
      className={cn(
        'p-4 hover:shadow-glow-sm transition-all group',
        className
      )}
      onClick={handleClick}
    >
      <div className="flex items-center gap-3 mb-3">
        <div
          className="p-2 rounded-lg transition-colors"
          style={{ backgroundColor: `${color}20` }}
        >
          <Icon
            className="w-5 h-5 transition-transform group-hover:scale-110"
            style={{ color }}
          />
        </div>
        <span className="font-medium text-text-primary">{dimension.name}</span>
      </div>
      
      <div className="flex items-end justify-between mb-3">
        <span className="text-2xl font-bold text-text-primary">{score}</span>
        {trend !== 0 && (
          <span
            className={cn(
              'text-sm font-medium',
              trend > 0 ? 'text-semantic-success' : 'text-semantic-error'
            )}
          >
            {trend > 0 ? '+' : ''}{trend}%
          </span>
        )}
      </div>
      
      <ProgressBar value={score} color={color} height="sm" />
      
      {activeMilestones > 0 && (
        <p className="mt-2 text-xs text-text-tertiary">
          {activeMilestones} active milestone{activeMilestones !== 1 ? 's' : ''}
        </p>
      )}
    </GlassCard>
  );
}

// Grid component for displaying all 8 dimensions
interface DimensionGridProps {
  dimensions: Array<{
    id: string;
    name: string;
    score: number;
    trend?: number;
    activeMilestones?: number;
  }>;
  className?: string;
}

export function DimensionGrid({ dimensions, className }: DimensionGridProps) {
  return (
    <div className={cn('grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4', className)}>
      {dimensions.map((dim) => (
        <DimensionCard
          key={dim.id}
          dimension={{ id: dim.id, name: dim.name }}
          score={dim.score}
          trend={dim.trend}
          activeMilestones={dim.activeMilestones}
        />
      ))}
    </div>
  );
}
