import { useState } from 'react';
import { LifeOSScoreRings } from '@components/organisms/LifeOSScoreRings';
import { IdentityRadar } from '@components/organisms/IdentityRadar';
import { NetWorthChart } from '@components/organisms/NetWorthChart';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { Button } from '@components/atoms/Button';
import { Badge } from '@components/atoms/Badge';
import { cn } from '@utils/cn';
import { useGetDashboardSnapshotQuery, useGetNetWorthHistoryQuery, useCompleteTaskMutation } from '@/services/endpoints/dashboard';
import { Activity, TrendingUp, Target, Calendar } from 'lucide-react';

export function Dashboard() {
  const { data: snapshot, isLoading, error, refetch } = useGetDashboardSnapshotQuery();
  const { data: netWorthHistory, isLoading: isLoadingHistory } = useGetNetWorthHistoryQuery({ 
    months: 12, 
    currency: 'ZAR' 
  });
  const [completeTask] = useCompleteTaskMutation();
  const [completingTasks, setCompletingTasks] = useState<Set<string>>(new Set());

  const handleCompleteTask = async (taskId: string) => {
    setCompletingTasks(prev => new Set(prev).add(taskId));
    try {
      await completeTask({
        taskId,
        completed: true,
      }).unwrap();
      await refetch();
    } catch (err) {
      console.error('Failed to complete task:', err);
    } finally {
      setCompletingTasks(prev => {
        const next = new Set(prev);
        next.delete(taskId);
        return next;
      });
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !snapshot) {
    return (
      <div className="text-center text-red-400 p-8">
        <p>Error loading dashboard</p>
        <Button onClick={() => refetch()} className="mt-4">
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-7xl mx-auto px-4 md:px-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl md:text-3xl lg:text-4xl font-bold text-text-primary">
            Dashboard
          </h1>
          <p className="text-text-secondary mt-1">
            Welcome to LifeOS v3.0
          </p>
        </div>
      </div>

      {/* Hero Row - LifeOS Score Rings + Identity Radar */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* LifeOS Score Rings - Takes 2 columns on desktop */}
        <GlassCard variant="default" className="lg:col-span-2 p-6 md:p-8" data-testid="lifeos-score">
          <LifeOSScoreRings
            lifeScore={snapshot.lifeScore}
            healthIndex={snapshot.healthIndex}
            adherenceIndex={snapshot.adherenceIndex}
            wealthHealthScore={snapshot.wealthHealthScore}
            size="large"
          />
          
          {/* Longevity Badge */}
          <div className="mt-6 flex justify-center">
            <Badge variant="success" className="text-base px-4 py-2">
              <Activity className="w-4 h-4 mr-2" />
              +{(snapshot.longevityYearsAdded ?? 0).toFixed(1)} years added
            </Badge>
          </div>
        </GlassCard>

        {/* Identity Stats + Priority Actions - Combined */}
        <GlassCard variant="default" className="p-6">
          <h3 className="text-lg font-semibold text-text-primary mb-4">Identity Stats</h3>
          <IdentityRadar
            currentStats={snapshot.primaryStats || {}}
            targetStats={snapshot.primaryStats || {}}
            size="sm"
          />
          
          {/* Priority Actions moved under Identity Stats */}
          <div className="mt-6 pt-6 border-t border-glass-border">
            <div className="flex items-center justify-between mb-4">
              <h4 className="text-base font-semibold text-text-primary flex items-center gap-2">
                <Target className="w-4 h-4 text-blue-400" />
                Today's Priority Actions
              </h4>
              <Badge variant="info" className="text-xs">
                {(snapshot.todayTasks || []).filter(t => t.isCompleted).length}/{(snapshot.todayTasks || []).length}
              </Badge>
            </div>
            
            <div className="space-y-2 max-h-48 overflow-y-auto">
              {(!snapshot.todayTasks || snapshot.todayTasks.length === 0) ? (
                <p className="text-text-secondary text-xs text-center py-4">
                  No tasks for today. Great job!
                </p>
              ) : (
                snapshot.todayTasks.map((task) => (
                  <div
                    key={task.taskId}
                    className={cn(
                      'flex items-center gap-2 p-2 rounded-lg transition-all border',
                      task.isCompleted 
                        ? 'bg-green-500/10 border-green-500/30'
                        : 'bg-background-secondary border-glass-border hover:border-accent-purple/50'
                    )}
                  >
                    <button
                      onClick={() => !task.isCompleted && handleCompleteTask(task.taskId)}
                      disabled={task.isCompleted || completingTasks.has(task.taskId)}
                      className={cn(
                        'w-4 h-4 rounded border-2 flex items-center justify-center transition-colors flex-shrink-0',
                        task.isCompleted
                          ? 'bg-green-500 border-green-500'
                          : 'border-text-muted hover:border-accent-purple'
                      )}
                    >
                      {task.isCompleted && (
                        <svg className="w-2.5 h-2.5 text-text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                        </svg>
                      )}
                    </button>
                    
                    <div className="flex-1 min-w-0">
                      <p className={cn(
                        'text-xs font-medium truncate',
                        task.isCompleted ? 'text-text-secondary line-through' : 'text-text-primary'
                      )}>
                        {task.title}
                      </p>
                      <p className="text-xs text-text-muted capitalize">
                        {task.dimensionCode.replace('_', ' ')}
                      </p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </GlassCard>
      </div>

      {/* Second Row - Health Snapshot & Wealth Health (2 columns instead of 3) */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Health Snapshot */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-text-primary flex items-center gap-2">
              <Activity className="w-5 h-5 text-green-400" />
              Health Snapshot
            </h3>
          </div>
          
          <div className="space-y-4">
            <div className="flex justify-between items-center">
              <span className="text-text-secondary text-sm">Health Index</span>
              <span className="text-2xl font-bold text-text-primary">
                {Math.round(snapshot.healthIndex)}
              </span>
            </div>
            
            <div className="h-2 bg-gray-800 rounded-full overflow-hidden">
              <div 
                className="h-full bg-gradient-to-r from-green-400 to-emerald-600 transition-all duration-500"
                style={{ width: `${snapshot.healthIndex}%` }}
              />
            </div>
            
            {/* Dimension scores for health */}
            {(snapshot.dimensions || [])
              .filter(d => d.code === 'health_recovery')
              .map(dim => (
                <div key={dim.code} className="pt-4 border-t border-gray-700/50">
                  <div className="flex justify-between items-center text-sm">
                    <span className="text-text-secondary">Dimension Score</span>
                    <Badge variant={dim.score >= 70 ? 'success' : dim.score >= 50 ? 'warning' : 'error'}>
                      {Math.round(dim.score)}
                    </Badge>
                  </div>
                </div>
              ))}
          </div>
        </GlassCard>

        {/* Finance Snapshot */}
        <GlassCard variant="default" className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-text-primary flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-amber-400" />
              Wealth Health
            </h3>
          </div>
          
          <div className="space-y-4">
            <div>
              <p className="text-text-secondary text-sm mb-1">Net Worth</p>
              <p className="text-3xl font-bold text-text-primary">
                {new Intl.NumberFormat('en-ZA', {
                  style: 'currency',
                  currency: 'ZAR',
                  minimumFractionDigits: 0,
                  maximumFractionDigits: 0,
                }).format(snapshot.netWorthHomeCcy)}
              </p>
            </div>
            
            <div className="flex justify-between items-center">
              <span className="text-text-secondary text-sm">Wealth Health Score</span>
              <span className="text-2xl font-bold text-text-primary">
                {Math.round(snapshot.wealthHealthScore)}
              </span>
            </div>
            
            <div className="h-2 bg-gray-800 rounded-full overflow-hidden">
              <div 
                className="h-full bg-gradient-to-r from-amber-400 to-orange-600 transition-all duration-500"
                style={{ width: `${snapshot.wealthHealthScore}%` }}
              />
            </div>
          </div>
        </GlassCard>
      </div>

      {/* Wealth Projection Chart */}
      <GlassCard variant="default" className="p-6">
        <h3 className="text-lg font-semibold text-text-primary mb-4 flex items-center gap-2">
          <TrendingUp className="w-5 h-5 text-purple-400" />
          Wealth Projection
        </h3>
        {isLoadingHistory ? (
          <div className="flex items-center justify-center h-64">
            <Spinner size="md" />
          </div>
        ) : netWorthHistory?.data && netWorthHistory.data.length > 0 ? (
          <NetWorthChart 
            data={netWorthHistory.data} 
            currency="ZAR" 
            height={300}
          />
        ) : (
          <div className="flex items-center justify-center h-64 text-text-secondary">
            <p>No net worth history data available</p>
          </div>
        )}
      </GlassCard>

      {/* Dimensions Grid */}
      <GlassCard variant="default" className="p-6">
        <h3 className="text-lg font-semibold text-text-primary mb-4 flex items-center gap-2">
          <Calendar className="w-5 h-5 text-purple-400" />
          All Dimensions
        </h3>
        
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {(snapshot.dimensions || []).map((dim) => (
            <div
              key={dim.code}
              className="group p-4 rounded-lg bg-background-secondary border border-glass-border hover:border-accent-purple/50 transition-all cursor-pointer hover:shadow-glow-sm"
            >
              <p className="text-sm text-text-secondary capitalize mb-2 group-hover:text-text-primary transition-colors">
                {dim.code.replace('_', ' ')}
              </p>
              <p className="text-2xl font-bold text-text-primary">
                {Math.round(dim.score)}
              </p>
            </div>
          ))}
        </div>
      </GlassCard>

      {/* Next Key Events */}
      {(snapshot.nextKeyEvents || []).length > 0 && (
        <GlassCard variant="default" className="p-6">
          <h3 className="text-lg font-semibold text-text-primary mb-4 flex items-center gap-2">
            <Calendar className="w-5 h-5 text-yellow-400" />
            Upcoming Events
          </h3>
          <div className="space-y-2">
            {(snapshot.nextKeyEvents || []).map((event, idx) => (
              <div key={idx} className="flex items-center justify-between p-3 rounded-lg bg-gray-800/50">
                <span className="text-text-secondary capitalize">{event.type.replace('_', ' ')}</span>
                <Badge variant="info">{new Date(event.date).toLocaleDateString()}</Badge>
              </div>
            ))}
          </div>
        </GlassCard>
      )}
    </div>
  );
}
