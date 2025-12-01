import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Badge } from '@components/atoms/Badge';
import { ProjectionChart } from '@components/organisms/ProjectionChart';
import { ArrowLeft, Play, Settings, Plus, Trash2, Calendar, DollarSign, Loader2, AlertCircle } from 'lucide-react';
import { cn } from '@utils/cn';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import type { Scenario, FutureEvent } from '@/types';
import { ScenarioEditor } from './placeholders/ScenarioEditor';
import { FutureEventsEditor } from './placeholders/FutureEventsEditor';
import {
  useGetScenarioQuery,
  useGetScenarioEventsQuery,
  useGetScenarioProjectionsQuery,
  useRunSimulationMutation,
  useUpdateScenarioMutation,
  useAddEventMutation,
  useDeleteEventMutation,
} from '@/services';

export function SimulationDetail() {
  const { scenarioId } = useParams<{ scenarioId: string }>();
  const navigate = useNavigate();
  const [isEditing, setIsEditing] = useState(false);
  const [showEventEditor, setShowEventEditor] = useState(false);

  // API queries
  const { 
    data: scenario, 
    isLoading: isLoadingScenario, 
    error: scenarioError 
  } = useGetScenarioQuery(scenarioId!, { skip: !scenarioId });
  
  const { 
    data: events = [], 
    isLoading: isLoadingEvents 
  } = useGetScenarioEventsQuery(scenarioId!, { skip: !scenarioId });
  
  const { 
    data: projectionsData,
    isLoading: isLoadingProjections,
  } = useGetScenarioProjectionsQuery(
    { scenarioId: scenarioId! }, 
    { skip: !scenarioId }
  );

  // API mutations
  const [runSimulation, { isLoading: isRunning }] = useRunSimulationMutation();
  const [updateScenario] = useUpdateScenarioMutation();
  const [addEvent] = useAddEventMutation();
  const [deleteEvent] = useDeleteEventMutation();

  const handleRunSimulation = async () => {
    if (!scenarioId) return;
    try {
      await runSimulation(scenarioId).unwrap();
    } catch (error) {
      console.error('Failed to run simulation:', error);
    }
  };

  const handleUpdateScenario = async (data: Partial<Scenario>) => {
    if (!scenarioId) return;
    try {
      await updateScenario({ id: scenarioId, ...data }).unwrap();
      setIsEditing(false);
    } catch (error) {
      console.error('Failed to update scenario:', error);
    }
  };

  const handleAddEvent = async (event: Omit<FutureEvent, 'id' | 'scenarioId'>) => {
    if (!scenarioId) return;
    try {
      await addEvent({ scenarioId, event }).unwrap();
      setShowEventEditor(false);
    } catch (error) {
      console.error('Failed to add event:', error);
    }
  };

  const handleRemoveEvent = async (eventId: string) => {
    if (!scenarioId) return;
    try {
      await deleteEvent({ scenarioId, eventId }).unwrap();
    } catch (error) {
      console.error('Failed to delete event:', error);
    }
  };

  // Combine scenario with events
  const scenarioWithEvents: Scenario | undefined = scenario 
    ? { ...scenario, events } 
    : undefined;

  // Transform projections data for display
  const projections = projectionsData?.projections ?? [];
  const milestones = projectionsData?.milestones ?? [];
  const summary = projectionsData?.summary ?? {
    startNetWorth: 0,
    endNetWorth: 0,
    totalGrowth: 0,
    annualizedReturn: 0,
    totalMonths: 0,
  };

  // Calculate summary values for display
  const displaySummary = {
    finalNetWorth: summary.endNetWorth,
    totalIncome: projections.reduce((sum, p) => sum + p.income, 0),
    totalExpenses: projections.reduce((sum, p) => sum + p.expenses, 0),
    avgMonthlyGrowth: summary.totalMonths > 0 
      ? (summary.annualizedReturn * 100 / 12).toFixed(1) 
      : '0',
  };

  // Loading state
  if (isLoadingScenario || isLoadingEvents) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="w-8 h-8 text-accent-purple animate-spin" />
      </div>
    );
  }

  // Error state
  if (scenarioError || !scenarioWithEvents) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/simulation')}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <ArrowLeft className="w-5 h-5 text-text-secondary" />
          </button>
          <h1 className="text-2xl font-bold text-text-primary">Scenario Not Found</h1>
        </div>
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center gap-3 text-semantic-error">
            <AlertCircle className="w-5 h-5" />
            <span>Failed to load scenario. It may have been deleted.</span>
          </div>
        </GlassCard>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/simulation')}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <ArrowLeft className="w-5 h-5 text-text-secondary" />
          </button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold text-text-primary">{scenarioWithEvents.name}</h1>
              {scenarioWithEvents.isActive && <Badge variant="success">Active</Badge>}
            </div>
            <p className="text-text-secondary">{scenarioWithEvents.description}</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <Button variant="ghost" icon={<Settings className="w-4 h-4" />} onClick={() => setIsEditing(true)}>
            Edit
          </Button>
          <Button 
            icon={isRunning ? <Loader2 className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4" />} 
            onClick={handleRunSimulation}
            disabled={isRunning}
          >
            {isRunning ? 'Running...' : 'Run Simulation'}
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      {isLoadingProjections ? (
        <div className="flex items-center justify-center py-8">
          <Loader2 className="w-6 h-6 text-accent-purple animate-spin" />
        </div>
      ) : (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <GlassCard variant="default" className="p-4">
            <p className="text-text-tertiary text-sm mb-1">Final Net Worth</p>
            <p className="text-2xl font-bold text-text-primary">
              {formatCurrency(displaySummary.finalNetWorth, 'ZAR')}
            </p>
          </GlassCard>
          <GlassCard variant="default" className="p-4">
            <p className="text-text-tertiary text-sm mb-1">Total Income</p>
            <p className="text-2xl font-bold text-semantic-success">
              {formatCurrency(displaySummary.totalIncome, 'ZAR')}
            </p>
          </GlassCard>
          <GlassCard variant="default" className="p-4">
            <p className="text-text-tertiary text-sm mb-1">Total Expenses</p>
            <p className="text-2xl font-bold text-semantic-error">
              {formatCurrency(displaySummary.totalExpenses, 'ZAR')}
            </p>
          </GlassCard>
          <GlassCard variant="default" className="p-4">
            <p className="text-text-tertiary text-sm mb-1">Avg Monthly Growth</p>
            <p className="text-2xl font-bold text-accent-purple">
              {displaySummary.avgMonthlyGrowth}%
            </p>
          </GlassCard>
        </div>
      )}

      {/* Projection Chart */}
      {projections.length > 0 && (
        <ProjectionChart
          data={projections}
          milestones={milestones}
          showCard
          height={400}
        />
      )}

      {/* No Projections Message */}
      {!isLoadingProjections && projections.length === 0 && (
        <GlassCard variant="default" className="p-8 text-center">
          <Play className="w-12 h-12 text-text-tertiary mx-auto mb-4" />
          <h3 className="text-lg font-medium text-text-primary mb-2">No projection data</h3>
          <p className="text-text-secondary mb-4">
            Run the simulation to see financial projections
          </p>
          <Button 
            icon={isRunning ? <Loader2 className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4" />} 
            onClick={handleRunSimulation}
            disabled={isRunning}
          >
            {isRunning ? 'Running...' : 'Run Simulation'}
          </Button>
        </GlassCard>
      )}

      {/* Milestones */}
      {milestones.length > 0 && (
        <GlassCard variant="default" className="p-6">
          <h2 className="text-lg font-semibold text-text-primary mb-4">Projected Milestones</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {milestones.map((milestone, i) => (
              <div
                key={i}
                className="p-4 rounded-lg bg-background-hover/50 border border-glass-border"
              >
                <div className="flex items-center gap-2 mb-2">
                  <DollarSign className="w-4 h-4 text-semantic-warning" />
                  <span className="font-semibold text-text-primary">{milestone.label}</span>
                </div>
                <p className="text-text-secondary text-sm">
                  {milestone.achievedDate
                    ? `Projected: ${new Date(milestone.achievedDate).toLocaleDateString('en-ZA', { year: 'numeric', month: 'long' })}`
                    : 'Not achieved in projection'}
                </p>
                <div className="mt-2 flex items-center gap-2">
                  <div className="flex-1 h-2 bg-glass-light rounded-full overflow-hidden">
                    <div
                      className="h-full bg-semantic-warning rounded-full"
                      style={{ width: `${milestone.probability * 100}%` }}
                    />
                  </div>
                  <span className="text-xs text-text-tertiary">
                    {Math.round(milestone.probability * 100)}%
                  </span>
                </div>
              </div>
            ))}
          </div>
        </GlassCard>
      )}

      {/* Future Events */}
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-text-primary">Future Events</h2>
          <Button
            variant="secondary"
            size="sm"
            icon={<Plus className="w-4 h-4" />}
            onClick={() => setShowEventEditor(true)}
          >
            Add Event
          </Button>
        </div>
        
        {scenarioWithEvents.events.length === 0 ? (
          <p className="text-text-tertiary text-center py-8">No events added yet</p>
        ) : (
          <div className="space-y-3">
            {scenarioWithEvents.events.map((event) => (
              <div
                key={event.id}
                className="flex items-center justify-between p-4 rounded-lg bg-background-hover/50"
              >
                <div className="flex items-center gap-4">
                  <div className="p-2 rounded-lg bg-glass-medium">
                    <Calendar className="w-4 h-4 text-accent-cyan" />
                  </div>
                  <div>
                    <p className="font-medium text-text-primary">{event.name}</p>
                    <p className="text-sm text-text-tertiary">
                      {new Date(event.date).toLocaleDateString('en-ZA', { year: 'numeric', month: 'short' })}
                      {event.isRecurring && ` • ${event.recurringFrequency}`}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <span className={cn(
                    'font-semibold',
                    event.type.includes('income') ? 'text-semantic-success' : 'text-semantic-error'
                  )}>
                    {event.type.includes('income') ? '+' : '-'}
                    {formatCurrency(event.amount, event.currency)}
                  </span>
                  <button
                    onClick={() => handleRemoveEvent(event.id)}
                    className="p-2 rounded-lg hover:bg-background-hover transition-colors text-text-tertiary hover:text-semantic-error"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </GlassCard>

      {/* Modals */}
      {isEditing && (
        <ScenarioEditor
          scenario={scenarioWithEvents}
          onSave={handleUpdateScenario}
          onClose={() => setIsEditing(false)}
        />
      )}
      {showEventEditor && (
        <FutureEventsEditor
          onAdd={handleAddEvent}
          onClose={() => setShowEventEditor(false)}
        />
      )}
    </div>
  );
}
