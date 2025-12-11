import { useState, useEffect } from 'react';
import {
  LifeScoreCard,
  NetWorthCard,
  DimensionGrid,
  StreaksWidget,
  TodaysTasksList,
  NetWorthChart,
  IdentityRadar,
} from '@components/organisms';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
import { cn } from '@utils/cn';
import { useGetPrimaryStatsQuery } from '@/services/endpoints';
import type { Streak, TaskItem, NetWorthDataPoint, DimensionId } from '@/types';

// API response types
interface DashboardApiResponse {
  data: {
    lifeScore: number;
    lifeScoreTrend: number;
    netWorth: {
      value: number;
      totalAssets: number;
      totalLiabilities: number;
      currency: string;
      change: number;
      changePercent: number;
    };
    dimensions: Array<{
      id: string;
      code: string;
      name: string;
      icon: string;
      score: number;
      trend: number;
      activeMilestones: number;
    }>;
    streaks: Array<{
      id: string;
      name: string;
      habitId: string;
      currentDays: number;
      longestDays: number;
      lastCompletedAt: string | null;
    }>;
    tasks: Array<{
      id: string;
      title: string;
      completed: boolean;
      dimensionId: string | null;
    }>;
  };
}



export function Dashboard() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dashboardData, setDashboardData] = useState<DashboardApiResponse['data'] | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [netWorthHistory, setNetWorthHistory] = useState<NetWorthDataPoint[]>([]);
  const [chartPeriod, setChartPeriod] = useState<'1M' | '3M' | '6M' | '1Y' | '5Y' | '10Y' | 'ALL'>('5Y');

  // Fetch primary stats for Identity Radar
  const { data: primaryStats } = useGetPrimaryStatsQuery();

  // Fetch projections from baseline scenario
  const fetchProjections = async (period: string) => {
    const token = localStorage.getItem('accessToken');
    const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};
    
    try {
      // First get scenarios to find baseline
      const scenariosRes = await fetch('/api/simulations/scenarios', { headers });
      if (!scenariosRes.ok) return null;
      
      const scenariosData = await scenariosRes.json();
      const baseline = scenariosData.data?.find((s: { attributes: { isBaseline: boolean } }) => s.attributes.isBaseline);
      
      if (!baseline) return null;
      
      // Run simulation to ensure fresh data
      await fetch(`/api/simulations/scenarios/${baseline.id}/run`, {
        method: 'POST',
        headers: { ...headers, 'Content-Type': 'application/json' },
        body: JSON.stringify({ recalculateFromStart: true }),
      });
      
      // Fetch projections
      const projectionsRes = await fetch(`/api/simulations/scenarios/${baseline.id}/projections`, { headers });
      if (!projectionsRes.ok) return null;
      
      const projectionsData = await projectionsRes.json();
      if (!projectionsData.data?.monthlyProjections?.length) return null;
      
      // Map and filter based on period, including accounts data
      const allProjections: NetWorthDataPoint[] = projectionsData.data.monthlyProjections.map((p: { 
        period: string; 
        netWorth: number;
        accounts?: Array<{
          accountId: string;
          accountName: string;
          balance: number;
        }>;
      }) => ({
        date: p.period,
        value: Math.round(p.netWorth),
        accounts: p.accounts?.map(a => ({
          accountId: a.accountId,
          accountName: a.accountName,
          balance: a.balance,
        })),
      }));
      
      const monthsToShow = {
        '1M': 1,
        '3M': 3,
        '6M': 6,
        '1Y': 12,
        '5Y': 60,
        '10Y': 120,
        'ALL': allProjections.length,
      }[period] || 60;
      
      return allProjections.slice(0, monthsToShow + 1);
    } catch {
      return null;
    }
  };

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        // Get token from localStorage (would be set by auth flow)
        const token = localStorage.getItem('accessToken');
        const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};
        
        const response = await fetch('/api/dashboard', { headers });
        
        if (!response.ok) {
          throw new Error('Failed to fetch dashboard data');
        }
        
        const data: DashboardApiResponse = await response.json();
        setDashboardData(data.data);
        setTasks(data.data.tasks.map(t => ({
          id: t.id,
          title: t.title,
          completed: t.completed,
          dimensionId: (t.dimensionId || 'health') as DimensionId,
        })));

        // Fetch real projections from baseline scenario
        const projections = await fetchProjections(chartPeriod);
        if (projections && projections.length > 0) {
          setNetWorthHistory(projections);
        } else {
          // Fallback to simple projection if no baseline scenario
          setNetWorthHistory(generateProjectionsForDashboard(data.data.netWorth.value, chartPeriod));
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, []);

  // Update projections when chart period changes
  useEffect(() => {
    if (!dashboardData) return;
    
    const updateProjections = async () => {
      const projections = await fetchProjections(chartPeriod);
      if (projections && projections.length > 0) {
        setNetWorthHistory(projections);
      } else {
        setNetWorthHistory(generateProjectionsForDashboard(dashboardData.netWorth.value, chartPeriod));
      }
    };
    
    updateProjections();
  }, [chartPeriod, dashboardData]);
  
  // Generate future projections based on current net worth (fallback)
  const generateProjectionsForDashboard = (currentNetWorth: number, period: string): NetWorthDataPoint[] => {
    const projections: NetWorthDataPoint[] = [];
    const periodMonths: Record<string, number> = {
      '1M': 1,
      '3M': 3,
      '6M': 6,
      '1Y': 12,
      '5Y': 60,
      '10Y': 120,
      'ALL': 240,
    };
    const months = periodMonths[period] || 60;
    
    // Assume modest growth rate of ~5% annually for dashboard overview
    const monthlyGrowthRate = 0.05 / 12;
    let projectedValue = currentNetWorth;
    
    for (let i = 0; i <= months; i++) {
      const date = new Date();
      date.setMonth(date.getMonth() + i);
      projections.push({
        date: date.toISOString().slice(0, 7),
        value: Math.round(projectedValue),
      });
      // Apply growth for next month
      projectedValue = projectedValue * (1 + monthlyGrowthRate);
    }
    return projections;
  };

  const handleToggleTask = async (id: string, completed: boolean) => {
    setTasks((prev) =>
      prev.map((t) => (t.id === id ? { ...t, completed } : t))
    );
    
    // Call API to update task
    try {
      const token = localStorage.getItem('accessToken');
      await fetch(`/api/tasks/${id}/complete`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ completed }),
      });
    } catch (err) {
      console.error('Failed to update task:', err);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !dashboardData) {
    return (
      <div className="text-center text-red-400 p-8">
        <p>Error loading dashboard: {error}</p>
        <button 
          onClick={() => window.location.reload()} 
          className="mt-4 px-4 py-2 bg-primary-500 rounded-lg"
        >
          Retry
        </button>
      </div>
    );
  }

  const dimensions = dashboardData.dimensions.map(d => ({
    id: d.code,
    name: d.name,
    score: d.score,
    trend: d.trend,
    activeMilestones: d.activeMilestones,
  }));

  const streaks: Streak[] = dashboardData.streaks.map(s => ({
    id: s.id,
    name: s.name || 'Unknown',
    habitId: s.habitId,
    currentDays: s.currentDays,
    longestDays: s.longestDays,
    lastCompletedAt: s.lastCompletedAt || new Date().toISOString(),
  }));

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl md:text-2xl lg:text-3xl font-bold text-text-primary whitespace-nowrap">Dashboard</h1>
          <p className="text-text-secondary mt-1 text-sm md:text-base whitespace-nowrap">Welcome back to LifeOS</p>
        </div>
      </div>

      {/* Life Score & Net Worth Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <LifeScoreCard score={dashboardData.lifeScore} trend={dashboardData.lifeScoreTrend} />
        <NetWorthCard 
          value={dashboardData.netWorth.value} 
          change={dashboardData.netWorth.change} 
          changePercent={dashboardData.netWorth.changePercent} 
        />
      </div>

      {/* Identity Radar - v1.1 */}
      {primaryStats && (
        <GlassCard variant="default" className="p-4 md:p-6">
          <h2 className="text-base md:text-lg font-semibold text-text-primary mb-4">Identity Stats</h2>
          <IdentityRadar
            currentStats={primaryStats.currentStats}
            targetStats={primaryStats.targets}
            size="md"
          />
        </GlassCard>
      )}

      {/* Dimension Cards */}
      <div>
        <h2 className="text-base md:text-lg lg:text-xl font-semibold text-text-primary mb-4 whitespace-nowrap">Dimensions</h2>
        <DimensionGrid dimensions={dimensions} />
      </div>

      {/* Net Worth Chart */}
      <GlassCard variant="default" className="p-4 md:p-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-4">
          <h2 className="text-base md:text-lg font-semibold text-text-primary whitespace-nowrap">Net Worth Trend</h2>
          <div className="flex gap-1 flex-wrap">
            {(['1M', '3M', '6M', '1Y', '5Y', '10Y', 'ALL'] as const).map((period) => (
              <button
                key={period}
                onClick={() => setChartPeriod(period)}
                className={cn(
                  'px-2 py-0.5 text-xs rounded-md transition-colors',
                  chartPeriod === period 
                    ? 'bg-accent-purple text-white' 
                    : 'text-text-secondary hover:bg-background-hover'
                )}
              >
                {period}
              </button>
            ))}
          </div>
        </div>
        <NetWorthChart data={netWorthHistory} height={250} />
      </GlassCard>

      {/* Active Streaks & Tasks */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <StreaksWidget streaks={streaks} />
        <TodaysTasksList tasks={tasks} onToggleTask={handleToggleTask} />
      </div>
    </div>
  );
}
