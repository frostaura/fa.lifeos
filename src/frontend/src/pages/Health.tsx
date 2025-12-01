import { useState, useEffect, useMemo } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { MetricSparkline } from '@components/molecules/MetricSparkline';
import { TimeScaleSlider, type TimeRange } from '@components/molecules/TimeScaleSlider';
import { Activity, Moon, Footprints, Scale, Droplet, Heart } from 'lucide-react';
import { useGetMetricDefinitionsQuery, useGetMetricRecordsQuery, useGetMetricHistoryQuery, type MetricDefinition } from '@/services';

interface LongevityData {
  baselineLifeExpectancy: number;
  estimatedYearsAdded: number;
  adjustedLifeExpectancy: number;
  confidenceLevel: string;
  breakdown: Array<{
    modelCode: string;
    modelName: string;
    yearsAdded: number;
    inputValues: Record<string, number>;
  }>;
}

interface MetricDisplay {
  code: string;
  value: number | string | null;
  unit: string;
}

// Metric codes we want to display on the health page
const TRACKED_METRICS = ['weight_kg', 'body_fat_pct', 'steps', 'sleep_hours', 'resting_hr'];

export function Health() {
  const [longevityLoading, setLongevityLoading] = useState(true);
  const [longevity, setLongevity] = useState<LongevityData | null>(null);
  const [timeRange, setTimeRange] = useState<TimeRange>({ label: '30D', value: '30d', days: 30 });

  // Calculate date range for history query
  const toDate = useMemo(() => new Date().toISOString().split('T')[0], []);
  const fromDate = useMemo(
    () => new Date(Date.now() - timeRange.days * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    [timeRange.days]
  );

  // Fetch metric definitions for units
  const { data: definitions, isLoading: definitionsLoading } = useGetMetricDefinitionsQuery();

  // Fetch metric history for sparklines
  const { data: historyData, isLoading: historyLoading } = useGetMetricHistoryQuery({
    codes: TRACKED_METRICS,
    from: fromDate,
    to: toDate,
    granularity: 'daily',
  });

  // Fetch latest records for each tracked metric
  const weightQuery = useGetMetricRecordsQuery({ code: 'weight_kg', page: 1, pageSize: 1 });
  const bodyFatQuery = useGetMetricRecordsQuery({ code: 'body_fat_pct', page: 1, pageSize: 1 });
  const stepsQuery = useGetMetricRecordsQuery({ code: 'steps', page: 1, pageSize: 1 });
  const sleepQuery = useGetMetricRecordsQuery({ code: 'sleep_hours', page: 1, pageSize: 1 });
  const restingHrQuery = useGetMetricRecordsQuery({ code: 'resting_hr', page: 1, pageSize: 1 });

  // Build a map of metric code -> latest record
  const recordQueries = useMemo(() => ({
    weight_kg: weightQuery,
    body_fat_pct: bodyFatQuery,
    steps: stepsQuery,
    sleep_hours: sleepQuery,
    resting_hr: restingHrQuery,
  }), [weightQuery, bodyFatQuery, stepsQuery, sleepQuery, restingHrQuery]);

  // Build definitions lookup map
  const definitionsMap = useMemo((): Record<string, MetricDefinition> => {
    if (!definitions) return {};
    return definitions.reduce<Record<string, MetricDefinition>>((acc, def) => {
      acc[def.code] = def;
      return acc;
    }, {});
  }, [definitions]);

  // Combine definitions with latest records
  const metrics: MetricDisplay[] = useMemo(() => {
    return TRACKED_METRICS.map(code => {
      const query = recordQueries[code as keyof typeof recordQueries];
      const def = definitionsMap[code];
      const latestRecord = query?.data?.data?.[0];
      
      return {
        code,
        value: latestRecord?.value ?? null,
        unit: def?.unit ?? '',
      };
    }).filter(m => m.value !== null); // Only show metrics that have data
  }, [recordQueries, definitionsMap]);

  // Check if any metric queries are still loading
  const metricsLoading = Object.values(recordQueries).some(q => q.isLoading) || definitionsLoading || historyLoading;

  // Fetch longevity data (no RTK Query hook available yet)
  useEffect(() => {
    const fetchLongevity = async () => {
      const token = localStorage.getItem('accessToken');
      const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};

      try {
        const longevityRes = await fetch('/api/longevity', { headers });
        if (longevityRes.ok) {
          const data = await longevityRes.json();
          setLongevity(data.data.attributes);
        }
      } catch (err) {
        console.error('Failed to fetch longevity data:', err);
      } finally {
        setLongevityLoading(false);
      }
    };

    fetchLongevity();
  }, []);

  const loading = longevityLoading || metricsLoading;

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  const metricIcons: Record<string, typeof Scale> = {
    weight_kg: Scale,
    body_fat_pct: Droplet,
    steps: Footprints,
    sleep_hours: Moon,
    resting_hr: Heart,
  };

  const metricNames: Record<string, string> = {
    weight_kg: 'Weight',
    body_fat_pct: 'Body Fat',
    steps: 'Steps',
    sleep_hours: 'Sleep',
    resting_hr: 'Resting HR',
  };

  const totalYears = longevity?.adjustedLifeExpectancy || 80;
  const baseYears = longevity?.baselineLifeExpectancy || 80;

  // Build longevity factors from API breakdown
  const longevityFactors = [
    { key: 'base', name: 'Base', years: baseYears, color: '#6b6b7a' },
    ...(longevity?.breakdown || []).map(b => ({
      key: b.modelCode,
      name: b.modelName.split(' ')[0],
      years: b.yearsAdded,
      color: b.yearsAdded >= 0 ? '#22c55e' : '#ef4444',
    })),
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-text-primary">Health & Longevity</h1>
        <p className="text-text-secondary mt-1">Track your health metrics and life expectancy</p>
      </div>

      {/* Longevity Estimate */}
      <GlassCard variant="elevated" glow="accent" className="p-8">
        <div className="text-center">
          <p className="text-text-secondary mb-2">Estimated Life Expectancy</p>
          <div className="flex items-baseline justify-center gap-2">
            <span className="text-6xl font-bold text-gradient">{totalYears.toFixed(1)}</span>
            <span className="text-2xl text-text-tertiary">years</span>
          </div>
          <p className="text-text-tertiary text-sm mt-2">
            Confidence: {longevity?.confidenceLevel || 'calculating...'}
          </p>
        </div>

        {/* Factors breakdown */}
        <div className="mt-8 flex flex-wrap justify-center gap-6">
          {longevityFactors.map((factor) => (
            <div key={factor.key} className="text-center">
              <p className="text-text-tertiary text-xs mb-1">{factor.name}</p>
              <p className="font-semibold" style={{ color: factor.color }}>
                {factor.years > 0 && factor.name !== 'Base' ? '+' : ''}{factor.years}
              </p>
            </div>
          ))}
        </div>
      </GlassCard>

      {/* Metrics Grid */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold text-text-primary">Health Metrics</h2>
          <TimeScaleSlider value={timeRange.value} onChange={setTimeRange} />
        </div>
        {metrics.length === 0 ? (
          <GlassCard variant="default" className="p-6">
            <p className="text-text-secondary text-center">
              No health metrics recorded yet. Start tracking your metrics to see them here.
            </p>
          </GlassCard>
        ) : (
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {metrics.map((metric) => {
              const Icon = metricIcons[metric.code] || Activity;
              const history = historyData?.data?.[metric.code];
              const displayValue = metric.value !== null
                ? (metric.code === 'steps' && typeof metric.value === 'number' 
                    ? metric.value.toLocaleString() 
                    : metric.value)
                : '—';
              return (
                <GlassCard key={metric.code} variant="default" className="p-6">
                  <div className="flex items-center gap-3 mb-2">
                    <div className="p-2 rounded-lg bg-accent-green/20">
                      <Icon className="w-5 h-5 text-accent-green" />
                    </div>
                    <span className="text-text-secondary">{metricNames[metric.code] || metric.code}</span>
                  </div>
                  <div className="flex items-baseline gap-2 mb-3">
                    <span className="text-3xl font-bold text-text-primary">
                      {displayValue}
                    </span>
                    <span className="text-text-tertiary">{metric.unit}</span>
                  </div>
                  {history?.points && history.points.length > 0 && (
                    <MetricSparkline
                      data={history.points}
                      targetValue={history.targetValue}
                      height={50}
                      color="#22c55e"
                    />
                  )}
                  {(!history?.points || history.points.length === 0) && (
                    <p className="text-sm text-text-tertiary">Latest recorded</p>
                  )}
                </GlassCard>
              );
            })}
          </div>
        )}
      </div>

      {/* Habit Streaks */}
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <Activity className="w-5 h-5 text-accent-purple" />
          <h2 className="text-lg font-semibold text-text-primary">Habit Streaks</h2>
        </div>
        <div className="grid grid-cols-7 gap-1">
          {[85, 100, 45, 70, 20, 90, 60, 100, 30, 75, 15, 80, 55, 95, 
            40, 100, 25, 65, 50, 85, 10, 100, 35, 70, 90, 45, 75, 100,
            20, 55, 80, 60, 95, 40, 100].map((intensity, i) => (
            <div
              key={i}
              className="aspect-square rounded"
              style={{
                backgroundColor: intensity > 70
                  ? '#22c55e'
                  : intensity > 40
                  ? 'rgba(34, 197, 94, 0.5)'
                  : intensity > 20
                  ? 'rgba(34, 197, 94, 0.2)'
                  : 'rgba(255, 255, 255, 0.05)',
              }}
            />
          ))}
        </div>
      </GlassCard>
    </div>
  );
}
