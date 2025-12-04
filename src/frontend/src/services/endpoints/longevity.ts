import { apiSlice } from '@store/api/apiSlice';

export interface LongevityModel {
  id: string;
  code: string;
  name: string;
  description: string | null;
  inputMetrics: string[];
  modelType: string;
  parameters: string;
  sourceCitation: string | null;
  sourceUrl: string | null;
  isActive: boolean;
  version: number;
}

interface LongevityModelItemResponse {
  id: string;
  type: string;
  attributes: {
    code: string;
    name: string;
    description: string | null;
    inputMetrics: string[];
    modelType: string;
    parameters: string;
    sourceCitation: string | null;
    sourceUrl: string | null;
    isActive: boolean;
    version: number;
  };
}

interface LongevityModelsListResponse {
  data: LongevityModelItemResponse[];
}

export interface UpdateLongevityModelRequest {
  name?: string;
  description?: string;
  parameters?: string;
  isActive?: boolean;
}

export const longevityApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getLongevityModels: builder.query<LongevityModel[], void>({
      query: () => '/api/longevity-models',
      providesTags: ['LongevityModels'],
      transformResponse: (response: LongevityModelsListResponse): LongevityModel[] =>
        response.data.map((item) => ({
          id: item.id,
          code: item.attributes.code,
          name: item.attributes.name,
          description: item.attributes.description,
          inputMetrics: item.attributes.inputMetrics,
          modelType: item.attributes.modelType,
          parameters: item.attributes.parameters,
          sourceCitation: item.attributes.sourceCitation,
          sourceUrl: item.attributes.sourceUrl,
          isActive: item.attributes.isActive,
          version: item.attributes.version,
        })),
    }),

    updateLongevityModel: builder.mutation<void, { id: string } & UpdateLongevityModelRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/longevity-models/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['LongevityModels'],
    }),
  }),
});

export const {
  useGetLongevityModelsQuery,
  useUpdateLongevityModelMutation,
} = longevityApi;
