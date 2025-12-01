import { useState, useEffect } from 'react';
import {
  LifeScoreCard,
  NetWorthCard,
  DimensionGrid,
  StreaksWidget,
  TodaysTasksList,
  NetWorthChart,
} from '@components/organisms';
import { GlassCard } from '@components/atoms/GlassCard';
import { Spinner } from '@components/atoms/Spinner';
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

interface NetWorthHistoryApiResponse {
  data: {
    history: Array<{
      date: string;
      value: number;
    }>;
    summary: {
      currentNetWorth: number;
      change: number;
      changePercent: number;
    };
  };
}

export function Dashboard() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dashboardData, setDashboardData] = useState<DashboardApiResponse['data'] | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [netWorthHistory, setNetWorthHistory] = useState<NetWorthDataPoint[]>([]);

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

        // Fetch net worth history from API
        try {
          const historyRes = await fetch('/api/dashboard/net-worth/history?period=1Y', { headers });
          if (historyRes.ok) {
            const historyData: NetWorthHistoryApiResponse = await historyRes.json();
            if (historyData.data?.history?.length > 0) {
              setNetWorthHistory(historyData.data.history.map(h => ({
                date: h.date,
                value: h.value,
              })));
            } else {
              // Fallback to generated data if no history exists yet
              setNetWorthHistory(generateFallbackHistory(data.data.netWorth.value));
            }
          } else {
            setNetWorthHistory(generateFallbackHistory(data.data.netWorth.value));
          }
        } catch {
          setNetWorthHistory(generateFallbackHistory(data.data.netWorth.value));
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, []);
  
  // Generate fallback history when no API data exists
  const generateFallbackHistory = (currentNetWorth: number): NetWorthDataPoint[] => {
    const history: NetWorthDataPoint[] = [];
    for (let i = 10; i >= 0; i--) {
      const date = new Date();
      date.setMonth(date.getMonth() - i);
      const variance = (Math.random() - 0.3) * 0.1;
      history.push({
        date: date.toISOString().slice(0, 7),
        value: Math.round(currentNetWorth * (1 - (i * 0.02) + variance)),
      });
    }
    return history;
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
          <h1 className="text-3xl font-bold text-text-primary">Dashboard</h1>
          <p className="text-text-secondary mt-1">Welcome back to LifeOS</p>
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

      {/* Dimension Cards */}
      <div>
        <h2 className="text-xl font-semibold text-text-primary mb-4">Dimensions</h2>
        <DimensionGrid dimensions={dimensions} />
      </div>

      {/* Net Worth Chart */}
      <GlassCard variant="default" className="p-6">
        <h2 className="text-lg font-semibold text-text-primary mb-4">Net Worth Trend</h2>
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
