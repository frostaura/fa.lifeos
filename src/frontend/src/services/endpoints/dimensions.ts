import { apiSlice } from '@store/api/apiSlice';
import type {
  DimensionListResponse,
  DimensionDetailResponse,
  UpdateDimensionWeightRequest,
} from '@/types';

export const dimensionsApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Get all dimensions
    getDimensions: builder.query<DimensionListResponse, void>({
      query: () => '/api/dimensions',
      providesTags: ['Dimensions'],
    }),

    // Get single dimension by ID with relationships
    getDimension: builder.query<DimensionDetailResponse, string>({
      query: (id) => `/api/dimensions/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Dimensions', id }],
    }),

    // Update dimension weight
    updateDimensionWeight: builder.mutation<
      void,
      { id: string } & UpdateDimensionWeightRequest
    >({
      query: ({ id, ...body }) => ({
        url: `/api/dimensions/${id}/weight`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['Dimensions', 'Dashboard'],
    }),
  }),
});

export const {
  useGetDimensionsQuery,
  useGetDimensionQuery,
  useUpdateDimensionWeightMutation,
} = dimensionsApi;
