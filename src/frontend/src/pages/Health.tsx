import { useState, useEffect, useMemo } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { Button } from '@components/atoms/Button';
import { MetricSparkline } from '@components/molecules/MetricSparkline';
import { TimeScaleSlider, type TimeRange } from '@components/molecules/TimeScaleSlider';
import { Activity, Moon, Footprints, Scale, Droplet, Heart, TrendingUp, TrendingDown, Info, Lightbulb, HeartPulse, Cigarette, Gauge, Settings, X, Target, Pencil } from 'lucide-react';
import { useGetMetricDefinitionsQuery, useGetMetricHistoryQuery, useGetDimensionsQuery, useUpdateMetricDefinitionMutation, useGetLongevityModelsQuery, useUpdateLongevityModelMutation, type MetricDefinition, type LongevityModel } from '@/services';

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
    notes?: string;
  }>;
  recommendations: Array<{
    area: string;
    suggestion: string;
    potentialGain: number;
  }>;
}

interface LongevityParams {
  metricCode?: string;
  threshold?: number;
  direction?: string;
  optimalMin?: number;
  optimalMax?: number;
  maxYearsAdded?: number;
}

export function Health() {
  const [longevityLoading, setLongevityLoading] = useState(true);
  const [longevity, setLongevity] = useState<LongevityData | null>(null);
  const [timeRange, setTimeRange] = useState<TimeRange>({ label: '30D', value: '30d', days: 30 });
  
  // Modal states
  const [editingMetric, setEditingMetric] = useState<MetricDefinition | null>(null);
  const [editingModel, setEditingModel] = useState<LongevityModel | null>(null);
  const [editTargetValue, setEditTargetValue] = useState<string>('');
  const [editTargetDirection, setEditTargetDirection] = useState<'AtOrAbove' | 'AtOrBelow'>('AtOrAbove');
  const [editModelParams, setEditModelParams] = useState<LongevityParams>({});
  const [showRulesPanel, setShowRulesPanel] = useState(false);

  // Calculate date range for history query
  const toDate = useMemo(() => new Date().toISOString().split('T')[0], []);
  const fromDate = useMemo(
    () => new Date(Date.now() - timeRange.days * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    [timeRange.days]
  );

  // Fetch all metric definitions
  const { data: definitions, isLoading: definitionsLoading, refetch: refetchDefinitions } = useGetMetricDefinitionsQuery();
  
  // Fetch dimensions to find health dimension
  const { data: dimensions } = useGetDimensionsQuery();
  
  // Fetch longevity models
  const { data: longevityModels, refetch: refetchModels } = useGetLongevityModelsQuery();
  
  // Mutations
  const [updateMetric] = useUpdateMetricDefinitionMutation();
  const [updateModel] = useUpdateLongevityModelMutation();
  
  // Find health dimension ID
  const healthDimensionId = useMemo(() => {
    return dimensions?.data?.find((d) => d.attributes.code === 'health')?.id;
  }, [dimensions]);
  
  // Filter to only health metrics (those with health dimension)
  const healthMetrics = useMemo(() => {
    if (!definitions || !healthDimensionId) return [];
    return definitions.filter(d => d.dimensionId === healthDimensionId);
  }, [definitions, healthDimensionId]);
  
  // Get metric codes for history query
  const healthMetricCodes = useMemo(() => healthMetrics.map(m => m.code), [healthMetrics]);

  // Fetch metric history for sparklines
  const { data: historyData, isLoading: historyLoading } = useGetMetricHistoryQuery(
    {
      codes: healthMetricCodes,
      from: fromDate,
      to: toDate,
      granularity: 'daily',
    },
    { skip: healthMetricCodes.length === 0 }
  );

  // Fetch longevity data
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

  const loading = longevityLoading || definitionsLoading || historyLoading;

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  // Icon mapping for metric codes
  const metricIcons: Record<string, typeof Scale> = {
    weight_kg: Scale,
    body_fat_pct: Droplet,
    steps: Footprints,
    sleep_hours: Moon,
    resting_hr: Heart,
    hrv_ms: HeartPulse,
    bp_systolic: Gauge,
    bp_diastolic: Gauge,
    smoke_free_months: Cigarette,
  };

  const totalYears = longevity?.adjustedLifeExpectancy || 80;
  const baseYears = longevity?.baselineLifeExpectancy || 80;
  const yearsAdded = longevity?.estimatedYearsAdded || 0;

  // Metrics with recorded values (have latestValue)
  const metricsWithData = healthMetrics.filter(m => m.latestValue !== undefined && m.latestValue !== null);
  const metricsWithoutData = healthMetrics.filter(m => m.latestValue === undefined || m.latestValue === null);

  // Handle metric target edit
  const handleEditTarget = (metric: MetricDefinition) => {
    setEditingMetric(metric);
    setEditTargetValue(metric.targetValue?.toString() || '');
    setEditTargetDirection(metric.targetDirection || 'AtOrAbove');
  };

  const handleSaveTarget = async () => {
    if (!editingMetric) return;
    try {
      await updateMetric({
        code: editingMetric.code,
        targetValue: editTargetValue ? parseFloat(editTargetValue) : undefined,
        targetDirection: editTargetDirection,
      }).unwrap();
      refetchDefinitions();
      setEditingMetric(null);
    } catch (err) {
      console.error('Failed to update target:', err);
    }
  };

  // Handle longevity model edit
  const handleEditModel = (model: LongevityModel) => {
    setEditingModel(model);
    try {
      const params = JSON.parse(model.parameters) as LongevityParams;
      setEditModelParams(params);
    } catch {
      setEditModelParams({});
    }
  };

  const handleSaveModel = async () => {
    if (!editingModel) return;
    try {
      await updateModel({
        id: editingModel.id,
        parameters: JSON.stringify(editModelParams),
      }).unwrap();
      refetchModels();
      // Refetch longevity data
      const token = localStorage.getItem('accessToken');
      const res = await fetch('/api/longevity', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setLongevity(data.data.attributes);
      }
      setEditingModel(null);
    } catch (err) {
      console.error('Failed to update model:', err);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl md:text-2xl lg:text-3xl font-bold text-text-primary">Health & Longevity</h1>
          <p className="text-text-secondary mt-1 text-sm md:text-base">Track your health metrics and life expectancy</p>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setShowRulesPanel(!showRulesPanel)}
          className="flex items-center gap-2"
        >
          <Settings className="w-4 h-4" />
          <span className="hidden sm:inline">Longevity Rules</span>
        </Button>
      </div>

      {/* Longevity Rules Panel */}
      {showRulesPanel && longevityModels && (
        <GlassCard variant="default" className="p-4 md:p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <Settings className="w-5 h-5 text-accent-purple" />
              <h2 className="text-base md:text-lg font-semibold text-text-primary">Longevity Rules</h2>
            </div>
            <button onClick={() => setShowRulesPanel(false)} className="p-1 hover:bg-bg-tertiary rounded">
              <X className="w-4 h-4 text-text-tertiary" />
            </button>
          </div>
          <div className="space-y-3">
            {longevityModels.map((model) => {
              const params = (() => {
                try { return JSON.parse(model.parameters) as LongevityParams; } 
                catch { return {} as LongevityParams; }
              })();
              return (
                <div key={model.id} className="flex items-center justify-between p-3 bg-bg-tertiary rounded-lg">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className={`w-2 h-2 rounded-full ${model.isActive ? 'bg-semantic-success' : 'bg-text-tertiary'}`} />
                      <span className="font-medium text-sm text-text-primary">{model.name}</span>
                    </div>
                    <p className="text-xs text-text-tertiary mt-1">
                      {model.modelType === 'threshold' && (
                        <>Metric: {params.metricCode} {params.direction} {params.threshold} → +{params.maxYearsAdded} yrs</>
                      )}
                      {model.modelType === 'range' && (
                        <>Metric: {params.metricCode} in range {params.optimalMin}-{params.optimalMax} → +{params.maxYearsAdded} yrs</>
                      )}
                    </p>
                  </div>
                  <button
                    onClick={() => handleEditModel(model)}
                    className="p-1.5 hover:bg-bg-primary rounded transition-colors"
                  >
                    <Pencil className="w-3.5 h-3.5 text-text-tertiary" />
                  </button>
                </div>
              );
            })}
          </div>
        </GlassCard>
      )}

      {/* Longevity Estimate */}
      <GlassCard variant="elevated" glow="accent" className="p-4 md:p-6">
        <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
          {/* Main Estimate */}
          <div className="text-center lg:text-left">
            <div className="flex items-center justify-center lg:justify-start gap-2 mb-2">
              <p className="text-text-secondary text-xs md:text-sm">Estimated Life Expectancy</p>
              <div className="group relative">
                <Info className="w-3.5 h-3.5 text-text-tertiary cursor-help" />
                <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-3 py-2 bg-bg-primary border border-border-primary rounded-lg text-xs text-text-secondary w-64 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-10">
                  Based on your baseline life expectancy ({baseYears} years) adjusted by your health metrics using evidence-based longevity models.
                </div>
              </div>
            </div>
            <div className="flex items-baseline justify-center lg:justify-start gap-2">
              <span className="text-4xl md:text-5xl lg:text-6xl font-bold text-gradient">{totalYears.toFixed(1)}</span>
              <span className="text-lg md:text-xl text-text-tertiary">years</span>
            </div>
            <div className="flex items-center justify-center lg:justify-start gap-2 mt-2">
              {yearsAdded >= 0 ? (
                <TrendingUp className="w-4 h-4 text-semantic-success" />
              ) : (
                <TrendingDown className="w-4 h-4 text-semantic-error" />
              )}
              <span className={`text-sm font-medium ${yearsAdded >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
                {yearsAdded >= 0 ? '+' : ''}{yearsAdded.toFixed(1)} years from baseline
              </span>
            </div>
            <p className="text-text-tertiary text-xs mt-1">
              Confidence: <span className="capitalize">{longevity?.confidenceLevel || 'calculating...'}</span>
            </p>
          </div>

          {/* Breakdown */}
          {longevity?.breakdown && longevity.breakdown.length > 0 && (
            <div className="flex-1 max-w-md">
              <p className="text-text-secondary text-xs mb-3 font-medium">Contributing Factors</p>
              <div className="space-y-2">
                {longevity.breakdown.map((factor) => (
                  <div key={factor.modelCode} className="flex items-center justify-between text-sm">
                    <span className="text-text-secondary">{factor.modelName}</span>
                    <span className={`font-medium ${factor.yearsAdded >= 0 ? 'text-semantic-success' : 'text-semantic-error'}`}>
                      {factor.yearsAdded >= 0 ? '+' : ''}{factor.yearsAdded.toFixed(1)} yrs
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </GlassCard>

      {/* Recommendations */}
      {longevity?.recommendations && longevity.recommendations.length > 0 && (
        <GlassCard variant="default" className="p-4 md:p-6">
          <div className="flex items-center gap-2 mb-4">
            <Lightbulb className="w-5 h-5 text-accent-yellow" />
            <h2 className="text-base md:text-lg font-semibold text-text-primary">Recommendations</h2>
          </div>
          <div className="space-y-3">
            {longevity.recommendations.map((rec, idx) => (
              <div key={idx} className="flex items-start gap-3 p-3 bg-bg-tertiary rounded-lg">
                <div className="p-1.5 rounded-full bg-accent-yellow/20 mt-0.5">
                  <TrendingUp className="w-3 h-3 text-accent-yellow" />
                </div>
                <div className="flex-1">
                  <p className="text-sm text-text-primary">{rec.suggestion}</p>
                  <p className="text-xs text-semantic-success mt-1">
                    Potential gain: +{rec.potentialGain.toFixed(1)} years
                  </p>
                </div>
              </div>
            ))}
          </div>
        </GlassCard>
      )}

      {/* Metrics Grid */}
      <div>
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
          <h2 className="text-base md:text-lg font-semibold text-text-primary">
            Health Metrics
            <span className="text-text-tertiary text-sm font-normal ml-2">
              ({metricsWithData.length} tracked, {metricsWithoutData.length} pending)
            </span>
          </h2>
          <TimeScaleSlider value={timeRange.value} onChange={setTimeRange} />
        </div>
        
        {metricsWithData.length === 0 && metricsWithoutData.length === 0 ? (
          <GlassCard variant="default" className="p-4 md:p-6">
            <p className="text-text-secondary text-center text-sm md:text-base">
              No health metrics defined yet. Go to Metrics page to create health metric definitions.
            </p>
          </GlassCard>
        ) : (
          <>
            {/* Metrics with data */}
            {metricsWithData.length > 0 && (
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4 mb-4">
                {metricsWithData.map((metric) => {
                  const Icon = metricIcons[metric.code] || Activity;
                  const history = historyData?.data?.[metric.code];
                  const displayValue = metric.latestValue !== null && metric.latestValue !== undefined
                    ? (metric.code === 'steps' 
                        ? metric.latestValue.toLocaleString() 
                        : metric.latestValue)
                    : '—';
                  return (
                    <GlassCard key={metric.code} variant="default" className="p-3 md:p-4 group relative">
                      <button
                        onClick={() => handleEditTarget(metric)}
                        className="absolute top-2 right-2 p-1 opacity-0 group-hover:opacity-100 hover:bg-bg-tertiary rounded transition-all"
                        title="Edit target"
                      >
                        <Target className="w-3.5 h-3.5 text-text-tertiary" />
                      </button>
                      <div className="flex items-center gap-2 mb-2">
                        <div className="p-1.5 rounded-lg bg-accent-green/20">
                          <Icon className="w-4 h-4 text-accent-green" />
                        </div>
                        <span className="text-text-secondary text-xs truncate">{metric.name}</span>
                      </div>
                      <div className="flex items-baseline gap-1 mb-1">
                        <span className="text-xl md:text-2xl font-bold text-text-primary">
                          {displayValue}
                        </span>
                        <span className="text-text-tertiary text-xs">{metric.unit}</span>
                      </div>
                      {metric.targetValue && (
                        <p className="text-xs text-accent-purple mb-2">
                          Target: {metric.targetDirection === 'AtOrBelow' ? '≤' : '≥'} {metric.targetValue} {metric.unit}
                        </p>
                      )}
                      {history?.points && history.points.length > 0 ? (
                        <MetricSparkline
                          data={history.points}
                          targetValue={metric.targetValue}
                          targetDirection={metric.targetDirection}
                          currentValue={metric.latestValue}
                          height={50}
                          showLabels={true}
                        />
                      ) : (
                        <p className="text-xs text-text-tertiary">
                          {metric.latestRecordedAt 
                            ? new Date(metric.latestRecordedAt).toLocaleDateString()
                            : 'Latest recorded'}
                        </p>
                      )}
                    </GlassCard>
                  );
                })}
              </div>
            )}

            {/* Metrics without data */}
            {metricsWithoutData.length > 0 && (
              <div>
                <p className="text-text-tertiary text-xs mb-2">Not yet tracked:</p>
                <div className="flex flex-wrap gap-2">
                  {metricsWithoutData.map((metric) => {
                    const Icon = metricIcons[metric.code] || Activity;
                    return (
                      <div 
                        key={metric.code} 
                        className="inline-flex items-center gap-1.5 px-2 py-1 bg-bg-tertiary rounded-lg text-text-tertiary"
                      >
                        <Icon className="w-3 h-3" />
                        <span className="text-xs">{metric.name}</span>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Edit Target Modal */}
      {editingMetric && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <GlassCard variant="elevated" className="w-full max-w-md p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-text-primary">Edit Target: {editingMetric.name}</h3>
              <button onClick={() => setEditingMetric(null)} className="p-1 hover:bg-bg-tertiary rounded">
                <X className="w-5 h-5 text-text-tertiary" />
              </button>
            </div>
            <div className="space-y-4">
              <div>
                <label className="block text-sm text-text-secondary mb-1">Target Value ({editingMetric.unit})</label>
                <input
                  type="number"
                  value={editTargetValue}
                  onChange={(e) => setEditTargetValue(e.target.value)}
                  className="w-full px-3 py-2 bg-bg-tertiary border border-border-primary rounded-lg text-text-primary"
                  placeholder="Enter target value"
                />
              </div>
              <div>
                <label className="block text-sm text-text-secondary mb-1">Target Direction</label>
                <select
                  value={editTargetDirection}
                  onChange={(e) => setEditTargetDirection(e.target.value as 'AtOrAbove' | 'AtOrBelow')}
                  className="w-full px-3 py-2 bg-bg-tertiary border border-border-primary rounded-lg text-text-primary"
                >
                  <option value="AtOrAbove">≥ At or Above (higher is better)</option>
                  <option value="AtOrBelow">≤ At or Below (lower is better)</option>
                </select>
              </div>
              <div className="flex gap-3">
                <Button variant="ghost" onClick={() => setEditingMetric(null)} className="flex-1">
                  Cancel
                </Button>
                <Button variant="primary" onClick={handleSaveTarget} className="flex-1">
                  Save
                </Button>
              </div>
            </div>
          </GlassCard>
        </div>
      )}

      {/* Edit Longevity Model Modal */}
      {editingModel && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <GlassCard variant="elevated" className="w-full max-w-md p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-text-primary">Edit Rule: {editingModel.name}</h3>
              <button onClick={() => setEditingModel(null)} className="p-1 hover:bg-bg-tertiary rounded">
                <X className="w-5 h-5 text-text-tertiary" />
              </button>
            </div>
            <div className="space-y-4">
              {editingModel.modelType === 'threshold' && (
                <>
                  <div>
                    <label className="block text-sm text-text-secondary mb-1">Threshold Value</label>
                    <input
                      type="number"
                      value={editModelParams.threshold || ''}
                      onChange={(e) => setEditModelParams({...editModelParams, threshold: parseFloat(e.target.value) || 0})}
                      className="w-full px-3 py-2 bg-bg-tertiary border border-border-primary rounded-lg text-text-primary"
                    />
                  </div>
                  <div>
                    <label className="block text-sm text-text-secondary mb-1">Direction</label>
                    <select
                      value={editModelParams.direction || 'above'}
                      onChange={(e) => setEditModelParams({...editModelParams, direction: e.target.value})}
                      className="w-full px-3 py-2 bg-bg-tertiary border border-border-primary rounded-lg text-text-primary"
                    >
                      <option value="above">Above threshold</option>
                      <option value="below">Below threshold</option>
                    </select>
                  </div>
                </>
              )}
              {editingModel.modelType === 'range' && (
                <>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-sm text-text-secondary mb-1">Optimal Min</label>
                      <input
                        type="number"
                        value={editModelParams.optimalMin || ''}
                        onChange={(e) => setEditModelParams({...editModelParams, optimalMin: parseFloat(e.target.value) || 0})}
                        className="w-full px-3 py-2 bg-bg-tertiary border border-border-primary rounded-lg text-text-primary"
                      />
                    </div>
                    <div>
                      <label className="block text-sm text-text-secondary mb-1">Optimal Max</label>
                      <input
                        type="number"
                        value={editModelParams.optimalMax || ''}
                        onChange={(e) => setEditModelParams({...editModelParams, optimalMax: parseFloat(e.target.value) || 0})}
                        className="w-full px-3 py-2 bg-bg-tertiary border border-border-primary rounded-lg text-text-primary"
                      />
                    </div>
                  </div>
                </>
              )}
              <div>
                <label className="block text-sm text-text-secondary mb-1">Max Years Added</label>
                <input
                  type="number"
                  step="0.1"
                  value={editModelParams.maxYearsAdded || ''}
                  onChange={(e) => setEditModelParams({...editModelParams, maxYearsAdded: parseFloat(e.target.value) || 0})}
                  className="w-full px-3 py-2 bg-bg-tertiary border border-border-primary rounded-lg text-text-primary"
                />
              </div>
              <div className="flex gap-3">
                <Button variant="ghost" onClick={() => setEditingModel(null)} className="flex-1">
                  Cancel
                </Button>
                <Button variant="primary" onClick={handleSaveModel} className="flex-1">
                  Save
                </Button>
              </div>
            </div>
          </GlassCard>
        </div>
      )}
    </div>
  );
}
