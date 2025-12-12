// MCP Tool Types - aligned with backend McpController

export interface DashboardSnapshot {
  lifeScore: number;
  healthIndex: number;
  adherenceIndex: number;
  wealthHealthScore: number;
  longevityYearsAdded: number;
  primaryStats: {
    strength: number;
    wisdom: number;
    charisma: number;
    composure: number;
    energy: number;
    influence: number;
    vitality: number;
  };
  dimensions: Array<{
    code: string;
    score: number;
  }>;
  todayTasks: Array<{
    taskId: string;
    dimensionCode: string;
    title: string;
    isCompleted: boolean;
  }>;
  netWorthHomeCcy: number;
  nextKeyEvents: Array<{
    type: string;
    date: string;
  }>;
}

export interface RecordMetricsRequest {
  timestamp: string;
  source: string;
  metrics: Record<string, any>;
}

export interface RecordMetricsResponse {
  success: boolean;
  createdRecords: number;
}

export interface ListTasksRequest {
  dimensionCode?: string;
  onlyActive?: boolean;
}

export interface TaskStreak {
  currentStreakLength: number;
  longestStreakLength: number;
}

export interface ListTasksResponse {
  tasks: Array<{
    id: string;
    dimensionCode: string;
    title: string;
    emoji: string | null;
    frequency: string;
    isHabit: boolean;
    lastCompletedAt: string | null;
    streak: TaskStreak | null;
  }>;
}

export interface CompleteTaskRequest {
  taskId: string;
  timestamp: string;
  valueNumber?: number;
}

export interface CompleteTaskResponse {
  success: boolean;
  taskCompletionId: string;
  updatedStreak: {
    currentStreakLength: number;
    longestStreakLength: number;
    riskPenaltyScore: number;
  } | null;
}

export interface WeeklyReviewResponse {
  period: {
    start: string;
    end: string;
  };
  healthIndexChange: {
    from: number;
    to: number;
  };
  adherenceIndexChange: {
    from: number;
    to: number;
  };
  wealthHealthChange: {
    from: number;
    to: number;
  };
  longevityChange: {
    from: number;
    to: number;
  };
  topStreaks: Array<{
    taskTitle: string;
    length: number;
  }>;
  atRiskStreaks: Array<{
    taskTitle: string;
    consecutiveMisses: number;
  }>;
  focusActions: string[];
}

export interface MonthlyReviewResponse {
  period: {
    start: string;
    end: string;
  };
  netWorthChange: {
    from: number;
    to: number;
    percentChange: number;
  };
  primaryStatsChange: Record<string, {
    from: number;
    to: number;
  }>;
  milestonesCompleted: Array<{
    title: string;
    completedAt: string;
  }>;
  topMetricImprovements: Array<{
    metricName: string;
    improvement: string;
  }>;
  focusAreas: string[];
}

export interface IdentityProfileResponse {
  title: string;
  description: string;
  values: string[];
  primaryStatTargets: Record<string, number>;
  linkedMilestones: Array<{
    id: string;
    title: string;
  }>;
}

export interface UpdateIdentityTargetsRequest {
  primaryStatTargets: Record<string, number>;
}

export interface UpdateIdentityTargetsResponse {
  success: boolean;
  message: string;
}
