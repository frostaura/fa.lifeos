import { apiSlice } from './apiSlice';
import type {
  DashboardSnapshot,
  RecordMetricsRequest,
  RecordMetricsResponse,
  ListTasksRequest,
  ListTasksResponse,
  CompleteTaskRequest,
  CompleteTaskResponse,
  WeeklyReviewResponse,
  MonthlyReviewResponse,
  IdentityProfileResponse,
  UpdateIdentityTargetsRequest,
  UpdateIdentityTargetsResponse,
} from '../../types/mcp';

export const mcpApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Dashboard Snapshot
    getDashboardSnapshot: builder.query<DashboardSnapshot, void>({
      query: () => ({
        url: '/api/mcp/tools/lifeos.getDashboardSnapshot',
        method: 'POST',
      }),
      transformResponse: (response: { success: boolean; data: DashboardSnapshot }) => response.data,
      providesTags: ['Dashboard'],
    }),

    // Record Metrics
    recordMetrics: builder.mutation<RecordMetricsResponse, RecordMetricsRequest>({
      query: (request) => ({
        url: '/api/mcp/tools/lifeos.recordMetrics',
        method: 'POST',
        body: request,
      }),
      invalidatesTags: ['Dashboard', 'Metrics', 'Health'],
    }),

    // List Tasks
    listTasks: builder.query<ListTasksResponse, ListTasksRequest>({
      query: (params) => ({
        url: '/api/mcp/tools/lifeos.listTasks',
        method: 'POST',
        body: params,
      }),
      providesTags: ['Tasks'],
    }),

    // Complete Task
    completeTask: builder.mutation<CompleteTaskResponse, CompleteTaskRequest>({
      query: (request) => ({
        url: '/api/mcp/tools/lifeos.completeTask',
        method: 'POST',
        body: request,
      }),
      invalidatesTags: ['Tasks', 'Dashboard'],
    }),

    // Weekly Review
    getWeeklyReview: builder.query<WeeklyReviewResponse, { weekStartDate: string }>({
      query: (params) => ({
        url: '/api/mcp/tools/lifeos.getWeeklyReview',
        method: 'POST',
        body: params,
      }),
    }),

    // Monthly Review
    getMonthlyReview: builder.query<MonthlyReviewResponse, { monthStartDate: string }>({
      query: (params) => ({
        url: '/api/mcp/tools/lifeos.getMonthlyReview',
        method: 'POST',
        body: params,
      }),
    }),

    // Identity Profile
    getIdentityProfile: builder.query<IdentityProfileResponse, void>({
      query: () => ({
        url: '/api/mcp/tools/lifeos.getIdentityProfile',
        method: 'POST',
      }),
    }),

    // Update Identity Targets
    updateIdentityTargets: builder.mutation<UpdateIdentityTargetsResponse, UpdateIdentityTargetsRequest>({
      query: (request) => ({
        url: '/api/mcp/tools/lifeos.updateIdentityTargets',
        method: 'POST',
        body: request,
      }),
      invalidatesTags: ['Dashboard'],
    }),
  }),
});

export const {
  useGetDashboardSnapshotQuery,
  useRecordMetricsMutation,
  useListTasksQuery,
  useCompleteTaskMutation,
  useGetWeeklyReviewQuery,
  useGetMonthlyReviewQuery,
  useGetIdentityProfileQuery,
  useUpdateIdentityTargetsMutation,
} = mcpApi;
