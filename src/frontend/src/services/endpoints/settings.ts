import { apiSlice } from '@store/api/apiSlice';

export interface ProfileSettings {
  email: string;
  username: string | null;
  homeCurrency: string;
  dateOfBirth: string | null;
  lifeExpectancyBaseline: number;
  defaultAssumptions: {
    inflationRateAnnual: number;
    defaultGrowthRate: number;
    retirementAge: number;
  };
  dimensions: Array<{
    id: string;
    code: string;
    name: string;
    weight: number;
    icon: string;
  }>;
}

export interface ApiKeyInfo {
  id: string;
  name: string;
  keyPrefix: string;
  scopes: string;
  createdAt: string;
  expiresAt: string | null;
  lastUsedAt: string | null;
}

export interface CreateApiKeyResponse {
  id: string;
  name: string;
  key: string;
  keyPrefix: string;
  scopes: string;
  expiresAt: string | null;
  warning: string;
}

export interface UpdateProfileRequest {
  username?: string;
  homeCurrency?: string;
  dateOfBirth?: string;
  lifeExpectancyBaseline?: number;
  defaultAssumptions?: {
    inflationRateAnnual?: number;
    defaultGrowthRate?: number;
    retirementAge?: number;
  };
}

export interface CreateApiKeyRequest {
  name?: string;
  scopes?: string;
  expiresInDays?: number;
}

export interface DimensionWeight {
  dimensionId: string;
  weight: number;
}

export const settingsApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Profile
    getProfile: builder.query<ProfileSettings, void>({
      query: () => '/api/settings/profile',
      transformResponse: (response: { data: ProfileSettings }) => response.data,
      providesTags: ['Settings'],
    }),

    updateProfile: builder.mutation<void, UpdateProfileRequest>({
      query: (body) => ({
        url: '/api/settings/profile',
        method: 'PUT',
        body,
      }),
      invalidatesTags: ['Settings', 'Dashboard'],
    }),

    updateDimensionWeights: builder.mutation<void, DimensionWeight[]>({
      query: (weights) => ({
        url: '/api/settings/dimensions/weights',
        method: 'PUT',
        body: { weights },
      }),
      invalidatesTags: ['Settings', 'Dimensions', 'Dashboard'],
    }),

    // API Keys
    getApiKeys: builder.query<ApiKeyInfo[], void>({
      query: () => '/api/settings/api-keys',
      transformResponse: (response: { data: ApiKeyInfo[] }) => response.data,
      providesTags: ['Settings'],
    }),

    createApiKey: builder.mutation<CreateApiKeyResponse, CreateApiKeyRequest>({
      query: (body) => ({
        url: '/api/settings/api-keys',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: CreateApiKeyResponse }) => response.data,
      invalidatesTags: ['Settings'],
    }),

    revokeApiKey: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/settings/api-keys/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Settings'],
    }),
  }),
});

export const {
  useGetProfileQuery,
  useUpdateProfileMutation,
  useUpdateDimensionWeightsMutation,
  useGetApiKeysQuery,
  useCreateApiKeyMutation,
  useRevokeApiKeyMutation,
} = settingsApi;
