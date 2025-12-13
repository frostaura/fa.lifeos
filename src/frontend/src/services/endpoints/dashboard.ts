import { apiSlice } from '@store/api/apiSlice';
import type { 
  DashboardData, 
  NetWorthData,
  NetWorthDataPoint,
  Projection,
  DimensionScore,
  Streak,
  TaskItem,
} from '@/types';
import type { DashboardSnapshot } from '@/types/mcp';

export const dashboardApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getDashboard: builder.query<DashboardData, void>({
      query: () => '/api/dashboard',
      providesTags: ['Dashboard'],
    }),
    
    // Dashboard snapshot - full snapshot with all metrics
    getDashboardSnapshot: builder.query<DashboardSnapshot, void>({
      query: () => '/api/dashboard',
      transformResponse: (response: { data: DashboardSnapshot }) => response.data,
      providesTags: ['Dashboard'],
    }),
    
    getNetWorth: builder.query<NetWorthData, { currency?: string }>({
      query: ({ currency = 'ZAR' }) => `/api/dashboard/net-worth?currency=${currency}`,
      providesTags: ['Dashboard'],
    }),
    
    getDimensionScores: builder.query<DimensionScore[], void>({
      query: () => '/api/dashboard/dimensions',
      providesTags: ['Dimensions'],
    }),
    
    getStreaks: builder.query<Streak[], void>({
      query: () => '/api/dashboard/streaks',
      providesTags: ['Dashboard'],
    }),
    
    getTodaysTasks: builder.query<TaskItem[], void>({
      query: () => '/api/dashboard/tasks/today',
      providesTags: ['Tasks'],
    }),
    
    getProjections: builder.query<Projection[], void>({
      query: () => '/api/dashboard/projections',
      providesTags: ['Projections'],
    }),
    
    getNetWorthHistory: builder.query<{ data: NetWorthDataPoint[] }, { months?: number; currency?: string }>({
      query: ({ months = 12, currency = 'ZAR' }) => 
        `/api/dashboard/net-worth/history?months=${months}&currency=${currency}`,
      transformResponse: (response: { data: { history: NetWorthDataPoint[]; summary: any }; meta: any }) => ({
        data: response.data.history
      }),
      providesTags: ['Dashboard'],
    }),
    
    completeTask: builder.mutation<void, { taskId: string; completed: boolean }>({
      query: ({ taskId, completed }) => ({
        url: `/api/tasks/${taskId}/complete`,
        method: 'POST',
        body: { completed },
      }),
      invalidatesTags: ['Tasks', 'Dashboard'],
    }),
  }),
});

export const {
  useGetDashboardQuery,
  useGetDashboardSnapshotQuery,
  useGetNetWorthQuery,
  useGetDimensionScoresQuery,
  useGetStreaksQuery,
  useGetTodaysTasksQuery,
  useGetProjectionsQuery,
  useGetNetWorthHistoryQuery,
  useCompleteTaskMutation,
} = dashboardApi;
