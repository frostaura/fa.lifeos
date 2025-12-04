import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { 
  ArrowLeft, 
  Plus, 
  Trash2, 
  Play, 
  Calendar,
  DollarSign,
  TrendingUp,
  TrendingDown,
  Loader2,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react';
import { cn } from '@utils/cn';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import {
  useCreateScenarioMutation,
  useAddEventMutation,
  useRunSimulationMutation,
} from '@/services';
import type { FutureEvent } from '@/types';

type EventType = 'one_time_income' | 'one_time_expense' | 'recurring_income' | 'recurring_expense' | 'asset_purchase' | 'asset_sale';

interface DraftEvent {
  id: string;
  name: string;
  type: EventType;
  date: string;
  amount: string;
  isRecurring: boolean;
  recurringFrequency?: 'monthly' | 'quarterly' | 'yearly';
  endDate?: string;
}

const eventTypeOptions: { value: EventType; label: string; icon: React.ComponentType<{ className?: string }>; color: string }[] = [
  { value: 'one_time_income', label: 'One-time Income', icon: TrendingUp, color: 'text-semantic-success' },
  { value: 'one_time_expense', label: 'One-time Expense', icon: TrendingDown, color: 'text-semantic-error' },
  { value: 'recurring_income', label: 'Recurring Income', icon: TrendingUp, color: 'text-accent-cyan' },
  { value: 'recurring_expense', label: 'Recurring Expense', icon: TrendingDown, color: 'text-accent-orange' },
  { value: 'asset_purchase', label: 'Asset Purchase', icon: Plus, color: 'text-accent-purple' },
  { value: 'asset_sale', label: 'Asset Sale', icon: DollarSign, color: 'text-semantic-warning' },
];

export function CustomScenarioBuilder() {
  const navigate = useNavigate();
  
  // Form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0]);
  const [endDate, setEndDate] = useState(
    new Date(Date.now() + 10 * 365 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
  );
  const [events, setEvents] = useState<DraftEvent[]>([]);
  const [isBaseline, setIsBaseline] = useState(false);
  
  // New event form state
  const [showEventForm, setShowEventForm] = useState(false);
  const [newEvent, setNewEvent] = useState<DraftEvent>({
    id: '',
    name: '',
    type: 'one_time_expense',
    date: startDate,
    amount: '',
    isRecurring: false,
  });
  
  // API state
  const [createScenario, { isLoading: isCreating }] = useCreateScenarioMutation();
  const [addEvent, { isLoading: isAddingEvent }] = useAddEventMutation();
  const [runSimulation, { isLoading: isRunning }] = useRunSimulationMutation();
  
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  
  const handleAddEvent = () => {
    if (!newEvent.name || !newEvent.amount || !newEvent.date) {
      setError('Please fill in all event fields');
      return;
    }
    
    const event: DraftEvent = {
      ...newEvent,
      id: `temp-${Date.now()}`,
      isRecurring: newEvent.type.includes('recurring'),
    };
    
    setEvents([...events, event]);
    setNewEvent({
      id: '',
      name: '',
      type: 'one_time_expense',
      date: startDate,
      amount: '',
      isRecurring: false,
    });
    setShowEventForm(false);
    setError(null);
  };
  
  const handleRemoveEvent = (id: string) => {
    setEvents(events.filter(e => e.id !== id));
  };
  
  const handleSubmit = async () => {
    if (!name) {
      setError('Please provide a scenario name');
      return;
    }
    
    if (!startDate || !endDate) {
      setError('Please provide start and end dates');
      return;
    }
    
    try {
      setError(null);
      
      // Create the scenario
      const scenario = await createScenario({
        name,
        description,
        startDate,
        endDate,
        isActive: isBaseline,
      }).unwrap();
      
      // Add all events
      for (const event of events) {
        await addEvent({
          scenarioId: scenario.id,
          event: {
            name: event.name,
            type: event.type as FutureEvent['type'],
            date: event.date,
            amount: parseFloat(event.amount),
            currency: 'ZAR',
            isRecurring: event.isRecurring,
            recurringFrequency: event.recurringFrequency,
          },
        }).unwrap();
      }
      
      // Run the simulation
      await runSimulation(scenario.id).unwrap();
      
      setSuccess(true);
      
      // Navigate to the scenario detail page
      setTimeout(() => {
        navigate(`/simulation/${scenario.id}`);
      }, 1500);
    } catch (err) {
      console.error('Failed to create scenario:', err);
      setError('Failed to create scenario. Please try again.');
    }
  };
  
  const getEventTypeInfo = (type: EventType) => {
    return eventTypeOptions.find(o => o.value === type) || eventTypeOptions[0];
  };
  
  const calculateYears = () => {
    if (!startDate || !endDate) return 0;
    const start = new Date(startDate);
    const end = new Date(endDate);
    return Math.round((end.getTime() - start.getTime()) / (365.25 * 24 * 60 * 60 * 1000));
  };
  
  if (success) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <GlassCard variant="elevated" className="p-8 text-center max-w-md">
          <CheckCircle2 className="w-16 h-16 text-semantic-success mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-text-primary mb-2">Scenario Created!</h2>
          <p className="text-text-secondary">
            Your custom scenario has been created and simulated. Redirecting...
          </p>
        </GlassCard>
      </div>
    );
  }
  
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/simulation/new')}
          className="p-2 rounded-lg hover:bg-background-hover transition-colors"
        >
          <ArrowLeft className="w-5 h-5 text-text-secondary" />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-text-primary">Custom Scenario Builder</h1>
          <p className="text-text-secondary mt-1">Build a detailed financial scenario with custom events</p>
        </div>
      </div>
      
      {/* Error Message */}
      {error && (
        <GlassCard variant="default" className="p-4 border-l-4 border-semantic-error">
          <div className="flex items-center gap-3 text-semantic-error">
            <AlertCircle className="w-5 h-5 flex-shrink-0" />
            <span>{error}</span>
          </div>
        </GlassCard>
      )}
      
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Scenario Details */}
        <GlassCard variant="default" className="p-6 lg:col-span-2">
          <h2 className="text-lg font-semibold text-text-primary mb-4">Scenario Details</h2>
          
          <div className="space-y-4">
            <Input
              label="Scenario Name"
              placeholder="e.g., Retirement Planning"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
            
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1.5">
                Description (optional)
              </label>
              <textarea
                className="w-full px-4 py-3 bg-glass-light border border-glass-border rounded-lg text-text-primary placeholder-text-tertiary focus:outline-none focus:ring-2 focus:ring-accent-purple/50 resize-none"
                rows={3}
                placeholder="Describe what this scenario models..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>
            
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Start Date"
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
              />
              <Input
                label="End Date"
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
              />
            </div>
            
            <div className="flex items-center gap-3 p-3 bg-glass-light rounded-lg">
              <Calendar className="w-5 h-5 text-accent-purple" />
              <span className="text-text-secondary">
                Simulation period: <span className="font-medium text-text-primary">{calculateYears()} years</span>
              </span>
            </div>
            
            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={isBaseline}
                onChange={(e) => setIsBaseline(e.target.checked)}
                className="w-4 h-4 rounded border-glass-border bg-glass-light text-accent-purple focus:ring-accent-purple/50"
              />
              <span className="text-text-secondary">Set as baseline scenario (affects dashboard projections)</span>
            </label>
          </div>
        </GlassCard>
        
        {/* Summary */}
        <GlassCard variant="default" className="p-6">
          <h2 className="text-lg font-semibold text-text-primary mb-4">Summary</h2>
          
          <div className="space-y-4">
            <div className="p-3 bg-glass-light rounded-lg">
              <div className="text-sm text-text-tertiary mb-1">Events</div>
              <div className="text-2xl font-bold text-text-primary">{events.length}</div>
            </div>
            
            <div className="p-3 bg-glass-light rounded-lg">
              <div className="text-sm text-text-tertiary mb-1">Total Income Events</div>
              <div className="text-lg font-semibold text-semantic-success">
                {formatCurrency(
                  events
                    .filter(e => e.type.includes('income') || e.type === 'asset_sale')
                    .reduce((sum, e) => sum + parseFloat(e.amount || '0'), 0),
                  'ZAR'
                )}
              </div>
            </div>
            
            <div className="p-3 bg-glass-light rounded-lg">
              <div className="text-sm text-text-tertiary mb-1">Total Expense Events</div>
              <div className="text-lg font-semibold text-semantic-error">
                {formatCurrency(
                  events
                    .filter(e => e.type.includes('expense') || e.type === 'asset_purchase')
                    .reduce((sum, e) => sum + parseFloat(e.amount || '0'), 0),
                  'ZAR'
                )}
              </div>
            </div>
            
            <Button
              className="w-full"
              onClick={handleSubmit}
              disabled={isCreating || isAddingEvent || isRunning || !name}
              icon={isCreating || isAddingEvent || isRunning ? <Loader2 className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4" />}
            >
              {isCreating ? 'Creating...' : isAddingEvent ? 'Adding Events...' : isRunning ? 'Running...' : 'Create & Run Scenario'}
            </Button>
          </div>
        </GlassCard>
      </div>
      
      {/* Events Section */}
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-text-primary">Future Events</h2>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => setShowEventForm(true)}
            icon={<Plus className="w-4 h-4" />}
          >
            Add Event
          </Button>
        </div>
        
        {/* Event Form */}
        {showEventForm && (
          <GlassCard variant="elevated" className="p-4 mb-4">
            <h3 className="font-medium text-text-primary mb-4">New Event</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <Input
                label="Event Name"
                placeholder="e.g., Bonus Payment"
                value={newEvent.name}
                onChange={(e) => setNewEvent({ ...newEvent, name: e.target.value })}
              />
              
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-1.5">
                  Event Type
                </label>
                <select
                  className="w-full px-4 py-3 bg-glass-light border border-glass-border rounded-lg text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple/50"
                  value={newEvent.type}
                  onChange={(e) => setNewEvent({ 
                    ...newEvent, 
                    type: e.target.value as EventType,
                    isRecurring: e.target.value.includes('recurring'),
                  })}
                >
                  {eventTypeOptions.map(option => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
              
              <Input
                label="Amount (ZAR)"
                type="number"
                placeholder="e.g., 50000"
                value={newEvent.amount}
                onChange={(e) => setNewEvent({ ...newEvent, amount: e.target.value })}
              />
              
              <Input
                label="Date"
                type="date"
                value={newEvent.date}
                onChange={(e) => setNewEvent({ ...newEvent, date: e.target.value })}
              />
              
              {newEvent.type.includes('recurring') && (
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1.5">
                    Frequency
                  </label>
                  <select
                    className="w-full px-4 py-3 bg-glass-light border border-glass-border rounded-lg text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple/50"
                    value={newEvent.recurringFrequency || 'monthly'}
                    onChange={(e) => setNewEvent({ 
                      ...newEvent, 
                      recurringFrequency: e.target.value as 'monthly' | 'quarterly' | 'yearly',
                    })}
                  >
                    <option value="monthly">Monthly</option>
                    <option value="quarterly">Quarterly</option>
                    <option value="yearly">Yearly</option>
                  </select>
                </div>
              )}
            </div>
            
            <div className="flex gap-3 mt-4">
              <Button onClick={handleAddEvent} size="sm">
                Add Event
              </Button>
              <Button 
                variant="ghost" 
                size="sm"
                onClick={() => {
                  setShowEventForm(false);
                  setError(null);
                }}
              >
                Cancel
              </Button>
            </div>
          </GlassCard>
        )}
        
        {/* Events List */}
        {events.length === 0 ? (
          <div className="text-center py-8 text-text-tertiary">
            <Calendar className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>No events added yet. Add events to model future income, expenses, or asset changes.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {events.map((event) => {
              const typeInfo = getEventTypeInfo(event.type);
              const Icon = typeInfo.icon;
              
              return (
                <div
                  key={event.id}
                  className="flex items-center justify-between p-4 bg-glass-light rounded-lg"
                >
                  <div className="flex items-center gap-4">
                    <div className={cn('p-2 rounded-lg bg-background-tertiary', typeInfo.color)}>
                      <Icon className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="font-medium text-text-primary">{event.name}</div>
                      <div className="text-sm text-text-tertiary">
                        {typeInfo.label} • {new Date(event.date).toLocaleDateString()}
                        {event.isRecurring && ` • ${event.recurringFrequency}`}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className={cn(
                      'font-semibold',
                      event.type.includes('income') || event.type === 'asset_sale' 
                        ? 'text-semantic-success' 
                        : 'text-semantic-error'
                    )}>
                      {event.type.includes('income') || event.type === 'asset_sale' ? '+' : '-'}
                      {formatCurrency(parseFloat(event.amount || '0'), 'ZAR')}
                    </div>
                    <button
                      onClick={() => handleRemoveEvent(event.id)}
                      className="p-2 rounded-lg hover:bg-semantic-error/10 text-text-tertiary hover:text-semantic-error transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </GlassCard>
    </div>
  );
}
