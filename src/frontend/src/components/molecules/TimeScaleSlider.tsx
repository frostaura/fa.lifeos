export interface TimeRange {
  label: string;
  value: string;
  days: number;
}

interface TimeScaleSliderProps {
  value: string;
  onChange: (range: TimeRange) => void;
  ranges?: TimeRange[];
  className?: string;
}

const DEFAULT_RANGES: TimeRange[] = [
  { label: '7D', value: '7d', days: 7 },
  { label: '30D', value: '30d', days: 30 },
  { label: '90D', value: '90d', days: 90 },
  { label: '1Y', value: '1y', days: 365 },
  { label: '5Y', value: '5y', days: 1825 },
];

export function TimeScaleSlider({
  value,
  onChange,
  ranges = DEFAULT_RANGES,
  className = '',
}: TimeScaleSliderProps) {
  return (
    <div className={`flex gap-2 ${className}`}>
      {ranges.map((range) => (
        <button
          key={range.value}
          onClick={() => onChange(range)}
          className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors backdrop-blur-md
            ${
              value === range.value
                ? 'bg-accent-purple text-white'
                : 'bg-glass-light text-text-secondary hover:bg-glass-lighter border border-glass-border'
            }`}
        >
          {range.label}
        </button>
      ))}
    </div>
  );
}
