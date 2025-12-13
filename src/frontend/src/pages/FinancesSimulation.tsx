import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Spinner } from '@components/atoms/Spinner';
import { Badge } from '@components/atoms/Badge';
import { Plus, Play, Calculator, TrendingUp, Loader2, AlertCircle, Calendar, Target } from 'lucide-react';

interface Scenario {
  id: string;
  name: string;
  description?: string;
  isBaseline: boolean;
  startDate: string;
  endDate: string;
  createdAt: string;
}

export function FinancesSimulation() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [scenarios, setScenarios] = useState<Scenario[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [runningScenarioId, setRunningScenarioId] = useState<string | null>(null);

  useEffect(() => {
    fetchScenarios();
  }, []);

  const fetchScenarios = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('accessToken');
      const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};

      const response = await fetch('/api/simulations/scenarios', { headers });
      
      if (!response.ok) {
        throw new Error('Failed to fetch scenarios');
      }

      const data = await response.json();
      
      const mappedScenarios: Scenario[] = data.data.map((s: {
        id: string;
        type: string;
        attributes: {
          name: string;
          description?: string;
          isBaseline: boolean;
          startDate: string;
          endDate: string;
          createdAt: string;
        };
      }) => ({
        id: s.id,
        name: s.attributes.name,
        description: s.attributes.description,
        isBaseline: s.attributes.isBaseline,
        startDate: s.attributes.startDate,
        endDate: s.attributes.endDate,
        createdAt: s.attributes.createdAt,
      }));

      setScenarios(mappedScenarios);
    } catch (err) {
      console.error('Failed to fetch scenarios:', err);
      setError(err instanceof Error ? err.message : 'Failed to load scenarios');
    } finally {
      setLoading(false);
    }
  };

  const handleRunSimulation = async (scenarioId: string) => {
    setRunningScenarioId(scenarioId);
    
    try {
      const token = localStorage.getItem('accessToken');
      const headers: HeadersInit = {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      };

      const response = await fetch(`/api/simulations/scenarios/${scenarioId}/run`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ recalculateFromStart: true }),
      });

      if (!response.ok) {
        throw new Error('Failed to run simulation');
      }

      // Navigate to scenario detail page after running
      navigate(`/simulation/${scenarioId}`);
    } catch (err) {
      console.error('Failed to run simulation:', err);
      alert('Failed to run simulation. Please try again.');
    } finally {
      setRunningScenarioId(null);
    }
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-ZA', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const getYearsDuration = (start: string, end: string) => {
    if (!start || !end) return 0;
    const startDate = new Date(start);
    const endDate = new Date(end);
    if (isNaN(startDate.getTime()) || isNaN(endDate.getTime())) return 0;
    const years = (endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24 * 365);
    return Math.round(years);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-64 space-y-4">
        <AlertCircle className="w-12 h-12 text-semantic-error" />
        <p className="text-text-secondary">{error}</p>
        <Button onClick={fetchScenarios} variant="secondary">
          Try Again
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-base md:text-lg font-semibold text-text-primary">Financial Simulations</h2>
          <p className="text-text-secondary text-xs mt-1">Build and run financial scenarios to project your future</p>
        </div>
        <Button 
          onClick={() => navigate('/simulation/new')} 
          icon={<Plus className="w-3 h-3" />}
          className="text-xs px-2 py-1"
        >
          New Scenario
        </Button>
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <GlassCard 
          variant="default" 
          className="p-3 cursor-pointer hover:bg-background-hover transition-colors"
          onClick={() => navigate('/simulation/builder')}
        >
          <div className="flex items-start gap-3">
            <div className="p-2 rounded-lg bg-accent-purple/20">
              <Calculator className="w-4 h-4 text-accent-purple" />
            </div>
            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-semibold text-text-primary">Custom Scenario</h3>
              <p className="text-xs text-text-secondary mt-0.5">Build a detailed simulation from scratch</p>
            </div>
          </div>
        </GlassCard>

        <GlassCard 
          variant="default" 
          className="p-3 cursor-pointer hover:bg-background-hover transition-colors"
          onClick={() => navigate('/simulation/new')}
        >
          <div className="flex items-start gap-3">
            <div className="p-2 rounded-lg bg-accent-cyan/20">
              <TrendingUp className="w-4 h-4 text-accent-cyan" />
            </div>
            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-semibold text-text-primary">Quick Wizard</h3>
              <p className="text-xs text-text-secondary mt-0.5">Answer simple questions for instant projections</p>
            </div>
          </div>
        </GlassCard>

        <GlassCard 
          variant="default" 
          className="p-3 cursor-pointer hover:bg-background-hover transition-colors"
          onClick={() => {
            const baseline = scenarios.find(s => s.isBaseline);
            if (baseline) {
              navigate(`/simulation/${baseline.id}`);
            } else {
              alert('No baseline scenario found. Please create one first.');
            }
          }}
        >
          <div className="flex items-start gap-3">
            <div className="p-2 rounded-lg bg-accent-orange/20">
              <Target className="w-4 h-4 text-accent-orange" />
            </div>
            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-semibold text-text-primary">View Baseline</h3>
              <p className="text-xs text-text-secondary mt-0.5">See your current trajectory</p>
            </div>
          </div>
        </GlassCard>
      </div>

      {/* Scenarios List */}
      <GlassCard variant="default" className="p-4">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-sm md:text-base font-semibold text-text-primary">Your Scenarios</h3>
          <span className="text-text-tertiary text-xs">{scenarios.length} scenarios</span>
        </div>

        {scenarios.length === 0 ? (
          <div className="text-center py-8">
            <Calculator className="w-12 h-12 text-text-tertiary mx-auto mb-3" />
            <p className="text-text-secondary text-sm mb-1">No scenarios yet</p>
            <p className="text-text-tertiary text-xs mb-4">Create your first scenario to start projecting your financial future</p>
            <Button 
              onClick={() => navigate('/simulation/new')} 
              icon={<Plus className="w-3 h-3" />}
              className="text-xs"
            >
              Create First Scenario
            </Button>
          </div>
        ) : (
          <div className="space-y-3">
            {scenarios.map((scenario) => (
              <div
                key={scenario.id}
                className="p-3 rounded-lg bg-background-hover/50 hover:bg-background-hover transition-colors"
              >
                <div className="flex items-start justify-between gap-3">
                  <div 
                    className="flex-1 min-w-0 cursor-pointer"
                    onClick={() => navigate(`/simulation/${scenario.id}`)}
                  >
                    <div className="flex items-center gap-2 mb-1">
                      <h4 className="font-semibold text-text-primary text-sm">{scenario.name}</h4>
                      {scenario.isBaseline && (
                        <Badge variant="success" className="text-[10px] px-1.5 py-0.5">
                          Baseline
                        </Badge>
                      )}
                    </div>
                    {scenario.description && (
                      <p className="text-xs text-text-secondary mb-2">{scenario.description}</p>
                    )}
                    <div className="flex flex-wrap items-center gap-3 text-xs text-text-tertiary">
                      <div className="flex items-center gap-1">
                        <Calendar className="w-3 h-3" />
                        <span>{formatDate(scenario.startDate)} - {formatDate(scenario.endDate)}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <TrendingUp className="w-3 h-3" />
                        <span>{getYearsDuration(scenario.startDate, scenario.endDate)} years</span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      onClick={(e) => {
                        e.stopPropagation();
                        handleRunSimulation(scenario.id);
                      }}
                      variant="secondary"
                      disabled={runningScenarioId === scenario.id}
                      icon={runningScenarioId === scenario.id ? (
                        <Loader2 className="w-3 h-3 animate-spin" />
                      ) : (
                        <Play className="w-3 h-3" />
                      )}
                      className="text-xs px-2 py-1"
                    >
                      {runningScenarioId === scenario.id ? 'Running...' : 'Run'}
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </GlassCard>

      {/* Info Card */}
      <GlassCard variant="default" className="p-4">
        <div className="flex items-start gap-3">
          <div className="p-2 rounded-lg bg-accent-purple/20">
            <AlertCircle className="w-4 h-4 text-accent-purple" />
          </div>
          <div className="flex-1 min-w-0">
            <h3 className="text-sm font-semibold text-text-primary mb-2">About Financial Simulations</h3>
            <div className="space-y-1 text-xs text-text-secondary">
              <p>• <strong>Baseline Scenario:</strong> Your current trajectory based on existing accounts, income, expenses, and investments</p>
              <p>• <strong>Custom Scenarios:</strong> Test "what if" situations like major purchases, career changes, or investment strategies</p>
              <p>• <strong>Projections:</strong> See month-by-month predictions of your net worth, account balances, and milestone achievements</p>
              <p>• <strong>Comparisons:</strong> Compare different scenarios side-by-side to make informed decisions</p>
            </div>
          </div>
        </div>
      </GlassCard>
    </div>
  );
}
