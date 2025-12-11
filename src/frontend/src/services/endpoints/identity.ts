import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { RootState } from '../../store';

// Types
export interface IdentityProfile {
  archetype: string;
  archetypeDescription?: string;
  values: string[];
  primaryStatTargets: Record<string, number>;
  linkedMilestones: Array<{ id: string; title: string }>;
}

export interface IdentityProfileRequest {
  archetype: string;
  archetypeDescription?: string;
  values?: string[];
  primaryStatTargets?: Record<string, number>;
  linkedMilestoneIds?: string[];
}

export interface PrimaryStats {
  currentStats: Record<string, number>;
  targets: Record<string, number>;
  calculatedAt: string;
  breakdown?: Record<string, { fromDimensions: Record<string, number>; weighted: number }>;
}

export interface PrimaryStatHistoryPoint {
  recordedAt: string;
  strength: number;
  wisdom: number;
  charisma: number;
  composure: number;
  energy: number;
  influence: number;
  vitality: number;
}

export interface ReviewSnapshot {
  periodStart: string;
  periodEnd: string;
  healthIndex: number | null;
  adherenceIndex: number | null;
  wealthHealth: number | null;
  longevity: number | null;
  healthIndexDelta: number | null;
  adherenceIndexDelta: number | null;
  wealthHealthDelta: number | null;
  longevityDelta: number | null;
  topStreaks: Array<{ taskId: string; taskTitle: string; streakDays: number }>;
  recommendedActions: Array<{ action: string; priority: string; dimension: string }>;
  primaryStats: Record<string, number>;
  primaryStatsDelta: Record<string, number>;
  financialSummary?: {
    netWorth: number | null;
    netWorthDelta: number | null;
    totalIncome: number | null;
    totalExpenses: number | null;
    netCashFlow: number | null;
    savingsRate: number | null;
  };
  dimensionScores?: Record<string, number>;
  generatedAt: string;
}

export interface DimensionReview {
  dimension: {
    code: string;
    name: string;
    score: number;
  };
  periodStart: string;
  periodEnd: string;
  period: string;
  metrics: Array<{
    code: string;
    name: string;
    unit: string;
    currentValue: number | null;
    previousValue: number | null;
    delta: number | null;
    recordCount: number;
  }>;
  streaks: Array<{
    taskTitle: string;
    currentStreak: number;
    longestStreak: number;
    isActive: boolean;
  }>;
  activeMilestones: number;
  completedMilestones: number;
  recommendedActions: Array<{ action: string; priority: string; dimension: string }>;
  generatedAt: string;
}

export interface FinancialReview {
  periodStart: string;
  periodEnd: string;
  period: string;
  summary: {
    netWorth: number;
    totalAssets: number;
    totalLiabilities: number;
    wealthHealthScore: number;
    netCashFlow: number;
    totalIncome: number;
    totalExpenses: number;
    savingsRate: number;
  };
  accountBreakdown: Array<{
    type: string;
    totalBalance: number;
    count: number;
  }>;
  projections: {
    projectedNetWorthIn12Months: number;
    projectedGrowth: number;
    monthlyData: Array<{
      month: string;
      netWorth: number;
      income: number;
      expenses: number;
    }>;
  } | null;
  recommendedActions: Array<{ action: string; priority: string; dimension: string }>;
  generatedAt: string;
}

export interface OnboardingStatus {
  isComplete: boolean;
  steps: Array<{ step: string; completed: boolean }>;
  currentStep: string;
}

export interface HealthBaselinesRequest {
  currentWeight: number;
  targetWeight: number;
  currentBodyFat?: number;
  targetBodyFat?: number;
  height: number;
  birthDate?: string;
}

export interface MajorGoalsRequest {
  financialGoals?: Array<{
    description: string;
    targetAmount: number;
    targetAge: number;
  }>;
  lifeMilestones?: Array<{
    description: string;
    targetDate?: string;
    dimension?: string;
  }>;
}

export interface IdentityStepRequest {
  archetype?: string;
  values?: string[];
  primaryStatFocus?: string[];
}

// API
export const identityApi = createApi({
  reducerPath: 'identityApi',
  baseQuery: fetchBaseQuery({
    baseUrl: '/api',
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth?.token || localStorage.getItem('accessToken');
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return headers;
    },
  }),
  tagTypes: ['IdentityProfile', 'PrimaryStats', 'Reviews', 'Onboarding'],
  endpoints: (builder) => ({
    // Identity Profile
    getIdentityProfile: builder.query<IdentityProfile, void>({
      query: () => '/identity-profile',
      transformResponse: (response: { data: IdentityProfile }) => response.data,
      providesTags: ['IdentityProfile'],
    }),
    updateIdentityProfile: builder.mutation<IdentityProfile, IdentityProfileRequest>({
      query: (body) => ({
        url: '/identity-profile',
        method: 'PUT',
        body,
      }),
      transformResponse: (response: { data: IdentityProfile }) => response.data,
      invalidatesTags: ['IdentityProfile'],
    }),

    // Primary Stats
    getPrimaryStats: builder.query<PrimaryStats, void>({
      query: () => '/primary-stats',
      transformResponse: (response: { data: PrimaryStats }) => response.data,
      providesTags: ['PrimaryStats'],
    }),
    getPrimaryStatsHistory: builder.query<PrimaryStatHistoryPoint[], { days?: number }>({
      query: ({ days = 30 }) => `/primary-stats/history?days=${days}`,
      transformResponse: (response: { data: PrimaryStatHistoryPoint[] }) => response.data,
    }),
    recalculatePrimaryStats: builder.mutation<PrimaryStats, void>({
      query: () => ({
        url: '/primary-stats/recalculate',
        method: 'POST',
      }),
      transformResponse: (response: { data: PrimaryStats }) => response.data,
      invalidatesTags: ['PrimaryStats'],
    }),

    // Reviews
    getWeeklyReview: builder.query<ReviewSnapshot, void>({
      query: () => '/reviews/weekly',
      transformResponse: (response: { data: ReviewSnapshot }) => response.data,
      providesTags: ['Reviews'],
    }),
    getMonthlyReview: builder.query<ReviewSnapshot, void>({
      query: () => '/reviews/monthly',
      transformResponse: (response: { data: ReviewSnapshot }) => response.data,
      providesTags: ['Reviews'],
    }),
    getReviewHistory: builder.query<ReviewSnapshot[], { type?: string; count?: number }>({
      query: ({ type = 'weekly', count = 10 }) => `/reviews/history?type=${type}&count=${count}`,
      transformResponse: (response: { data: ReviewSnapshot[] }) => response.data,
    }),
    generateReview: builder.mutation<ReviewSnapshot, { type?: string }>({
      query: ({ type = 'weekly' }) => ({
        url: `/reviews/generate?type=${type}`,
        method: 'POST',
      }),
      transformResponse: (response: { data: ReviewSnapshot }) => response.data,
      invalidatesTags: ['Reviews'],
    }),
    getDimensionReview: builder.query<DimensionReview, { dimensionCode: string; period?: string }>({
      query: ({ dimensionCode, period = 'weekly' }) => `/reviews/dimension/${dimensionCode}?period=${period}`,
      transformResponse: (response: { data: DimensionReview }) => response.data,
      providesTags: ['Reviews'],
    }),
    getFinancialReview: builder.query<FinancialReview, { period?: string }>({
      query: ({ period = 'monthly' }) => `/reviews/financial?period=${period}`,
      transformResponse: (response: { data: FinancialReview }) => response.data,
      providesTags: ['Reviews'],
    }),

    // Onboarding
    getOnboardingStatus: builder.query<OnboardingStatus, void>({
      query: () => '/onboarding/status',
      transformResponse: (response: { data: OnboardingStatus }) => response.data,
      providesTags: ['Onboarding'],
    }),
    submitHealthBaselines: builder.mutation<void, HealthBaselinesRequest>({
      query: (body) => ({
        url: '/onboarding/health-baselines',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Onboarding'],
    }),
    submitMajorGoals: builder.mutation<void, MajorGoalsRequest>({
      query: (body) => ({
        url: '/onboarding/major-goals',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Onboarding'],
    }),
    submitIdentity: builder.mutation<void, IdentityStepRequest>({
      query: (body) => ({
        url: '/onboarding/identity',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Onboarding', 'IdentityProfile'],
    }),
    completeOnboarding: builder.mutation<void, void>({
      query: () => ({
        url: '/onboarding/complete',
        method: 'POST',
      }),
      invalidatesTags: ['Onboarding'],
    }),
  }),
});

export const {
  useGetIdentityProfileQuery,
  useUpdateIdentityProfileMutation,
  useGetPrimaryStatsQuery,
  useGetPrimaryStatsHistoryQuery,
  useRecalculatePrimaryStatsMutation,
  useGetWeeklyReviewQuery,
  useGetMonthlyReviewQuery,
  useGetReviewHistoryQuery,
  useGenerateReviewMutation,
  useGetDimensionReviewQuery,
  useGetFinancialReviewQuery,
  useGetOnboardingStatusQuery,
  useSubmitHealthBaselinesMutation,
  useSubmitMajorGoalsMutation,
  useSubmitIdentityMutation,
  useCompleteOnboardingMutation,
} = identityApi;
