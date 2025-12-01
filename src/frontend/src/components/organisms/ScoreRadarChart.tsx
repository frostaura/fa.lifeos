import {
  ResponsiveContainer,
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
  Tooltip,
} from 'recharts';
import { GlassCard } from '@components/atoms/GlassCard';
import { cn } from '@utils/cn';
import type { DimensionId } from '@/types';

interface ScoreRadarChartProps {
  data: Array<{
    dimension: DimensionId;
    name: string;
    score: number;
  }>;
  showCard?: boolean;
  height?: number;
  className?: string;
}

interface TooltipPayloadItem {
  value: number;
  payload: { name: string };
}

interface CustomTooltipProps {
  active?: boolean;
  payload?: TooltipPayloadItem[];
}

function CustomTooltip({ active, payload }: CustomTooltipProps) {
  if (!active || !payload?.length) return null;

  return (
    <div className="bg-background-tertiary border border-glass-border rounded-lg p-3 shadow-lg">
      <p className="text-text-tertiary text-xs mb-1">{payload[0].payload.name}</p>
      <p className="text-text-primary font-semibold">{payload[0].value}/100</p>
    </div>
  );
}

export function ScoreRadarChart({
  data,
  showCard = false,
  height = 350,
  className,
}: ScoreRadarChartProps) {
  const chartContent = (
    <ResponsiveContainer width="100%" height={height}>
      <RadarChart data={data} margin={{ top: 20, right: 30, bottom: 20, left: 30 }}>
        <defs>
          <linearGradient id="radarGradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#8b5cf6" stopOpacity={0.8} />
            <stop offset="100%" stopColor="#22d3ee" stopOpacity={0.3} />
          </linearGradient>
        </defs>
        <PolarGrid 
          stroke="rgba(255,255,255,0.1)" 
          gridType="polygon"
        />
        <PolarAngleAxis
          dataKey="name"
          tick={{ fill: '#a0a0b0', fontSize: 12 }}
        />
        <PolarRadiusAxis
          angle={22.5}
          domain={[0, 100]}
          tick={{ fill: '#6b6b7a', fontSize: 10 }}
          axisLine={false}
        />
        <Tooltip content={<CustomTooltip />} />
        <Radar
          name="Score"
          dataKey="score"
          stroke="#8b5cf6"
          strokeWidth={2}
          fill="url(#radarGradient)"
          fillOpacity={0.6}
        />
      </RadarChart>
    </ResponsiveContainer>
  );

  if (!showCard) {
    return <div className={className}>{chartContent}</div>;
  }

  return (
    <GlassCard variant="default" className={cn('p-6', className)}>
      <h3 className="text-lg font-semibold text-text-primary mb-4">Dimension Overview</h3>
      {chartContent}
    </GlassCard>
  );
}
