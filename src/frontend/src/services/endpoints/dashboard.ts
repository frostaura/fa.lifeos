import { apiSlice } from '@store/api/apiSlice';
import type { 
  DashboardData, 
  NetWorthData, 
  Projection,
  DimensionScore,
  Streak,
  TaskItem,
} from '@/types';

export const dashboardApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getDashboard: builder.query<DashboardData, void>({
      query: () => '/api/dashboard',
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
  useGetNetWorthQuery,
  useGetDimensionScoresQuery,
  useGetStreaksQuery,
  useGetTodaysTasksQuery,
  useGetProjectionsQuery,
  useCompleteTaskMutation,
} = dashboardApi;
