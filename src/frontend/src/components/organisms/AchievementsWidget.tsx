import { useState, useEffect } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { 
  Trophy, 
  Flame, 
  Target, 
  Wallet,
  Star,
  Crown,
  Activity,
  Calendar,
  TrendingUp,
  Lock,
  Check
} from 'lucide-react';
import { cn } from '@utils/cn';

interface Achievement {
  code: string;
  name: string;
  description: string;
  icon: string;
  tier: string;
  xpValue: number;
  unlocked: boolean;
  unlockedAt: string | null;
}

interface UserXP {
  totalXp: number;
  level: number;
  weeklyXp: number;
  xpToNextLevel: number;
}

const iconMap: Record<string, React.ReactNode> = {
  trophy: <Trophy className="w-5 h-5" />,
  flame: <Flame className="w-5 h-5" />,
  target: <Target className="w-5 h-5" />,
  wallet: <Wallet className="w-5 h-5" />,
  star: <Star className="w-5 h-5" />,
  crown: <Crown className="w-5 h-5" />,
  activity: <Activity className="w-5 h-5" />,
  calendar: <Calendar className="w-5 h-5" />,
  'trending-up': <TrendingUp className="w-5 h-5" />,
};

const tierColors: Record<string, string> = {
  bronze: 'from-amber-700 to-amber-500',
  silver: 'from-gray-400 to-gray-300',
  gold: 'from-yellow-500 to-yellow-300',
  platinum: 'from-cyan-400 to-cyan-200',
  diamond: 'from-purple-400 to-pink-400',
};

const tierBgColors: Record<string, string> = {
  bronze: 'bg-amber-500/20 border-amber-500/30',
  silver: 'bg-gray-400/20 border-gray-400/30',
  gold: 'bg-yellow-500/20 border-yellow-500/30',
  platinum: 'bg-cyan-400/20 border-cyan-400/30',
  diamond: 'bg-purple-400/20 border-purple-400/30',
};

interface AchievementBadgeProps {
  achievement: Achievement;
  compact?: boolean;
}

function AchievementBadge({ achievement, compact = false }: AchievementBadgeProps) {
  const icon = iconMap[achievement.icon] || <Trophy className="w-5 h-5" />;
  const tierColor = tierColors[achievement.tier] || tierColors.bronze;
  const tierBg = tierBgColors[achievement.tier] || tierBgColors.bronze;

  if (compact) {
    return (
      <div 
        className={cn(
          'relative p-2 rounded-lg border transition-all cursor-pointer group',
          achievement.unlocked ? tierBg : 'bg-background-hover/50 border-border-subtle opacity-50'
        )}
        title={`${achievement.name}: ${achievement.description}`}
      >
        <div className={cn(
          'transition-colors',
          achievement.unlocked ? `bg-gradient-to-br ${tierColor} bg-clip-text text-transparent` : 'text-text-tertiary'
        )}>
          {icon}
        </div>
        {!achievement.unlocked && (
          <Lock className="absolute -top-1 -right-1 w-3 h-3 text-text-tertiary" />
        )}
      </div>
    );
  }

  return (
    <div 
      className={cn(
        'flex items-center gap-3 p-3 rounded-lg border transition-all',
        achievement.unlocked ? tierBg : 'bg-background-hover/30 border-border-subtle opacity-60'
      )}
    >
      <div className={cn(
        'p-2 rounded-lg',
        achievement.unlocked ? `bg-gradient-to-br ${tierColor}` : 'bg-background-hover'
      )}>
        <div className={achievement.unlocked ? 'text-white' : 'text-text-tertiary'}>
          {icon}
        </div>
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className={cn(
            'font-medium text-sm truncate',
            achievement.unlocked ? 'text-text-primary' : 'text-text-tertiary'
          )}>
            {achievement.name}
          </span>
          {achievement.unlocked && (
            <Check className="w-4 h-4 text-semantic-success flex-shrink-0" />
          )}
        </div>
        <p className="text-xs text-text-tertiary truncate">{achievement.description}</p>
      </div>
      <div className="text-right">
        <span className="text-xs font-medium text-accent-cyan">+{achievement.xpValue} XP</span>
        <p className="text-xs text-text-tertiary capitalize">{achievement.tier}</p>
      </div>
    </div>
  );
}

interface XPBarProps {
  xp: UserXP;
}

function XPBar({ xp }: XPBarProps) {
  const progress = xp.xpToNextLevel > 0 
    ? Math.min(100, (1 - (xp.xpToNextLevel / (xp.totalXp + xp.xpToNextLevel))) * 100)
    : 100;

  return (
    <div className="flex items-center gap-4 p-4 bg-background-hover/50 rounded-lg">
      <div className="flex items-center justify-center w-12 h-12 rounded-full bg-gradient-to-br from-accent-purple to-accent-cyan text-white font-bold text-lg">
        {xp.level}
      </div>
      <div className="flex-1">
        <div className="flex items-center justify-between mb-1">
          <span className="text-sm font-medium text-text-primary">Level {xp.level}</span>
          <span className="text-xs text-text-tertiary">{xp.totalXp.toLocaleString()} XP</span>
        </div>
        <div className="h-2 bg-background-card rounded-full overflow-hidden">
          <div 
            className="h-full bg-gradient-to-r from-accent-purple to-accent-cyan transition-all duration-500"
            style={{ width: `${progress}%` }}
          />
        </div>
        <p className="text-xs text-text-tertiary mt-1">
          {xp.xpToNextLevel > 0 ? `${xp.xpToNextLevel} XP to next level` : 'Max level reached!'}
        </p>
      </div>
      <div className="text-right">
        <span className="text-lg font-bold text-accent-cyan">{xp.weeklyXp}</span>
        <p className="text-xs text-text-tertiary">This week</p>
      </div>
    </div>
  );
}

interface AchievementsWidgetProps {
  compact?: boolean;
  maxItems?: number;
}

export function AchievementsWidget({ compact = false, maxItems = 6 }: AchievementsWidgetProps) {
  const [loading, setLoading] = useState(true);
  const [achievements, setAchievements] = useState<Achievement[]>([]);
  const [xp, setXp] = useState<UserXP | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const token = localStorage.getItem('accessToken');
        const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};

        const [achievementsRes, xpRes] = await Promise.all([
          fetch('/api/achievements', { headers }),
          fetch('/api/achievements/xp', { headers })
        ]);

        if (achievementsRes.ok) {
          const achievementsData = await achievementsRes.json();
          setAchievements(achievementsData.data);
        }

        if (xpRes.ok) {
          const xpData = await xpRes.json();
          setXp(xpData.data);
        }
      } catch (err) {
        setError('Failed to load achievements');
        console.error('Failed to fetch achievements:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-center h-32">
          <Spinner size="md" />
        </div>
      </GlassCard>
    );
  }

  if (error) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <Trophy className="w-5 h-5 text-accent-purple" />
          <h2 className="text-lg font-semibold text-text-primary">Achievements</h2>
        </div>
        <p className="text-text-tertiary text-sm text-center py-4">{error}</p>
      </GlassCard>
    );
  }

  const unlockedCount = achievements.filter(a => a.unlocked).length;
  const displayAchievements = compact 
    ? achievements.slice(0, maxItems)
    : achievements;

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Trophy className="w-5 h-5 text-accent-purple" />
          <h2 className="text-lg font-semibold text-text-primary">Achievements</h2>
        </div>
        <span className="text-sm text-text-tertiary">
          {unlockedCount}/{achievements.length} unlocked
        </span>
      </div>

      {xp && <XPBar xp={xp} />}

      <div className={cn(
        'mt-4',
        compact ? 'flex flex-wrap gap-2' : 'space-y-2'
      )}>
        {displayAchievements.map((achievement) => (
          <AchievementBadge 
            key={achievement.code} 
            achievement={achievement} 
            compact={compact}
          />
        ))}
      </div>

      {compact && achievements.length > maxItems && (
        <p className="text-xs text-text-tertiary text-center mt-3">
          +{achievements.length - maxItems} more achievements
        </p>
      )}
    </GlassCard>
  );
}
