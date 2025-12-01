import { apiSlice } from '@store/api/apiSlice';

// Types
export interface MilestoneAttributes {
  title: string;
  description: string | null;
  dimensionId: string;
  dimensionCode: string;
  targetDate: string | null;
  targetMetricCode: string | null;
  targetMetricValue: number | null;
  status: 'active' | 'completed' | 'abandoned';
  completedAt: string | null;
}

export interface MilestoneItem {
  id: string;
  type: 'milestone';
  attributes: MilestoneAttributes;
}

export interface MilestoneListResponse {
  data: MilestoneItem[];
}

export interface MilestoneDetailResponse {
  data: MilestoneItem;
}

export interface CreateMilestoneRequest {
  title: string;
  description?: string;
  dimensionId: string;
  targetDate?: string;
  targetMetricCode?: string;
  targetMetricValue?: number;
}

export interface UpdateMilestoneRequest {
  title?: string;
  description?: string;
  targetDate?: string;
  targetMetricCode?: string;
  targetMetricValue?: number;
  status?: 'active' | 'completed' | 'abandoned';
}

export const milestonesApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Get milestones (optionally filtered by dimensionId)
    getMilestones: builder.query<MilestoneListResponse, { dimensionId?: string } | void>({
      query: (params) => {
        const queryParams = params && params.dimensionId 
          ? `?dimensionId=${params.dimensionId}` 
          : '';
        return `/api/milestones${queryParams}`;
      },
      providesTags: (result) =>
        result
          ? [
              ...result.data.map(({ id }) => ({ type: 'Milestones' as const, id })),
              { type: 'Milestones', id: 'LIST' },
            ]
          : [{ type: 'Milestones', id: 'LIST' }],
    }),

    // Get single milestone
    getMilestone: builder.query<MilestoneDetailResponse, string>({
      query: (id) => `/api/milestones/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Milestones', id }],
    }),

    // Create milestone
    createMilestone: builder.mutation<MilestoneDetailResponse, CreateMilestoneRequest>({
      query: (body) => ({
        url: '/api/milestones',
        method: 'POST',
        body,
      }),
      invalidatesTags: [
        { type: 'Milestones', id: 'LIST' },
        'Dimensions',
        'Dashboard',
      ],
    }),

    // Update milestone
    updateMilestone: builder.mutation<MilestoneDetailResponse, { id: string } & UpdateMilestoneRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/milestones/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Milestones', id },
        { type: 'Milestones', id: 'LIST' },
        'Dimensions',
        'Dashboard',
      ],
    }),

    // Delete milestone
    deleteMilestone: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/milestones/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, id) => [
        { type: 'Milestones', id },
        { type: 'Milestones', id: 'LIST' },
        'Dimensions',
        'Dashboard',
      ],
    }),
  }),
});

export const {
  useGetMilestonesQuery,
  useGetMilestoneQuery,
  useCreateMilestoneMutation,
  useUpdateMilestoneMutation,
  useDeleteMilestoneMutation,
} = milestonesApi;
