import { useMemo } from 'react';
import { LineChart, Line, ReferenceLine, ResponsiveContainer, YAxis } from 'recharts';

interface MetricSparklineProps {
  data: Array<{ timestamp: string; value: number }>;
  targetValue?: number;
  height?: number;
  width?: number;
  color?: string;
  showTarget?: boolean;
  className?: string;
}

export function MetricSparkline({
  data,
  targetValue,
  height = 60,
  width,
  color = '#22c55e',
  showTarget = true,
  className = '',
}: MetricSparklineProps) {
  // Sort data chronologically for proper visualization
  const sortedData = useMemo(
    () =>
      [...data].sort(
        (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
      ),
    [data]
  );

  // Calculate Y-axis domain to include both data and target value
  const yDomain = useMemo(() => {
    if (sortedData.length === 0) return ['auto', 'auto'];

    const values = sortedData.map((d) => d.value);
    let min = Math.min(...values);
    let max = Math.max(...values);

    // Include target value in the domain if present
    if (showTarget && targetValue !== undefined) {
      min = Math.min(min, targetValue);
      max = Math.max(max, targetValue);
    }

    // Add 5% padding to the domain
    const padding = (max - min) * 0.05;
    return [min - padding, max + padding];
  }, [sortedData, targetValue, showTarget]);

  if (sortedData.length === 0) {
    return null;
  }

  return (
    <ResponsiveContainer width={width ?? '100%'} height={height} className={className}>
      <LineChart data={sortedData} margin={{ top: 5, right: 5, bottom: 5, left: 5 }}>
        <YAxis domain={yDomain} hide />
        <Line
          type="monotone"
          dataKey="value"
          stroke={color}
          strokeWidth={2}
          dot={false}
          activeDot={false}
        />
        {showTarget && targetValue !== undefined && (
          <ReferenceLine
            y={targetValue}
            stroke="#a855f7"
            strokeDasharray="4 4"
            strokeWidth={1.5}
          />
        )}
      </LineChart>
    </ResponsiveContainer>
  );
}
