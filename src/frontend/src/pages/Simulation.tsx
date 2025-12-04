import { useNavigate } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Badge } from '@components/atoms/Badge';
import { Plus, Play, Calendar, TrendingUp, Loader2, AlertCircle } from 'lucide-react';
import { useGetScenariosQuery } from '@/services';

export function Simulation() {
  const navigate = useNavigate();
  const { data: scenarios = [], isLoading, error } = useGetScenariosQuery();

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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-text-primary">Simulation</h1>
          <p className="text-text-secondary mt-1">Build financial scenarios and projections</p>
        </div>
        <Button onClick={() => navigate('/simulation/new')} icon={<Plus className="w-4 h-4" />}>
          New Scenario
        </Button>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-8 h-8 text-accent-purple animate-spin" />
        </div>
      )}

      {/* Error State */}
      {error && (
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center gap-3 text-semantic-error">
            <AlertCircle className="w-5 h-5" />
            <span>Failed to load scenarios. Please try again.</span>
          </div>
        </GlassCard>
      )}

      {/* Active Scenario Highlight */}
      {!isLoading && scenarios.find(s => s.isActive) && (
        <GlassCard variant="elevated" glow="accent" className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center gap-2 mb-2">
                <Badge variant="success">Active</Badge>
                <span className="text-lg font-semibold text-text-primary">
                  {scenarios.find(s => s.isActive)?.name}
                </span>
              </div>
              <p className="text-text-secondary">
                {scenarios.find(s => s.isActive)?.description}
              </p>
            </div>
            <Button 
              variant="secondary" 
              icon={<Play className="w-4 h-4" />}
              onClick={() => navigate(`/simulation/${scenarios.find(s => s.isActive)?.id}`)}
            >
              View Results
            </Button>
          </div>
        </GlassCard>
      )}

      {/* Scenarios List */}
      {!isLoading && !error && (
        <div>
        <h2 className="text-xl font-semibold text-text-primary mb-4">All Scenarios</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {scenarios.map((scenario) => (
            <GlassCard
              key={scenario.id}
              variant="default"
              className="p-5 hover:shadow-glow-sm transition-all cursor-pointer"
              onClick={() => navigate(`/simulation/${scenario.id}`)}
            >
              <div className="flex items-start justify-between mb-3">
                <div className="flex items-center gap-2">
                  <TrendingUp className="w-5 h-5 text-accent-purple" />
                  <h3 className="font-semibold text-text-primary">{scenario.name}</h3>
                </div>
                {scenario.isActive && <Badge variant="success">Active</Badge>}
              </div>
              
              <p className="text-text-secondary text-sm mb-4 line-clamp-2">
                {scenario.description}
              </p>

              <div className="flex items-center gap-4 text-xs text-text-tertiary">
                <div className="flex items-center gap-1">
                  <Calendar className="w-3 h-3" />
                  <span>{getYearsDuration(scenario.startDate, scenario.endDate)} years</span>
                </div>
                <span>•</span>
                <span>{scenario.events.length} events</span>
              </div>

              <div className="mt-4 pt-4 border-t border-glass-border flex items-center justify-between">
                <span className="text-xs text-text-tertiary">
                  Created {formatDate(scenario.createdAt)}
                </span>
                <button 
                  className="text-accent-purple hover:text-accent-purple/80 text-sm font-medium transition-colors"
                  onClick={(e) => {
                    e.stopPropagation();
                    navigate(`/simulation/${scenario.id}`);
                  }}
                >
                  Run →
                </button>
              </div>
            </GlassCard>
          ))}
        </div>
      </div>
      )}

      {/* Empty State */}
      {!isLoading && !error && scenarios.length === 0 && (
        <div className="text-center py-12">
          <TrendingUp className="w-12 h-12 text-text-tertiary mx-auto mb-4" />
          <h3 className="text-lg font-medium text-text-primary mb-2">No scenarios yet</h3>
          <p className="text-text-secondary mb-6">
            Create your first financial scenario to see projections
          </p>
          <Button onClick={() => navigate('/simulation/new')} icon={<Plus className="w-4 h-4" />}>
            Create Scenario
          </Button>
        </div>
      )}
    </div>
  );
}
