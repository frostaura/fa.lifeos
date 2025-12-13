import { useMemo } from 'react';
import { RadarChart, PolarGrid, PolarAngleAxis, PolarRadiusAxis, Radar, Legend, ResponsiveContainer } from 'recharts';

interface IdentityRadarProps {
  currentStats: Record<string, number>;
  targetStats: Record<string, number>;
  size?: 'sm' | 'md' | 'lg';
}

const STAT_LABELS: Record<string, string> = {
  strength: 'Strength',
  wisdom: 'Wisdom',
  charisma: 'Charisma',
  composure: 'Composure',
  energy: 'Energy',
  influence: 'Influence',
  vitality: 'Vitality',
};

const STAT_ORDER = ['strength', 'wisdom', 'charisma', 'composure', 'energy', 'influence', 'vitality'];

export function IdentityRadar({ currentStats, targetStats, size = 'md' }: IdentityRadarProps) {
  const data = useMemo(() => {
    return STAT_ORDER.map(stat => ({
      stat: STAT_LABELS[stat] || stat,
      current: currentStats[stat] ?? 0,
      target: targetStats[stat] ?? 75,
      fullMark: 100,
    }));
  }, [currentStats, targetStats]);

  const heights = {
    sm: 200,
    md: 300,
    lg: 400,
  };

  const height = heights[size];

  return (
    <div className="w-full" style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        <RadarChart cx="50%" cy="50%" outerRadius="70%" data={data}>
          <PolarGrid stroke="var(--border-glass)" strokeOpacity={0.5} />
          <PolarAngleAxis 
            dataKey="stat" 
            tick={{ fill: 'var(--text-secondary)', fontSize: size === 'sm' ? 10 : 12 }}
          />
          <PolarRadiusAxis 
            angle={90} 
            domain={[0, 100]} 
            tick={{ fill: 'var(--text-muted)', fontSize: 10 }}
            axisLine={false}
          />
          <Radar
            name="Target"
            dataKey="target"
            stroke="#8b5cf6"
            fill="transparent"
            strokeWidth={2}
            strokeDasharray="5 5"
          />
          <Radar
            name="Current"
            dataKey="current"
            stroke="#22d3ee"
            fill="#22d3ee"
            fillOpacity={0.2}
            strokeWidth={2}
          />
          <Legend 
            wrapperStyle={{ 
              paddingTop: '10px',
              fontSize: size === 'sm' ? '10px' : '12px',
              color: 'var(--text-secondary)'
            }}
          />
        </RadarChart>
      </ResponsiveContainer>
    </div>
  );
}

export default IdentityRadar;
