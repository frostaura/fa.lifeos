import { apiSlice } from '@store/api/apiSlice';

// Types
export interface LifeTaskAttributes {
  title: string;
  description: string | null;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency: 'daily' | 'weekly' | 'monthly' | 'ad_hoc';
  dimensionId: string | null;
  dimensionCode: string | null;
  milestoneId: string | null;
  linkedMetricCode: string | null;
  scheduledDate: string | null;
  scheduledTime: string | null;
  startDate: string;
  endDate: string | null;
  isCompleted: boolean;
  completedAt: string | null;
  isActive: boolean;
  tags: string[];
  streakDays?: number;
}

export interface LifeTaskItem {
  id: string;
  type: 'task';
  attributes: LifeTaskAttributes;
}

export interface TaskListResponse {
  data: LifeTaskItem[];
  meta: {
    page: number;
    perPage: number;
    total: number;
    totalPages: number;
  };
}

export interface TaskDetailResponse {
  data: LifeTaskItem;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency?: 'daily' | 'weekly' | 'monthly' | 'ad_hoc';
  dimensionId?: string;
  milestoneId?: string;
  linkedMetricCode?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  startDate?: string;
  endDate?: string;
  tags?: string[];
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  frequency?: 'daily' | 'weekly' | 'monthly' | 'ad_hoc';
  scheduledDate?: string;
  scheduledTime?: string;
  endDate?: string;
  isActive?: boolean;
  tags?: string[];
}

export interface CompleteTaskRequest {
  completedAt?: string;
  metricValue?: number;
}

export interface CompleteTaskResponse {
  data: LifeTaskItem;
  meta: {
    streakUpdated: boolean;
    newStreakLength?: number;
    metricRecorded: boolean;
  };
}

export interface GetTasksParams {
  taskType?: string;
  dimensionId?: string;
  milestoneId?: string;
  isCompleted?: boolean;
  isActive?: boolean;
  scheduledFrom?: string;
  scheduledTo?: string;
  tags?: string;
  page?: number;
  perPage?: number;
}

export const tasksApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Get tasks with filtering
    getTasks: builder.query<TaskListResponse, GetTasksParams | void>({
      query: (params) => {
        const queryParams = new URLSearchParams();
        if (params) {
          if (params.taskType) queryParams.append('taskType', params.taskType);
          if (params.dimensionId) queryParams.append('dimensionId', params.dimensionId);
          if (params.milestoneId) queryParams.append('milestoneId', params.milestoneId);
          if (params.isCompleted !== undefined) queryParams.append('isCompleted', String(params.isCompleted));
          if (params.isActive !== undefined) queryParams.append('isActive', String(params.isActive));
          if (params.scheduledFrom) queryParams.append('scheduledFrom', params.scheduledFrom);
          if (params.scheduledTo) queryParams.append('scheduledTo', params.scheduledTo);
          if (params.tags) queryParams.append('tags', params.tags);
          if (params.page) queryParams.append('page', String(params.page));
          if (params.perPage) queryParams.append('perPage', String(params.perPage));
        }
        const queryString = queryParams.toString();
        return `/api/tasks${queryString ? `?${queryString}` : ''}`;
      },
      providesTags: (result) =>
        result
          ? [
              ...result.data.map(({ id }) => ({ type: 'Tasks' as const, id })),
              { type: 'Tasks', id: 'LIST' },
            ]
          : [{ type: 'Tasks', id: 'LIST' }],
    }),

    // Get single task
    getTask: builder.query<TaskDetailResponse, string>({
      query: (id) => `/api/tasks/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Tasks', id }],
    }),

    // Create task
    createTask: builder.mutation<TaskDetailResponse, CreateTaskRequest>({
      query: (body) => ({
        url: '/api/tasks',
        method: 'POST',
        body,
      }),
      invalidatesTags: [
        { type: 'Tasks', id: 'LIST' },
        'Dimensions',
        'Dashboard',
      ],
    }),

    // Update task
    updateTask: builder.mutation<TaskDetailResponse, { id: string } & UpdateTaskRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/tasks/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Tasks', id },
        { type: 'Tasks', id: 'LIST' },
        'Dimensions',
        'Dashboard',
      ],
    }),

    // Delete task
    deleteTask: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/tasks/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, id) => [
        { type: 'Tasks', id },
        { type: 'Tasks', id: 'LIST' },
        'Dimensions',
        'Dashboard',
      ],
    }),

    // Complete task
    completeTask: builder.mutation<CompleteTaskResponse, { id: string } & CompleteTaskRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/tasks/${id}/complete`,
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Tasks', id },
        { type: 'Tasks', id: 'LIST' },
        'Dimensions',
        'Dashboard',
      ],
    }),
  }),
});

export const {
  useGetTasksQuery,
  useGetTaskQuery,
  useCreateTaskMutation,
  useUpdateTaskMutation,
  useDeleteTaskMutation,
  useCompleteTaskMutation,
} = tasksApi;
