import { useMemo } from 'react';
import { LineChart, Line, ReferenceLine, ResponsiveContainer, YAxis, XAxis } from 'recharts';

// Format numbers compactly (e.g., 10500 -> 10.5k)
function formatCompact(value: number): string {
  if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
  if (value >= 1000) return `${(value / 1000).toFixed(1)}k`;
  if (Number.isInteger(value)) return value.toString();
  return value.toFixed(1);
}

export type TargetDirection = 'AtOrAbove' | 'AtOrBelow';

interface MetricSparklineProps {
  data: Array<{ timestamp: string; value: number }>;
  targetValue?: number;
  targetDirection?: TargetDirection;
  currentValue?: number; // The actual latest value (from metric definition, not chart data)
  height?: number;
  width?: number;
  color?: string;
  showTarget?: boolean;
  showLabels?: boolean;
  className?: string;
}

export function MetricSparkline({
  data,
  targetValue,
  targetDirection = 'AtOrAbove',
  currentValue,
  height = 60,
  width,
  color,
  showTarget = true,
  showLabels = false,
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

  // Calculate if current value meets target
  // Use currentValue prop if provided, otherwise fall back to last chart point
  const { isOnTarget, latestValue } = useMemo(() => {
    if (targetValue === undefined) {
      return { isOnTarget: null, latestValue: null };
    }
    
    // Use provided currentValue, or fall back to last point in chart data
    const valueToCheck = currentValue ?? (sortedData.length > 0 ? sortedData[sortedData.length - 1].value : null);
    
    if (valueToCheck === null) {
      return { isOnTarget: null, latestValue: null };
    }
    
    const onTarget = targetDirection === 'AtOrAbove' 
      ? valueToCheck >= targetValue 
      : valueToCheck <= targetValue;
    return { isOnTarget: onTarget, latestValue: valueToCheck };
  }, [sortedData, targetValue, targetDirection, currentValue]);

  // Determine line color based on target status
  const lineColor = useMemo(() => {
    if (color) return color; // Use explicit color if provided
    if (isOnTarget === null) return '#22c55e'; // Default green if no target
    return isOnTarget ? '#22c55e' : '#ef4444'; // Green if on target, red if off
  }, [color, isOnTarget]);

  // Calculate Y-axis domain to include both data and target value
  const { yDomain, minValue, maxValue } = useMemo(() => {
    if (sortedData.length === 0) return { yDomain: ['auto', 'auto'] as const, minValue: 0, maxValue: 100 };

    const values = sortedData.map((d) => d.value);
    let min = Math.min(...values);
    let max = Math.max(...values);

    // Include target value in the domain if present
    if (showTarget && targetValue !== undefined) {
      min = Math.min(min, targetValue);
      max = Math.max(max, targetValue);
    }

    // Include 0 in the domain for context
    if (showLabels) {
      min = Math.min(min, 0);
    }

    // Add 10% padding to the domain
    const padding = (max - min) * 0.1;
    return {
      yDomain: [min - padding, max + padding] as [number, number],
      minValue: min,
      maxValue: max,
    };
  }, [sortedData, targetValue, showTarget, showLabels]);

  if (sortedData.length === 0) {
    return null;
  }

  // Target direction indicator
  const directionSymbol = targetDirection === 'AtOrAbove' ? '≥' : '≤';

  return (
    <div className={`relative ${className}`}>
      <ResponsiveContainer width={width ?? '100%'} height={height}>
        <LineChart data={sortedData} margin={{ top: 5, right: showLabels ? 45 : 5, bottom: 5, left: showLabels ? 5 : 5 }}>
          <YAxis domain={yDomain} hide />
          <XAxis dataKey="timestamp" hide />
          {/* Zero line for reference */}
          {showLabels && minValue <= 0 && maxValue >= 0 && (
            <ReferenceLine
              y={0}
              stroke="rgba(255,255,255,0.2)"
              strokeWidth={1}
            />
          )}
          <Line
            type="monotone"
            dataKey="value"
            stroke={lineColor}
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
      {/* Labels overlay */}
      {showLabels && (
        <div className="absolute right-0 top-0 bottom-0 flex flex-col justify-between text-[9px] text-text-tertiary pointer-events-none" style={{ width: '45px' }}>
          <span className="text-right truncate">{formatCompact(maxValue)}</span>
          {targetValue !== undefined && (
            <span className={`text-right truncate ${isOnTarget ? 'text-semantic-success' : 'text-semantic-error'}`}>
              {directionSymbol}{formatCompact(targetValue)}
            </span>
          )}
          <span className="text-right">0</span>
        </div>
      )}
    </div>
  );
}
